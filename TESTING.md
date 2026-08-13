# Testing

Run the complete Release test suite from the repository root:

```powershell
dotnet test VideoToMp3.sln -c Release
```

Tests use temporary directories and process doubles unless an explicit media-tool path is supplied. They do not require network access.

## Edge-case coverage

| Scenario | Primary automated coverage |
|---|---|
| One local MP4 | `FFmpegServiceTests.ConvertLocalToMp3Async_ConvertsRealSampleWhenFfmpegIsProvided` |
| Multiple local files | `MainWindowViewModelTests.ChooseFilesCommand_AddsAllSelectedFiles` |
| Local file without audio | `ConversionQueueServiceTests.StartAsync_FailsFileWithoutAudioBeforeConversion` |
| Corrupt local file | `ConversionQueueServiceTests.StartAsync_CorruptLocalFileFailsBeforeConversion` |
| Unicode filename | `InputParserServiceTests.Parse_PreservesUnicodePath` |
| Long filename | `OutputPathResolverTests.ResolveAvailableMp3Path_UsesSanitizedOnlineTitleAndLimitsLength` |
| Valid URL | `YtDlpServiceTests.ProbeAsync_ReadsVideoMetadataFromJson` |
| Invalid or unsupported URL | `YtDlpServiceTests.ProbeAsync_RejectsInvalidOrUnsafeUrlSchemes` |
| Network interruption | `YtDlpServiceTests.DownloadAsync_NetworkInterruptionReturnsStructuredFailure` |
| Cancel download | `ConversionQueueServiceTests.Cancel_OnlineDownload_CleansTemporaryDirectory` |
| Cancel conversion | `ConversionQueueServiceTests.Cancel_ActiveJob_ContinuesWithNextWaitingJob` |
| Duplicate output | `OutputPathResolverTests.ResolveAvailableMp3Path_AddsSuffixWithoutOverwriting` |
| Close app while running | `MainWindowViewModelTests.PrepareForShutdown_CancelsRunningQueueWithoutPrompt` |
| Invalid output path | `OutputDirectoryServiceTests.ValidateAndCreate_RejectsInvalidPathWithoutThrowing` |
| Missing ffmpeg | `FFmpegServiceTests.ConvertLocalToMp3Async_ReportsMissingFfmpegWithoutStartingProcess` |
| Missing yt-dlp | `YtDlpServiceTests.ProbeAsync_ReturnsDependencyMissingWithoutStartingProcess` |
| Corrupt settings | `JsonSettingsServiceTests.Load_CorruptFileFallsBackToDefaultsWithoutThrowing` |
| Playlist | `ConversionQueueServiceTests.StartAsync_ExpandsPlaylistIntoBoundedIndependentJobs` |
| Parallel jobs | `ConversionQueueServiceTests.StartAsync_ProcessesJobsInParallelWithoutExceedingConfiguredLimit` |
