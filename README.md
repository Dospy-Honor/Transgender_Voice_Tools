# VoiceTools for Windows

English | [简体中文](README_CN.md)

A local voice practice and pitch analysis app built with C# and UWP for Windows. Inspired by [Voice Tools by DevExtras](https://devextras.com/voicetools/), this is an independent, unofficial implementation and is not affiliated with or endorsed by DevExtras.

<p align="center">
  <img src="docs/branding/trans-microphone-master.png" alt="VoiceTools application icon" width="160" />
</p>

## Features

- **Live pitch:** opens automatically on launch and starts microphone capture. Displays a scrolling pitch trace, input level and pitch statistics.
- **Recent-audio playback:** replay the last 5, 10, 20 or 30 seconds; the default is 10 seconds. A gray line marks the recording start, and a red line follows playback.
- **Tone practice:** hold one of 24 note buttons from F♯2 to F4 to hear a reference tone while microphone analysis continues. Reference tuning is A4 = 440 Hz.
- **Full-recording analysis:** record a reading passage or import a supported WAV file, then view pitch statistics and time percentages within fixed pitch ranges. Replay or export recordings.
- **Frequency scales:** ERB (default), Mel and Linear change chart coordinates, grid spacing and band positions while keeping frequency values in Hz.
- **Language-specific content:** Simplified Chinese and English interfaces, with separate reading passages and suggested-sentence libraries.
- **Editable sentence libraries:** a dedicated page with numbered, single-line text boxes, a delete button per row, and Add / Save buttons. Only the current language's library is editable.
- **Audio device selection:** choose input and output devices or use the system defaults.

## Requirements

| Item | Requirement |
| --- | --- |
| Operating system | Windows 10 version 2004, build 19041, or later |
| Development environment | Visual Studio 2022 with UWP development components |
| Windows SDK | 10.0.22621.0 |
| Tests | PowerShell 7 (`pwsh`) |
| Audio | Microphone permission; headphones recommended for reference tones |

The project contains x86, x64, ARM and ARM64 configurations. Debug/x64 has been build-checked; other architectures require separate validation. **Windows 10 build 15063 / ARM32 compatibility has not been implemented.**

## Original target and TODO

The original target devices were **Lumia 950 and Lumia 950 XL** running Windows 10 Mobile. A suitable ARM32 compiler/toolchain is not currently available in the development environment, and the application uses some newer Windows APIs and UI features. Support for these devices is therefore a work in progress, not a currently supported configuration.

- [ ] Set up a compatible Windows 10 Mobile / ARM32 compiler and build toolchain.
- [ ] Audit and replace or provide fallbacks for features unavailable on Windows 10 build 15063.
- [ ] Adjust the minimum OS version and packaging configuration after compatibility work.
- [ ] Build an ARM32 package and validate installation, UI, microphone capture and playback on Lumia 950 / 950 XL.

## Build and run

1. Open `VoiceTools.sln` in Visual Studio.
2. Restore NuGet packages.
3. Select **Debug / x64 / Local Machine** and deploy using Visual Studio.
4. Allow microphone access when prompted. If access fails, check Windows microphone privacy settings and the selected input device.

From a Visual Studio developer shell, restore and build in separate commands:

```powershell
MSBuild.exe VoiceTools.csproj /t:Restore /p:Configuration=Debug /p:Platform=x64
MSBuild.exe VoiceTools.csproj /t:Build /p:Configuration=Debug /p:Platform=x64 /p:AppxBundle=Never
```

Packages are generated under `AppPackages/`. The current project enables signing and references `VoiceTools_TemporaryKey.pfx`. A development certificate does not establish public trust: configure your own signing certificate for distribution. Do not publish private signing keys.

A known packaging warning, `PRI257`, concerns the default language and dependency resource languages. Successful compilation does not replace device testing.

## Usage

### Live voice and playback

The app opens the Voice page and begins capturing audio automatically. The recording-start marker moves from right to left as the playback window fills. Once full, it remains at the left edge.

Click the playback button to listen to the available recording. The chart freezes and the playback marker moves to the right. **Clicking the button during playback stops immediately, clears the current audio and trace, and starts a new recording.** Playback completion and the Resume recording button also start a fresh recording; this is not a pause/resume toggle.

