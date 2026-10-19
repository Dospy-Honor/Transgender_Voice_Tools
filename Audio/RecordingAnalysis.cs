using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace VoiceTools.Audio
{
    public sealed class RecordingAnalysis
    {
        public double Duration { get; private set; }
        public double VoicedSeconds { get; private set; }
        public double[] BandSeconds { get; } = new double[4];
        public double Average { get; private set; }
        public double Median { get; private set; }
        public double Low { get; private set; }
        public double High { get; private set; }
        public double VolumeAverage { get; private set; }
        public double VolumeMedian { get; private set; }
        public double VolumeLow { get; private set; }
        public double VolumeHigh { get; private set; }
        public double Percent(int band) { return VoicedSeconds == 0 ? 0 : BandSeconds[band] / VoicedSeconds * 100; }
        // Same fixed intervals as the live pitch display; never classify silence as low pitch.
        public static int Band(double hz) { return hz < 85 || hz > 310 ? 3 : hz < 165 ? 0 : hz < 180 ? 1 : 2; }

        public static RecordingAnalysis ReadWave(Stream stream, CancellationToken cancellation, IProgress<double> progress = null)
        {
            var result = new RecordingAnalysis();
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                if (!stream.CanSeek || stream.Length < 44 || Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF")
                    throw new InvalidDataException("请选择标准 PCM 16 位 WAV 录音。");
                reader.ReadUInt32();
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") throw new InvalidDataException("文件不是 WAV 音频。");
                int channels = 0, rate = 0, bits = 0, format = 0, alignment = 0;
                long dataOffset = 0, dataLength = 0;
                while (stream.Position + 8 <= stream.Length)
                {
                    cancellation.ThrowIfCancellationRequested();
                    string name = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    uint size = reader.ReadUInt32();
                    long end = stream.Position + size;
                    if (end > stream.Length) throw new InvalidDataException("录音文件不完整。");
                    if (name == "fmt ")
                    {
                        if (size < 16) throw new InvalidDataException("WAV 格式信息不完整。");
                        format = reader.ReadUInt16(); channels = reader.ReadUInt16(); rate = reader.ReadInt32();
                        reader.ReadInt32(); alignment = reader.ReadUInt16(); bits = reader.ReadUInt16();
                        // Windows may write WAVE_FORMAT_EXTENSIBLE for PCM.
                        if (format == 65534 && size >= 40)
                        {
                            int extensionSize = reader.ReadUInt16();
                            int validBits = reader.ReadUInt16(); reader.ReadUInt32();
                            var subtype = new Guid(reader.ReadBytes(16));
                            if (extensionSize >= 22 && (validBits == 0 || validBits == 16) && subtype == new Guid("00000001-0000-0010-8000-00aa00389b71")) format = 1;
                        }
                    }
                    if (name == "data") { dataOffset = stream.Position; dataLength = size; }
                    stream.Position = end + (size % 2);
                }
                if (format != 1 || bits != 16 || channels < 1 || channels > 2 || rate < 8000 || rate > 96000 || alignment != channels * 2 || dataOffset == 0)
                    throw new InvalidDataException("支持 8–96 kHz、单声道或双声道的 PCM 16 位 WAV 文件。");
                if (dataLength % alignment != 0) throw new InvalidDataException("录音数据不完整。");
                long samples = dataLength / alignment;
                result.Duration = (double)samples / rate;
                stream.Position = dataOffset;
                int frameSize = rate * 70 / 1000;
                int step = Math.Max(1, rate / 12000);
                var pitches = new List<double>(); var levels = new List<double>();
                double weightedPitch = 0, energyTotal = 0;
                long processed = 0;
                while (processed < samples)
                {
                    cancellation.ThrowIfCancellationRequested();
                    int size = (int)Math.Min(frameSize, samples - processed);
                    var frame = new float[size]; double energy = 0;
                    for (int i = 0; i < size; i++)
                    {
                        double value = 0;
                        for (int channel = 0; channel < channels; channel++) value += reader.ReadInt16() / 32768.0;
                        frame[i] = (float)(value / channels); energy += frame[i] * frame[i];
                    }
                    energyTotal += energy;
                    levels.Add(10 * Math.Log10(Math.Max(1e-9, energy / size)));
                    var reduced = new float[size / step];
                    for (int i = 0; i < reduced.Length; i++)
                        for (int j = 0; j < step; j++) reduced[i] += frame[i * step + j] / step;
                    double hz = PitchDetector.Detect(reduced, rate / step);
                    double seconds = (double)size / rate;
                    if (hz > 0)
                    {
                        pitches.Add(hz); result.VoicedSeconds += seconds;
                        result.BandSeconds[Band(hz)] += seconds; weightedPitch += hz * seconds;
                    }
                    processed += size;
                    if (processed == samples || processed % (frameSize * 20L) == 0) progress?.Report((double)processed / samples);
                }
                if (pitches.Count > 0)
                {
                    pitches.Sort(); result.Average = weightedPitch / result.VoicedSeconds;
                    result.Median = Quantile(pitches, .5); result.Low = Quantile(pitches, .05); result.High = Quantile(pitches, .95);
                }
                if (levels.Count > 0)
                {
                    levels.Sort(); result.VolumeAverage = 10 * Math.Log10(Math.Max(1e-9, energyTotal / samples));
                    result.VolumeMedian = Quantile(levels, .5); result.VolumeLow = Quantile(levels, .05); result.VolumeHigh = Quantile(levels, .95);
                }
            }
            return result;
        }
        private static double Quantile(List<double> data, double q)
        {
            double index = (data.Count - 1) * q;
            int left = (int)index, right = Math.Min(left + 1, data.Count - 1);
            return data[left] + (data[right] - data[left]) * (index - left);
        }
    }
}
