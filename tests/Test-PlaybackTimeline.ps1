$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot '..\Audio\PlaybackTimeline.cs')
function Near($actual, $expected) { if ([Math]::Abs($actual - $expected) -gt 1e-8) { throw "Expected $expected, got $actual" } }
$t = [VoiceTools.Audio.PlaybackTimeline]::new(10)
Near $t.StartFraction 1
$t.UpdateRecording(2)
Near $t.StartFraction .8
$t.BeginReplay(2, 2)
Near ($t.PlaybackFraction(0)) .8
Near ($t.PlaybackFraction(1)) .9
Near ($t.PlaybackFraction(2)) 1
$t.UpdateRecording(40)
Near $t.EndSeconds 2
Near ($t.PlaybackFraction(1)) .9
$t.EndReplay()
$t.UpdateRecording(40)
Near $t.StartFraction 0
$t.BeginReplay(40,10)
Near $t.StartSeconds 30
Near ($t.PlaybackFraction(0)) 0
Near ($t.PlaybackFraction(5)) .5
Near ($t.PlaybackFraction(15)) 1
Near ($t.PlaybackFraction(-1)) 0
$t.Reset()
Near $t.StartFraction 1
Near $t.DurationSeconds 0
foreach ($window in @(5,10,20,30)) {
 $t = [VoiceTools.Audio.PlaybackTimeline]::new($window)
 $t.UpdateRecording($window / 2)
 Near $t.StartFraction .5
 $t.BeginReplay($window * 3, $window)
 Near ($t.PlaybackFraction($window / 2)) .5
}
Write-Output 'PASS short/full rolling buffers, frozen replay, media position, endpoints, reset and all replay durations'
