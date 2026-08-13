# Changelog

All notable changes are documented in this file. This project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Initial WPF .NET 8 solution with App, Core, Infrastructure, and Tests projects.
- Open-source governance files, issue forms, pull-request template, and CI workflow.
- Responsive WPF main-window shell with source input, output settings, queue, and overall progress.
- Observable conversion-job domain model, source/status enums, and media metadata contract.
- Multiline input parser for mixed local paths and HTTP/HTTPS URLs with validation and batch deduplication.
- Multi-file picker, drag-and-drop ingestion, queue binding, and duplicate protection across existing jobs.
- Output-folder selection with safe directory creation and MP3 bitrate options from 128 to 320 kbps.
- App-managed FFmpeg, ffprobe, and yt-dlp dependency resolution with version diagnostics and missing-tool status.
- JSON-based ffprobe analyzer for local duration, audio streams, container, title, and structured failures.
- Asynchronous local-video to MP3 conversion with bitrate selection, collision-safe output paths, cancellation, and structured FFmpeg errors.
- Machine-readable local conversion progress with duration-based percentages, throttling, and UI-context-safe job updates.
- Sequential conversion queue with Start All, local probing, dynamic enqueue policy, and explicit completed/failed job handling.
- Per-job and queue-wide cancellation with process-tree termination, canceled-state handling, and partial MP3 cleanup.
- Status-aware job actions for retry, cancel, remove, opening output files/folders, copying sources, and viewing failures.
- Structured yt-dlp URL probing for title, duration, thumbnail, extractor, playlist detection, and classified failures.
- Online URL-to-MP3 pipeline with isolated downloads, FFmpeg conversion, cancellation, and temporary-file cleanup.
- Stage-aware online progress from yt-dlp and FFmpeg, mapped across analyze, download, conversion, and completion.
- Windows-safe MP3 output naming with Unicode normalization, reserved-name protection, length limits, and duplicate suffixes.

## [0.1.0] - 2026-08-13

### Added

- Initial public project scaffold and documented architecture.

[Unreleased]: https://github.com/phamloina/video-to-mp3/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/phamloina/video-to-mp3/releases/tag/v0.1.0
