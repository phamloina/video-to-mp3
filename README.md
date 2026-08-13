# Video To MP3

[![CI](https://github.com/phamloina/video-to-mp3/actions/workflows/ci.yml/badge.svg)](https://github.com/phamloina/video-to-mp3/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/phamloina/video-to-mp3)](https://github.com/phamloina/video-to-mp3/releases/tag/v0.2.2)
[![Release downloads](https://img.shields.io/github/downloads/phamloina/video-to-mp3/total)](https://github.com/phamloina/video-to-mp3/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Good first issues](https://img.shields.io/github/issues/phamloina/video-to-mp3/good%20first%20issue)](https://github.com/phamloina/video-to-mp3/issues?q=is%3Aissue%20state%3Aopen%20label%3A%22good%20first%20issue%22)

Open-source Windows desktop app for converting local video files and supported online video URLs to MP3. Built with C#, .NET 8, WPF, FFmpeg, ffprobe, and yt-dlp.

> Status: stable release [`v0.2.2`](https://github.com/phamloina/video-to-mp3/releases/tag/v0.2.2). Core features and the portable Windows x64 package have passed automated QA. Online conversion remains subject to each source website's availability and access controls.

## Download

Download the versioned Windows x64 portable ZIP and its SHA-256 checksum from the [`v0.2.2` release](https://github.com/phamloina/video-to-mp3/releases/tag/v0.2.2). Extract the ZIP to a writable directory and run `VideoToMp3.exe`; no installation or administrator privileges are required.

Testing the preview on Windows? Follow [UAT.md](UAT.md) and report results in [UAT issue #28](https://github.com/phamloina/video-to-mp3/issues/28).

## Help the project

- Test the preview and post reproducible results in [UAT issue #28](https://github.com/phamloina/video-to-mp3/issues/28).
- Pick a labeled [good first issue](https://github.com/phamloina/video-to-mp3/issues?q=is%3Aissue%20state%3Aopen%20label%3A%22good%20first%20issue%22).
- Share the release with Windows users who can provide honest technical feedback.
- Star the repository only if the project is useful to you.

See [ADOPTION.md](ADOPTION.md) for the tester and contributor outreach plan. The project does not use spam, purchased engagement, or fabricated usage metrics.

Maintainers can use [OUTREACH_KIT.md](OUTREACH_KIT.md) for transparent Vietnamese and English tester invitations.

## Goals

- Queue local files and supported URLs in one desktop workflow.
- Keep the UI responsive during probing, downloading, and conversion.
- Provide accurate progress, cancellation, safe output naming, settings, and history.
- Respect platform terms and content rights. The project does not bypass DRM or protected content.

## Capabilities

- Multi-file picker and drag-and-drop
- URL input, metadata probe, and playlist support
- MP3 quality selection and configurable output folder
- Per-job and overall progress, retry, cancellation, and error details
- Windows x64 release packaging

See [ROADMAP.md](ROADMAP.md) for the public plan.

## Requirements

- Windows 10/11 x64
- .NET SDK 8.0 or later for development
- Internet access on first conversion so the app can download FFmpeg/ffprobe and yt-dlp when they are not already installed

The app first looks in its portable `tools` directory, `%LOCALAPPDATA%\VideoToMp3\tools`, Winget packages, and `PATH`. Missing tools are downloaded automatically on first use. You can alternatively install them for the current Windows user with Winget:

```powershell
winget install --id Gyan.FFmpeg --exact
winget install --id yt-dlp.yt-dlp --exact
```

If YouTube, Facebook, or another supported website requires sign-in, enable **Use browser cookies for online links** and select Firefox, Chrome, or Edge. This explicit opt-in remains off by default. Chromium-based browsers on Windows may prevent yt-dlp from reading their encrypted cookie database; Firefox is the recommended fallback. See the [yt-dlp cookie guidance](https://github.com/yt-dlp/yt-dlp/wiki/FAQ#how-do-i-pass-cookies-to-yt-dlp) before enabling it. The app does not bypass private content, DRM, or anti-bot verification.

## Build and test

```powershell
& "C:\Program Files\dotnet\dotnet.exe" restore VideoToMp3.sln
& "C:\Program Files\dotnet\dotnet.exe" build VideoToMp3.sln --configuration Debug
& "C:\Program Files\dotnet\dotnet.exe" test VideoToMp3.sln --configuration Debug
```

## Publish Windows x64

Create a self-contained, single-file Windows x64 build that runs without an installed .NET runtime:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/publish-win-x64.ps1
```

The output is written to `artifacts/publish/win-x64`. The app detects installed tools and downloads missing tools to `%LOCALAPPDATA%\VideoToMp3\tools` on first use. You may also place tools in these application-relative paths:

- `tools/ffmpeg/ffmpeg.exe`
- `tools/ffmpeg/ffprobe.exe`
- `tools/yt-dlp/yt-dlp.exe`

Settings and history are stored in `%LOCALAPPDATA%\VideoToMp3`; logs are stored in `%LOCALAPPDATA%\VideoToMp3\logs`. No developer-machine paths are required at runtime.

## Portable package

Create a versioned ZIP and SHA-256 checksum:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/package-portable-win-x64.ps1
```

The package is written to `artifacts/packages`. Extract it to any writable directory and run `VideoToMp3.exe`; installation and administrator privileges are not required. Delete the extracted directory to remove the application. User settings, history, and logs remain under `%LOCALAPPDATA%\VideoToMp3` and can be deleted separately if desired.

## Architecture

```text
src/VideoToMp3.App             WPF presentation layer (MVVM)
src/VideoToMp3.Core            Domain models and application contracts
src/VideoToMp3.Infrastructure  External-process and persistence implementations
tests/VideoToMp3.Tests         Automated tests
```

Dependencies point inward: App and Infrastructure depend on Core; Core has no WPF or infrastructure dependency.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md), follow the issue and pull-request templates, and run the build and tests before submitting a pull request.

## Security

Report vulnerabilities privately as described in [SECURITY.md](SECURITY.md). Do not open a public issue for a potential security vulnerability.

## License

Distributed under the [MIT License](LICENSE).
