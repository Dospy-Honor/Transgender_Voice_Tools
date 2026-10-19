$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot '..\Audio\PitchDetector.cs')
$rate = 12000
foreach ($frequency in @(70, 85, 120, 164, 165, 179, 180, 220, 310, 440)) {
    $samples = [float[]]::new(840)
    for ($i = 0; $i -lt $samples.Length; $i++) {
        $samples[$i] = 0.3 * [Math]::Sin(2 * [Math]::PI * $frequency * $i / $rate)
    }
    $actual = [VoiceTools.Audio.PitchDetector]::Detect($samples, $rate)
    if ([Math]::Abs($actual - $frequency) -gt 1) { throw "Frequency $frequency returned $actual" }
    Write-Output "PASS tone $frequency Hz -> $actual"
}
$silence = [float[]]::new(840)
if ([VoiceTools.Audio.PitchDetector]::Detect($silence, $rate) -ne 0) { throw 'Silence reported pitch' }
$random = [Random]::new(42)
$noise = [float[]]::new(840)
for ($i = 0; $i -lt $noise.Length; $i++) { $noise[$i] = 0.3 * ($random.NextDouble() * 2 - 1) }
if ([VoiceTools.Audio.PitchDetector]::Detect($noise, $rate) -ne 0) { throw 'Noise reported pitch' }
$harmonic = [float[]]::new(840)
for ($i = 0; $i -lt $harmonic.Length; $i++) {
    $phase = 2 * [Math]::PI * 140 * $i / $rate
    $harmonic[$i] = 0.15 * [Math]::Sin($phase) + 0.3 * [Math]::Sin(2 * $phase) + 0.05
}
$actual = [VoiceTools.Audio.PitchDetector]::Detect($harmonic, $rate)
if ([Math]::Abs($actual - 140) -gt 1) { throw "Harmonic/DC signal returned $actual" }
Write-Output 'PASS silence, seeded noise, strong second harmonic and DC offset'
