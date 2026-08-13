# Project State

- Status: Published release candidate
- Version: `0.2.0-preview.1`
- Last completed step: 32
- Next: Collect Windows 10/11 results in UAT issue #28, triage feedback, and plan the stable release
- Current blocker: None

## Verified baseline

- Release build: 0 warnings, 0 errors
- Automated tests: 121 passed, 0 failed, 0 skipped
- .NET analyzer validation: passed
- NuGet vulnerability audit, including transitive packages: clear
- Portable Windows x64 package and SHA-256 generation: passed
- Startup without FFmpeg, ffprobe, and yt-dlp: app remains running and reports missing dependencies
- Published ZIP checksum and required-entry verification: passed

## Active acceptance testing

- Tracking issue: https://github.com/phamloina/video-to-mp3/issues/28
- Clean Windows 10 x64: pending community evidence
- Clean Windows 11 x64: pending community evidence

External conversion tools are intentionally not bundled. Users must supply FFmpeg, ffprobe, and yt-dlp in the documented `tools` directories before converting media.
