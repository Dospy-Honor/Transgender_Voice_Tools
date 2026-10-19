using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using VoiceTools.Settings;

namespace VoiceTools.Audio
{
    [ComImport, Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe interface IMemoryBufferByteAccess { void GetBuffer(out byte* buffer, out uint capacity); }

    public sealed class VoiceEngine : IDisposable
    {
        private readonly object gate = new object();
        private AudioGraph graph;
        private AudioFrameOutputNode output;
        private AudioFrameInputNode toneInput;
        private AudioDeviceOutputNode toneOutput;
        private readonly ToneOscillator oscillator = new ToneOscillator();
        private readonly System.Threading.SemaphoreSlim toneSetup = new System.Threading.SemaphoreSlim(1, 1);
        private float[] ring;
        private int position, count, sampleRate;
        private long totalSamples;
        public double CapturedSeconds { get { lock (gate) return sampleRate == 0 ? 0 : (double)totalSamples / sampleRate; } }
        public bool Running { get; private set; }

        public async Task StartAsync()
        {
            if (graph != null) { graph.Start(); Running = true; return; }
            var settings = new AudioGraphSettings(AudioRenderCategory.Media);
            var selectedOutput = await AudioDevices.SelectedAsync(false);
            if (selectedOutput != null) settings.PrimaryRenderDevice = selectedOutput;
            var selectedInput = await AudioDevices.SelectedAsync(true);
            var result = await AudioGraph.CreateAsync(settings);
            if (result.Status != AudioGraphCreationStatus.Success) throw new InvalidOperationException(Locale.T("无法初始化音频设备：") + result.Status);
            graph = result.Graph;
            try
            {
                var input = selectedInput == null
                    ? await graph.CreateDeviceInputNodeAsync(MediaCategory.Other)
                    : await graph.CreateDeviceInputNodeAsync(MediaCategory.Other, graph.EncodingProperties, selectedInput);
                if (input.Status != AudioDeviceNodeCreationStatus.Success) throw new InvalidOperationException(Locale.T("无法访问麦克风，请检查 Windows 麦克风权限和输入设备：") + input.Status);
                sampleRate = (int)graph.EncodingProperties.SampleRate;
                ring = new float[sampleRate * 30];
                var format = AudioEncodingProperties.CreatePcm((uint)sampleRate, 1, 32);
                format.Subtype = MediaEncodingSubtypes.Float;
                output = graph.CreateFrameOutputNode(format);
                input.DeviceInputNode.AddOutgoingConnection(output);
                graph.QuantumProcessed += OnQuantum;
                graph.Start(); Running = true;
            }
            catch { Dispose(); throw; }
        }

        private unsafe void OnQuantum(AudioGraph sender, object args)
        {
            lock (gate)
            {
                if (output == null) return;
                using (var frame = output.GetFrame())
                using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
                using (var reference = buffer.CreateReference())
                {
                    byte* bytes; uint capacity;
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out bytes, out capacity);
                    var values = (float*)bytes;
                    int length = (int)Math.Min(buffer.Length, capacity) / sizeof(float);
                    for (int i = 0; i < length; i++)
                    {
                        ring[position] = values[i]; position = (position + 1) % ring.Length;
                        count = Math.Min(count + 1, ring.Length);
                    }
                    totalSamples += length;
                }
            }
        }

        public void Pause() { graph?.Stop(); Running = false; }
        public async Task EnsureToneOutputAsync()
        {
            await toneSetup.WaitAsync();
            try
            {
                if (toneInput != null || graph == null) return;
                var owner = graph;
                var result = await owner.CreateDeviceOutputNodeAsync();
                if (owner != graph) { result.DeviceOutputNode?.Dispose(); return; }
                if (result.Status != AudioDeviceNodeCreationStatus.Success)
                    throw new InvalidOperationException(Locale.T("无法播放参考音：") + result.Status);
                toneOutput = result.DeviceOutputNode;
                toneInput = graph.CreateFrameInputNode(graph.EncodingProperties);
                toneInput.AddOutgoingConnection(toneOutput);
                toneInput.QuantumStarted += ToneQuantum;
                toneOutput.Start();
                toneInput.Start();
            }
            finally { toneSetup.Release(); }
        }
        public void PlayTone(double hz) { oscillator.Play(hz); }
        public void StopTone() { oscillator.Stop(); }
        private unsafe void ToneQuantum(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args)
        {
            int frames = args.RequiredSamples;
            if (frames <= 0) return;
            int channels = (int)sender.EncodingProperties.ChannelCount;
            var samples = oscillator.Render(frames, (int)sender.EncodingProperties.SampleRate, channels);
            using (var frame = new AudioFrame((uint)(samples.Length * sizeof(float))))
            {
                using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
                using (var reference = buffer.CreateReference())
                {
                    byte* bytes; uint capacity;
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out bytes, out capacity);
                    if (capacity < samples.Length * sizeof(float)) return;
                    var values = (float*)bytes;
                    for (int i = 0; i < samples.Length; i++) values[i] = samples[i];
                }
                sender.AddFrame(frame);
            }
        }
        public void Clear() { lock (gate) { if (ring != null) Array.Clear(ring, 0, ring.Length); position = count = 0; totalSamples = 0; } }
        public float[] Snapshot(double seconds)
        {
            lock (gate)
            {
                int size = Math.Min(count, (int)(sampleRate * seconds));
                var data = new float[size];
                for (int i = 0; i < size; i++) data[i] = ring[(position - size + i + ring.Length) % ring.Length];
                return data;
            }
        }
        public double Analyze(out double db)
        {
            double ignored;
            return Analyze(out db, out ignored);
        }
        public double Analyze(out double db, out double capturedTime)
        {
            float[] data;
            lock (gate) { data = Snapshot(0.07); capturedTime = CapturedSeconds; }
            double energy = 0;
            foreach (var value in data) energy += value * value;
            db = data.Length == 0 ? -90 : Math.Max(-90, 10 * Math.Log10(Math.Max(1e-9, energy / data.Length)));
            if (data.Length < sampleRate * 0.06 || sampleRate == 0) return 0;
            int step = Math.Max(1, sampleRate / 12000);
            var reduced = new float[data.Length / step];
            for (int i = 0; i < reduced.Length; i++)
            {
                double sum = 0;
                for (int j = 0; j < step; j++) sum += data[i * step + j];
                reduced[i] = (float)(sum / step);
            }
            return PitchDetector.Detect(reduced, sampleRate / step);
        }
        public byte[] ReplayWave(int seconds) { return Wave(Snapshot(seconds), sampleRate); }
        public ReplaySnapshot CaptureReplay(int seconds)
        {
            lock (gate)
            {
                var samples = Snapshot(seconds);
                return new ReplaySnapshot { Wave = Wave(samples, sampleRate), EndSeconds = CapturedSeconds,
                    DurationSeconds = sampleRate == 0 ? 0 : (double)samples.Length / sampleRate };
            }
        }
        private static byte[] Wave(float[] samples, int rate)
        {
            if (samples.Length == 0 || rate == 0) return null;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
                foreach (var sample in samples) writer.Write((short)(Math.Max(-1, Math.Min(1, sample)) * 32767));
                return stream.ToArray();
            }
        }
        public void Dispose()
        {
            oscillator.Stop();
            Pause();
            if (toneInput != null) toneInput.QuantumStarted -= ToneQuantum;
            toneInput = null; toneOutput = null;
            if (graph != null) graph.QuantumProcessed -= OnQuantum;
            lock (gate) { output = null; Clear(); }
            graph?.Dispose(); graph = null;
        }
    }
}


