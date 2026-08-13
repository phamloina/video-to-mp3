# User Acceptance Testing

Use this checklist to validate a published release on real Windows systems. Track results and defects in [GitHub issue #28](https://github.com/phamloina/video-to-mp3/issues/28).

## Release under test

- Version: `v0.2.0-preview.1`
- Package: `VideoToMp3-0.2.0-preview.1-win-x64-portable.zip`
- Supported systems: Windows 10/11 x64

## Before testing

1. Download the ZIP and `.sha256` file from the release.
2. Verify the ZIP SHA-256 value.
3. Extract it to a writable directory.
4. Start `VideoToMp3.exe` before adding external tools and confirm the dependency guidance is visible.
5. Place FFmpeg, ffprobe, and yt-dlp in the documented application-relative `tools` directories.

## Acceptance matrix

| Area | Windows 10 x64 | Windows 11 x64 | Expected result |
| --- | --- | --- | --- |
| Portable startup | Pending | Pending | Starts without installation or administrator access |
| Missing dependencies | Pending | Pending | Reports missing tools without crashing |
| Local MP4 conversion | Pending | Pending | Produces a playable MP3 with selected bitrate |
| Supported URL conversion | Pending | Pending | Downloads and produces a playable MP3 |
| Cancellation and retry | Pending | Pending | Stops safely, cleans partial output, and can retry |
| Metadata and cover art | Pending | Pending | Writes available metadata without inventing values |
| Settings and history | Pending | Pending | Persist correctly after restart |
| Portable removal | Pending | Pending | Extracted directory can be deleted cleanly |

## Feedback format

Include:

- Windows edition and build
- Release version and checksum result
- Local file or URL source type without private data
- Expected and actual behavior
- Reproduction steps
- Relevant redacted log lines

Never attach copyrighted media, credentials, signed URLs, private history, or unredacted logs.