### Tone practice

Open Tone from the hamburger menu. Hold a note with the mouse, touch, or Space/Enter while focused. Release to stop the tone. The displayed frequency and note name come from the microphone. Headphones help prevent the reference tone from being picked up by the microphone.

### Reading analysis

Open Analysis, review or edit the passage, and select Record full passage. Select Stop and analyze to process the complete recording, including recordings longer than the live page's 30-second buffer.

Supported imports are **8–96 kHz, mono or stereo, 16-bit PCM WAV**, including PCM WAVE_FORMAT_EXTENSIBLE. Compressed WAV, MP3 and M4A are not supported.

### Settings and sentence libraries

Settings include language, gender/color markers, frequency scale, playback-window duration, reading text, and audio devices.

Select Suggested sentences to open the editor. Edit directly in the single-line fields, use **+** to append an empty row, and use the row's delete button to remove it. **Save** applies the changes. Empty rows must be filled or removed; an entirely empty library can be saved. Changes left unsaved do not take effect.

The editor displays only the current application language's library. Switch the application language in Settings to edit the other library. Returning to Voice loads the saved library for that language.

## How analysis works

Pitch detection uses this project's normalized autocorrelation implementation, not the original app's internal algorithm. Fixed reference bands are:

| Range | Reference color |
| --- | --- |
| 85 ≤ Hz < 165 | Blue |
| 165 ≤ Hz < 180 | Gray |
| 180 ≤ Hz ≤ 310 | Pink |
| Other reliably detected pitches | Outside the reference ranges |

Full-recording percentages use the duration of reliably detected pitch as their denominator. Silence and unrecognized frames affect coverage, not band percentages. The four categories total 100% before rounding. Mean pitch is duration-weighted; median and percentile statistics use valid frames.

Band thresholds and colors are not user-editable. Hiding gender/color markers changes presentation without changing calculations. These ranges reproduce a reference interface; they are not a medical standard. Pitch alone does not determine perceived voice gender. Input level is **dBFS**, not calibrated sound pressure level (dB SPL). Noise, breathy speech and multiple simultaneous voices can affect results.

## Local data

Audio processing runs locally; the app does not upload recordings. The live page keeps up to 30 seconds of audio in memory. Long recordings use temporary WAV files, which the app removes when leaving the analysis page; abnormal termination may leave temporary files for later system cleanup. Imported source files are not deleted. Exported files remain where you save them.

Settings, custom passages and sentence libraries are stored locally. Chinese and English content is stored separately.

## Tests

Run each script in its own PowerShell 7 process:

```powershell
pwsh -NoProfile -File tests/Test-PitchDetector.ps1
pwsh -NoProfile -File tests/Test-MusicalTone.ps1
pwsh -NoProfile -File tests/Test-RecordingAnalysis.ps1
pwsh -NoProfile -File tests/Test-PlaybackTimeline.ps1
pwsh -NoProfile -File tests/Test-Settings.ps1
```

Tests cover pitch detection, reference tones, full-file analysis, timeline coordinates, frequency scales, localization and language-separated storage. Settings tests use a storage adapter stub. Microphone capture, device routing, playback and installation still require Windows hardware testing; see [manual checks](docs/manual-checks.md).

## Project layout

- `Audio/` — capture, pitch detection, tone generation, playback and recording analysis.
- `Settings/` — preferences, localization, frequency scales and practice content.
- `*_Page.xaml` / `*_Page.xaml.cs` — application pages and interaction logic.
- `Assets/` — application icons, tiles and playback controls.
- `tests/` — automated checks.
- `docs/` — manual checks, visual references and branding notes.

## Artwork, references and licensing

The current icon was generated for this project. See [branding notes](docs/branding/README.md) for source images, generation prompts and instructions for switching icon references to `_old` assets.

Original Voice Tools icon backups (`*_old` and `docs/branding/icon_old.png`) and reference screenshots are third-party materials. Their inclusion does not grant permission to redistribute or reuse them. The Chinese default reading passage credits 《喜剧世界》第7期, 庞启帆/编译; that attribution is not a redistribution license. These materials must not be described as original project content or automatically covered by a project license.