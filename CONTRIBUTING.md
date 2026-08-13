# Contributing to Video To MP3

Thanks for improving the project.

## Before coding

1. Search existing issues and pull requests to avoid duplicate work.
2. Open an issue for substantial features or behavior changes before implementation.
3. Keep each pull request focused on one concern.

## Development rules

- Target .NET 8 and Windows WPF.
- Keep `VideoToMp3.Core` independent of WPF, file-system, and process APIs.
- Use async APIs and `CancellationToken` for long-running work.
- Do not invoke FFmpeg or yt-dlp through a shell command string; use `ProcessStartInfo.ArgumentList`.
- Never add DRM-bypass, cookie extraction, credential collection, telemetry, or secrets.
- Add or update focused tests for behavior changes.

## Validation

```powershell
& "C:\Program Files\dotnet\dotnet.exe" build VideoToMp3.sln --configuration Debug
& "C:\Program Files\dotnet\dotnet.exe" test VideoToMp3.sln --configuration Debug
```

## Pull requests

Use the pull-request template. Explain the user-visible change, tests performed, and dependency or security impact. Maintainers may request changes before merge.
