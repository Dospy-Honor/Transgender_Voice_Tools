$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Add-Type -Path @((Join-Path $PSScriptRoot 'SettingsStubs.cs'), (Join-Path $root 'Settings\AppSettings.cs'), (Join-Path $root 'Settings\FrequencyScale.cs'))
foreach ($scale in @('ERB','Mel','Linear')) {
 if ([Math]::Abs([VoiceTools.Settings.FrequencyScale]::Position(50,50,500,$scale)-1) -gt 1e-9) { throw "Low endpoint $scale" }
 if ([Math]::Abs([VoiceTools.Settings.FrequencyScale]::Position(500,50,500,$scale)) -gt 1e-9) { throw "High endpoint $scale" }
 $previous = 2.0
 foreach ($hz in 50..500) {
  $y = [VoiceTools.Settings.FrequencyScale]::Position($hz,50,500,$scale)
  if ($y -ge $previous) { throw "Nonmonotonic $scale" }; $previous = $y
 }
}
if ([Math]::Abs([VoiceTools.Settings.FrequencyScale]::Transform(1000,'Mel') - 999.9855) -gt .01) { throw 'Mel conversion' }
if ([Math]::Abs([VoiceTools.Settings.FrequencyScale]::Transform(1000,'ERB') - 15.6214) -gt .01) { throw 'ERB conversion' }
foreach ($s in [VoiceTools.Settings.PracticeTexts]::Sentences('en-US')) { if ($s -match '[\u4e00-\u9fff]') { throw 'Chinese sentence in English bank' } }
foreach ($s in [VoiceTools.Settings.PracticeTexts]::Sentences('zh-CN')) { if ($s -match '[a-zA-Z]') { throw 'English sentence in Chinese bank' } }
if ([VoiceTools.Settings.PracticeTexts]::DefaultPassage('en-US') -match '[\u4e00-\u9fff]') { throw 'Mixed default passage' }
Write-Output 'PASS ERB/Mel/Linear endpoints, monotonic coordinates, formulas and isolated sentence banks'
$folder = Join-Path $PSScriptRoot ('settings-fixture-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($folder) | Out-Null
[Windows.Storage.ApplicationData]::Current.LocalFolder.Path = $folder
try {
 if ([VoiceTools.Settings.AppSettings]::Language -ne 'zh-CN' -or ![VoiceTools.Settings.AppSettings]::ShowGender -or [VoiceTools.Settings.AppSettings]::Scale -ne 'ERB') { throw 'Defaults incorrect' }
 [IO.File]::WriteAllText([VoiceTools.Settings.AppSettings]::PassagePath, [VoiceTools.Settings.PracticeTexts]::LegacyChinesePassage.Replace("`n","`r`n"))
 if ([VoiceTools.Settings.AppSettings]::Passage -ne [VoiceTools.Settings.PracticeTexts]::DefaultPassage('zh-CN')) { throw 'Legacy default not upgraded' }
 [VoiceTools.Settings.AppSettings]::SavePassage([VoiceTools.Settings.PracticeTexts]::DefaultPassage('zh-CN'))
 if ([IO.File]::Exists([VoiceTools.Settings.AppSettings]::PassagePath)) { throw 'Default passage cached as custom text' }
 $initial = [VoiceTools.Settings.AppSettings]::Sentences
 if ($initial.Count -ne 4) { throw 'Default sentences missing' }
 [VoiceTools.Settings.AppSettings]::SaveSentences(@('第一句', "第二句`n第二行"))
 if ([VoiceTools.Settings.AppSettings]::Sentences[1] -ne "第二句`n第二行") { throw 'Sentence storage failed' }
 [VoiceTools.Settings.AppSettings]::Language = 'en-US'
 if ([VoiceTools.Settings.AppSettings]::Sentences.Count -ne 4) { throw 'Chinese library leaked into English' }
 [VoiceTools.Settings.AppSettings]::SaveSentences(@('English sentence'))
 [VoiceTools.Settings.AppSettings]::Language = 'zh-CN'
 if ([VoiceTools.Settings.AppSettings]::Sentences[0] -ne '第一句') { throw 'Chinese library lost' }
 [VoiceTools.Settings.AppSettings]::SaveSentences(@('修改后的语句'))
 if ([VoiceTools.Settings.AppSettings]::Sentences[0] -ne '修改后的语句') { throw 'Sentence edit failed' }
 [VoiceTools.Settings.AppSettings]::SaveSentences([string[]]@())
 if ([VoiceTools.Settings.AppSettings]::Sentences.Count -ne 0) { throw 'Deleting all sentences restored defaults' }
 $invalid = $false; try { [VoiceTools.Settings.AppSettings]::SaveSentences(@('  ')) } catch { $invalid = $true }
 if (!$invalid) { throw 'Blank sentence accepted' }
 [VoiceTools.Settings.AppSettings]::Language = 'en-US'
 if ([VoiceTools.Settings.AppSettings]::Sentences[0] -ne 'English sentence') { throw 'Editing Chinese altered English' }
 [VoiceTools.Settings.AppSettings]::Language = 'zh-CN'
 Write-Output 'PASS sentence defaults, edits, deletion, empty library, multiline storage and per-language isolation'
 [VoiceTools.Settings.AppSettings]::SavePassage('中文自定义长文')
 [VoiceTools.Settings.AppSettings]::Language = 'en-US'
 if ([VoiceTools.Settings.AppSettings]::Passage -eq '中文自定义长文') { throw 'Cross-language passage leak' }
 $longText = ('An English custom passage. ' * 1000)
 [VoiceTools.Settings.AppSettings]::SavePassage($longText)
 if ([VoiceTools.Settings.AppSettings]::Passage -ne $longText) { throw 'Long passage truncated' }
 [VoiceTools.Settings.AppSettings]::Language = 'zh-CN'
 if ([VoiceTools.Settings.AppSettings]::Passage -ne '中文自定义长文') { throw 'Chinese passage not retained' }
 [VoiceTools.Settings.AppSettings]::ShowGender = $false
 [VoiceTools.Settings.AppSettings]::Scale = 'ERB'
 [VoiceTools.Settings.AppSettings]::InputDevice = 'input-id'
 [VoiceTools.Settings.AppSettings]::OutputDevice = 'output-id'
 if ([Windows.Storage.ApplicationData]::Current.LocalSettings.Values['ShowGender'] -ne $false -or [VoiceTools.Settings.AppSettings]::Scale -ne 'ERB' -or [VoiceTools.Settings.AppSettings]::InputDevice -ne 'input-id' -or [VoiceTools.Settings.AppSettings]::OutputDevice -ne 'output-id') { throw 'Setting persistence mapping failed' }
 $rejected=$false; try { [VoiceTools.Settings.AppSettings]::SavePassage('  ') } catch { $rejected=$true }
 if (!$rejected) { throw 'Empty text accepted' }
 Write-Output 'PASS setting storage mapping, language-separated custom passages, long text and empty-text rejection (storage adapter test)'
} finally {
 $resolved = [IO.Path]::GetFullPath($folder)
 if (!$resolved.StartsWith([IO.Path]::GetFullPath($PSScriptRoot) + [IO.Path]::DirectorySeparatorChar)) { throw 'Unexpected fixture path' }
 Remove-Item -LiteralPath $resolved -Recurse -Force
}
$locale = Get-Content (Join-Path $root 'Settings\Locale.cs') -Raw
$block = [regex]::Match($locale,'(?s)private const string Translations = @"(.*?)";').Groups[1].Value
$keys = @{}
foreach($line in $block.Split("`n")) { $parts=$line.TrimEnd("`r").Split('|',2); if($parts.Length -eq 2){ $keys[$parts[0].Replace('\n',"`n")]=$true } }
foreach($file in Get-ChildItem $root -Filter '*.xaml') {
 [xml]$xml = Get-Content $file.FullName -Raw
 foreach($attribute in $xml.SelectNodes('//@*')) {
  if($attribute.Value -match '[\u4e00-\u9fff]' -and $attribute.Value -ne '简体中文' -and !$keys.ContainsKey($attribute.Value)) { throw "Untranslated XAML $($file.Name): $($attribute.Value)" }
 }
}
foreach($file in @('Settings_Page.xaml.cs','Voice_Page.xaml.cs','Analysis_Page.xaml.cs','MainPage.xaml.cs','Audio\VoiceEngine.cs')) {
 $code = Get-Content (Join-Path $root $file) -Raw
 foreach($match in [regex]::Matches($code,'"(?:\\.|[^"\\])*"')) {
  if($match.Value -match '[\u4e00-\u9fff]') {
   $value = $match.Value.Substring(1,$match.Value.Length-2)
   if(!$code.Substring(0,$match.Index).EndsWith('Locale.T(')) { throw "Dynamic string not localized: $value" }
   if(!$keys.ContainsKey($value)) { throw "Missing dynamic translation: $value" }
  }
 }
}
Write-Output 'PASS UI translation coverage and dynamic-string localization audit'

$erb = [VoiceTools.Settings.FrequencyScale]::Position(200,50,500,'ERB')
$mel = [VoiceTools.Settings.FrequencyScale]::Position(200,50,500,'Mel')
$linear = [VoiceTools.Settings.FrequencyScale]::Position(200,50,500,'Linear')
if ([Math]::Abs($erb-$mel) -lt .03 -or [Math]::Abs($mel-$linear) -lt .03) { throw 'Scales do not produce distinct chart positions' }
[VoiceTools.Settings.AppSettings]::Scale = 'Linear'
if ([VoiceTools.Settings.AppSettings]::Scale -ne 'Linear') { throw 'Explicit Linear selection lost' }
Write-Output 'PASS distinct scale coordinates and explicit Linear preference'
