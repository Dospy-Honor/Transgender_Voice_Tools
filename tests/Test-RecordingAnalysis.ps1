$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $PSScriptRoot '..\Audio\PitchDetector.cs'), (Join-Path $PSScriptRoot '..\Audio\RecordingAnalysis.cs'))
function New-TestWave([double[]]$Frequencies, [int]$Frames = 20, [int]$Channels = 1) {
 $rate = 16000
 $segment = 1120 * $Frames
 $total = $Frequencies.Length * $segment
 $stream = [IO.MemoryStream]::new()
 $writer = [IO.BinaryWriter]::new($stream, [Text.Encoding]::UTF8, $true)
 $writer.Write([Text.Encoding]::ASCII.GetBytes('RIFF')); $writer.Write([int](36 + $total * 2 * $Channels))
 $writer.Write([Text.Encoding]::ASCII.GetBytes('WAVEfmt ')); $writer.Write([int]16)
 $writer.Write([int16]1); $writer.Write([int16]$Channels); $writer.Write([int]$rate); $writer.Write([int]($rate * 2 * $Channels))
 $writer.Write([int16](2 * $Channels)); $writer.Write([int16]16)
 $writer.Write([Text.Encoding]::ASCII.GetBytes('data')); $writer.Write([int]($total * 2 * $Channels))
 foreach ($frequency in $Frequencies) {
  for ($i=0; $i -lt $segment; $i++) {
   $value = [int16](8000 * [Math]::Sin(2 * [Math]::PI * $frequency * $i / $rate))
   for ($c=0; $c -lt $Channels; $c++) { $writer.Write($value) }
  }
 }
 $writer.Flush(); $stream.Position = 0
 return ,$stream
}
$stream = New-TestWave @(120,172,220,400,0)
$result = [VoiceTools.Audio.RecordingAnalysis]::ReadWave($stream, [Threading.CancellationToken]::None, $null)
foreach ($band in 0..3) { if ([Math]::Abs($result.Percent($band)-25) -gt .1) { throw "Wrong band $band percentage $($result.Percent($band))" } }
if ([Math]::Abs($result.Duration-7) -gt .01 -or [Math]::Abs($result.VoicedSeconds-5.6) -gt .01) { throw 'Incorrect duration/coverage' }
$stream.Dispose()
Write-Output 'PASS four equal bands; silence excluded; file duration and coverage'
foreach ($hz in @(84.99,85,164.99,165,179.99,180,310,310.01)) {
 $expected = if($hz -lt 85 -or $hz -gt 310){3}elseif($hz -lt 165){0}elseif($hz -lt 180){1}else{2}
 if ([VoiceTools.Audio.RecordingAnalysis]::Band($hz) -ne $expected) { throw "Boundary $hz" }
}
Write-Output 'PASS fixed interval boundaries'
$stream = New-TestWave @(120,220) 230 2
$result = [VoiceTools.Audio.RecordingAnalysis]::ReadWave($stream, [Threading.CancellationToken]::None, $null)
if ($result.Duration -lt 30 -or [Math]::Abs($result.Percent(0)-50) -gt .1 -or [Math]::Abs($result.Percent(2)-50) -gt .1) { throw 'Long/stereo file was not fully analyzed' }
$stream.Dispose()
Write-Output 'PASS full 32.2-second stereo recording (not last 30 seconds)'
$stream = New-TestWave @(0)
$result = [VoiceTools.Audio.RecordingAnalysis]::ReadWave($stream, [Threading.CancellationToken]::None, $null)
if ($result.VoicedSeconds -ne 0 -or $result.Percent(0) -ne 0) { throw 'Silence classified' }
$stream.Dispose()
Write-Output 'PASS all-silent file'
$stream = New-TestWave @(120)
$cancel = [Threading.CancellationTokenSource]::new(); $cancel.Cancel()
$rejected = $false
try { [VoiceTools.Audio.RecordingAnalysis]::ReadWave($stream, $cancel.Token, $null) } catch { $rejected = $true }
if (!$rejected) { throw 'Cancellation not respected' }
$stream.Dispose(); $cancel.Dispose()
$stream = [IO.MemoryStream]::new([byte[]](1,2,3,4))
$rejected = $false
try { [VoiceTools.Audio.RecordingAnalysis]::ReadWave($stream, [Threading.CancellationToken]::None, $null) } catch { $rejected = $true }
if (!$rejected) { throw 'Invalid file accepted' }
$stream.Dispose()
Write-Output 'PASS cancellation and malformed file rejection'
