# Release Candidate Summary

Version: `0.2.0-preview.1`
QA date: 2026-08-13

## Final checklist

- [x] Clean Release build with warnings treated as errors
- [x] 121 automated tests pass
- [x] .NET analyzers pass at warning severity
- [x] No TODO, FIXME, or HACK markers in source, tests, scripts, or CI
- [x] No known vulnerable direct or transitive NuGet packages
- [x] Failure logs redact sensitive URL values
- [x] Application starts when external conversion tools are absent
- [x] Self-contained Windows x64 portable package builds successfully
- [x] ZIP contains the executable, version, license, notices, documentation, and tool placeholders
- [x] SHA-256 checksum is generated beside the ZIP
- [x] Portable removal process is documented in README

## Release contents

- Local video and supported URL conversion to MP3
- Sequential or bounded-parallel queue with progress, retry, and cancellation
- Metadata, optional cover art, playlist expansion, settings, and history
- Friendly errors, redacted rotating logs, notifications, and light/dark/system themes
- Versioned no-admin Windows x64 portable ZIP

## Known deployment requirement

FFmpeg, ffprobe, and yt-dlp remain external dependencies and are not included in the repository or portable ZIP. Conversion is available after placing the executables in their documented application-relative `tools` paths.
