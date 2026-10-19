using System;

namespace VoiceTools.Audio
{
    // All times come from captured sample counts or the media playback session,
    // never from UI timer tick counts or wall-clock time.
    public sealed class PlaybackTimeline
    {
        public PlaybackTimeline(double windowSeconds) { WindowSeconds = windowSeconds; }
        public double WindowSeconds { get; }
        public bool IsReplay { get; private set; }
        public double EndSeconds { get; private set; }
        public double StartSeconds { get; private set; }
        public double DurationSeconds => EndSeconds - StartSeconds;
        public void UpdateRecording(double capturedSeconds)
        {
            if (IsReplay) return;
            EndSeconds = Math.Max(0, capturedSeconds);
            StartSeconds = Math.Max(0, EndSeconds - WindowSeconds);
        }
        public void BeginReplay(double capturedEnd, double actualDuration)
        {
            EndSeconds = Math.Max(0, capturedEnd);
            StartSeconds = Math.Max(0, EndSeconds - Math.Min(WindowSeconds, Math.Max(0, actualDuration)));
            IsReplay = true;
        }
        public void EndReplay() { IsReplay = false; }
        public void Reset() { IsReplay = false; StartSeconds = EndSeconds = 0; }
        public double Fraction(double capturedTime)
        {
            return Math.Max(0, Math.Min(1, (capturedTime - EndSeconds + WindowSeconds) / WindowSeconds));
        }
        public double StartFraction => Fraction(StartSeconds);
        public double PlaybackFraction(double mediaPositionSeconds)
        {
            return Fraction(StartSeconds + Math.Max(0, Math.Min(DurationSeconds, mediaPositionSeconds)));
        }
    }
    public sealed class ReplaySnapshot
    {
        public byte[] Wave { get; set; }
        public double EndSeconds { get; set; }
        public double DurationSeconds { get; set; }
    }
    public sealed class PitchPoint
    {
        public double Time { get; set; }
        public double Pitch { get; set; }
    }
}
