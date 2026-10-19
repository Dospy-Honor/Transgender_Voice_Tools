using System;

namespace VoiceTools.Audio
{
    // Normalized autocorrelation, followed by sub-sample peak interpolation.
    public static class PitchDetector
    {
        public static double Detect(float[] samples, int sampleRate)
        {
            if (samples == null || sampleRate < 1000 || samples.Length < sampleRate / 30) return 0;
            double mean = 0, energy = 0;
            foreach (var value in samples) mean += value;
            mean /= samples.Length;
            var centered = new double[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                centered[i] = samples[i] - mean;
                energy += centered[i] * centered[i];
            }
            if (energy / samples.Length < 0.00001) return 0;
            int min = sampleRate / 500, max = Math.Min(sampleRate / 65, samples.Length / 2 - 1);
            var correlations = new double[max + 2];
            double best = 0;
            for (int lag = min - 1; lag <= max + 1; lag++)
            {
                double cross = 0, left = 0, right = 0;
                for (int i = 0; i < samples.Length - lag; i++)
                {
                    double a = centered[i], b = centered[i + lag];
                    cross += a * b; left += a * a; right += b * b;
                }
                correlations[lag] = cross / Math.Sqrt(Math.Max(1e-20, left * right));
                if (lag >= min && lag <= max) best = Math.Max(best, correlations[lag]);
            }
            if (best < 0.8) return 0;
            for (int lag = min; lag <= max; lag++)
            {
                double peak = correlations[lag];
                if (peak < Math.Max(0.8, best * 0.94) || peak < correlations[lag - 1] || peak < correlations[lag + 1]) continue;
                double denominator = correlations[lag - 1] - 2 * peak + correlations[lag + 1];
                double offset = Math.Abs(denominator) < 1e-12 ? 0 : 0.5 * (correlations[lag - 1] - correlations[lag + 1]) / denominator;
                return sampleRate / (lag + Math.Max(-0.5, Math.Min(0.5, offset)));
            }
            return 0;
        }
    }
}
