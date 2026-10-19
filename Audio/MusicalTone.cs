using System;

namespace VoiceTools.Audio
{
    public static class MusicalTone
    {
        private static readonly string[] Names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        public static double Frequency(int midi) { return 440 * Math.Pow(2, (midi - 69) / 12.0); }
        public static string Name(int midi) { return Names[((midi % 12) + 12) % 12] + (midi / 12 - 1); }
        public static string NearestNote(double hz)
        {
            if (hz <= 0 || double.IsNaN(hz) || double.IsInfinity(hz)) return "";
            return Name((int)Math.Round(69 + 12 * Math.Log(hz / 440, 2)));
        }
    }

    // Phase-continuous oscillator with a short gain ramp to avoid clicks on press/release.
    public sealed class ToneOscillator
    {
        private readonly object gate = new object();
        private double frequency = 220, phase, gain;
        private bool audible;
        public void Play(double hz) { lock (gate) { frequency = hz; audible = true; } }
        public void Stop() { lock (gate) audible = false; }
        public float[] Render(int frames, int sampleRate, int channels)
        {
            var data = new float[frames * channels];
            lock (gate)
            {
                double target = audible ? .14 : 0;
                double ramp = .14 / (sampleRate * .01);
                for (int i = 0; i < frames; i++)
                {
                    gain += Math.Max(-ramp, Math.Min(ramp, target - gain));
                    float value = (float)(gain * Math.Sin(phase));
                    phase = (phase + 2 * Math.PI * frequency / sampleRate) % (2 * Math.PI);
                    for (int c = 0; c < channels; c++) data[i * channels + c] = value;
                }
            }
            return data;
        }
    }
}
