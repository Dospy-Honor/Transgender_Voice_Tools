$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $PSScriptRoot '..\Audio\MusicalTone.cs'), (Join-Path $PSScriptRoot '..\Audio\PitchDetector.cs'))
if ([Math]::Abs([VoiceTools.Audio.MusicalTone]::Frequency(69) - 440) -gt .001) { throw 'A4 reference incorrect' }
if ([VoiceTools.Audio.MusicalTone]::NearestNote(224) -ne 'A3') { throw 'Original screenshot note mapping incorrect' }
if ([VoiceTools.Audio.MusicalTone]::NearestNote(0) -ne '') { throw 'Silence mapped to note' }
foreach($midi in 42..65) {
 $hz = [VoiceTools.Audio.MusicalTone]::Frequency($midi)
 if ([VoiceTools.Audio.MusicalTone]::NearestNote($hz) -ne [VoiceTools.Audio.MusicalTone]::Name($midi)) { throw "Note mapping $midi" }
 $osc = [VoiceTools.Audio.ToneOscillator]::new(); $osc.Play($hz)
 $signal = $osc.Render(2400, 12000, 1)
 $frame = [float[]]($signal[1200..2039])
 $detected = [VoiceTools.Audio.PitchDetector]::Detect($frame, 12000)
 if ([Math]::Abs($detected-$hz) -gt 1) { throw "Oscillator $hz detected $detected" }
 $osc.Stop(); $tail = $osc.Render(600, 12000, 1)
 foreach($sample in $tail[121..599]) { if ([Math]::Abs($sample) -gt .000001) { throw 'Tone continued after release' } }
}
Write-Output 'PASS 24 note mappings, oscillator pitch and release fade to silence'
$whole = [VoiceTools.Audio.ToneOscillator]::new(); $whole.Play(220)
$expected = $whole.Render(2400,12000,2)
$chunks = [VoiceTools.Audio.ToneOscillator]::new(); $chunks.Play(220)
$actual = @($chunks.Render(1200,12000,2)) + @($chunks.Render(1200,12000,2))
for($i=0; $i -lt $expected.Length; $i++) { if ([Math]::Abs($expected[$i]-$actual[$i]) -gt .000001) { throw 'Discontinuity between output frames' } }
Write-Output 'PASS phase continuity across audio callbacks and stereo output'
