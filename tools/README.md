# Managed media tools

The app automatically downloads missing media tools to `%LOCALAPPDATA%\VideoToMp3\tools` on first use. To supply portable binaries manually, place them in these locations:

```text
tools/ffmpeg/ffmpeg.exe
tools/ffmpeg/ffprobe.exe
tools/yt-dlp/yt-dlp.exe
```

The repository and release ZIP do not redistribute these binaries. Existing tools from this directory, the user-managed directory, Winget, or `PATH` are preferred over downloading.
