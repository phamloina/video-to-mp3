# Video To MP3

Open-source Windows desktop app for converting local video files and supported online video URLs to MP3. Built with C#, .NET 8, WPF, FFmpeg, ffprobe, and yt-dlp.

> Status: pre-release. Core conversion, queue, settings, history, theme, and Windows x64 publishing workflows are implemented and under active QA.

## Goals

- Queue local files and supported URLs in one desktop workflow.
- Keep the UI responsive during probing, downloading, and conversion.
- Provide accurate progress, cancellation, safe output naming, settings, and history.
- Respect platform terms and content rights. The project does not bypass DRM or protected content.

## Planned capabilities

- Multi-file picker and drag-and-drop
- URL input, metadata probe, and playlist support
- MP3 quality selection and configurable output folder
- Per-job and overall progress, retry, cancellation, and error details
- Windows x64 release packaging

See [ROADMAP.md](ROADMAP.md) for the public plan and [VIDEO_TO_MP3_CODEX_MASTER_PROMPT.md](VIDEO_TO_MP3_CODEX_MASTER_PROMPT.md) for implementation state.

## Requirements

- Windows 10/11 x64
- .NET SDK 8.0 or later for development
- FFmpeg/ffprobe and yt-dlp placed in the app-managed `tools` directory

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

The output is written to `artifacts/publish/win-x64`. Add the external tools to these application-relative paths before conversion:

- `tools/ffmpeg/ffmpeg.exe`
- `tools/ffmpeg/ffprobe.exe`
- `tools/yt-dlp/yt-dlp.exe`

Settings and history are stored in `%LOCALAPPDATA%\VideoToMp3`; logs are stored in `%LOCALAPPDATA%\VideoToMp3\logs`. No developer-machine paths are required at runtime.

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
