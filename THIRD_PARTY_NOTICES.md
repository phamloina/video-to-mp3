# Third-party tools

The portable package does not redistribute FFmpeg, ffprobe, or yt-dlp binaries.

When a required tool is not installed, the application downloads it at first use from the upstream yt-dlp release or the Windows FFmpeg build linked below. Users must comply with the applicable licenses:

- [FFmpeg](https://ffmpeg.org/) — LGPL/GPL depending on the selected build and configuration.
- [yt-dlp](https://github.com/yt-dlp/yt-dlp) — The Unlicense, with bundled components subject to their own notices.

Downloaded executables are stored outside the release package in `%LOCALAPPDATA%\VideoToMp3\tools`. The application also supports manually supplied executables as documented in `README.md`.
