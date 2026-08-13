# Codex for Open Source Application Draft

Status: **Do not submit yet — external evidence gate is not met**  
Last updated: 2026-08-13  
Official program: https://developers.openai.com/community/codex-for-oss

This public draft contains no private contact details. Add applicant identity and contact information only in OpenAI's official form, never in this repository.

## Project

**Name:** Video To MP3  
**Repository:** https://github.com/phamloina/video-to-mp3  
**License:** MIT  
**Maintainer role:** Repository owner and core maintainer  
**Primary language/platform:** C#, .NET 8, WPF, Windows 10/11 x64

## Short project description

Video To MP3 is an open-source Windows desktop application for converting local video files and supported online video URLs to MP3. It provides a responsive queue, bounded parallel conversion, progress, cancellation, retry, metadata, optional cover art, playlist expansion, settings, history, user-friendly errors, and redacted logs. FFmpeg, ffprobe, and yt-dlp are explicit external dependencies; the project does not bypass DRM or collect credentials.

## What problem it addresses

The project provides a transparent, auditable alternative to opaque media-conversion websites and ad-supported download utilities. Processing is local, no telemetry is included, external dependencies are disclosed, and users retain control of output, history, and logs.

The intended ecosystem contribution is a maintainable reference implementation for a Windows WPF application that safely orchestrates FFmpeg, ffprobe, and yt-dlp through asynchronous .NET process APIs. This ecosystem role is plausible but not yet supported by meaningful outside usage; the application must not claim otherwise.

## Current public evidence

- Public MIT repository: https://github.com/phamloina/video-to-mp3
- Release candidate: https://github.com/phamloina/video-to-mp3/releases/tag/v0.2.0-preview.1
- Windows CI: https://github.com/phamloina/video-to-mp3/actions/workflows/ci.yml
- Automated release assets: https://github.com/phamloina/video-to-mp3/actions/workflows/release-assets.yml
- Security policy: https://github.com/phamloina/video-to-mp3/security/policy
- UAT tracker: https://github.com/phamloina/video-to-mp3/issues/28
- First-contribution issue: https://github.com/phamloina/video-to-mp3/issues/30
- Readiness assessment: https://github.com/phamloina/video-to-mp3/blob/main/CODEX_OSS_ASSESSMENT.md

Technical baseline:

- 121 automated tests pass
- Release build completes with zero warnings and zero errors
- direct and transitive NuGet vulnerability audit is clear
- self-contained Windows x64 ZIP and SHA-256 assets are published
- CI uses read-only repository permissions; the release workflow scopes write access to contents

External-evidence snapshot on 2026-08-13:

- 0 stars
- 0 forks
- 0 release-asset downloads
- 0 outside UAT responses
- 0 outside pull requests or reviews

Refresh these metrics before submission. Never replace them with estimates or private claims.

## How Codex has supported maintenance

Codex has been used as a repository-maintenance collaborator for implementation, testing, QA, documentation, GitHub workflow creation, release preparation, browser-assisted pull-request administration, and evidence-based readiness review. Work was delivered through focused branches, CI checks, and public pull requests rather than one unreviewed code dump.

Representative public evidence:

- Final QA and dependency remediation: https://github.com/phamloina/video-to-mp3/pull/24
- Release preparation: https://github.com/phamloina/video-to-mp3/pull/25
- Release-asset automation: https://github.com/phamloina/video-to-mp3/pull/26
- UAT plan: https://github.com/phamloina/video-to-mp3/pull/29
- Contributor/adoption path: https://github.com/phamloina/video-to-mp3/pull/31
- Codex OSS readiness review: https://github.com/phamloina/video-to-mp3/pull/33

The repository's master implementation state, release summary, roadmap, tests, and PR history provide reviewable evidence of this workflow.

## Why six months of ChatGPT Pro with Codex would help

Six months of access would support ongoing maintainer work after the initial release:

- triage reproducible Windows and dependency-specific bug reports
- review outside pull requests and keep changes within security boundaries
- maintain compatibility with .NET, FFmpeg, ffprobe, yt-dlp, and Windows updates
- expand unit and integration coverage for external-process failures and unsafe paths
- prepare stable releases with changelog, checksum, and CI evidence
- keep contributor documentation and good-first-issue scopes current

The request is for sustained maintenance capacity, not for creating artificial adoption or replacing community review.

## Potential Codex Security relevance

Conditional Codex Security access could be useful because the application processes untrusted file paths, media metadata, URLs, thumbnails, and external-process output. High-value review areas include argument injection, unsafe path construction, partial-file cleanup, sensitive log disclosure, URL handling, archive/release integrity, and process-tree cancellation.

The project should request this access as conditional and explain the concrete threat surface. It should not claim that a Codex Security audit has already occurred.

## Potential API-credit use

The application itself does **not** call the OpenAI API. If API credits are requested, the proposed use should be limited to an optional maintainer workflow such as issue classification, pull-request review assistance, or release-note verification. No API-credit request should be made until that workflow has a public design, privacy boundary, human-review step, and cost control.

## Six-month maintenance plan

### Months 1–2

- collect Windows 10/11 UAT evidence
- reproduce and triage reported failures
- merge suitable outside contributions
- publish a patched preview if needed

### Months 3–4

- promote a stable release when acceptance criteria pass
- add regression tests for real user failures
- review external-tool compatibility and release integrity

### Months 5–6

- evaluate sustained usage and contributor health
- improve maintainer automation where it has a clear privacy boundary
- publish a maintenance report with releases, issues, contributors, and verified usage evidence

## Submission gate

Submit only when all statements can be supported by current public links and the following minimum evidence exists. These are maintainer-defined safeguards, not official OpenAI thresholds:

- [ ] At least three independent UAT results are recorded
- [ ] At least one outside bug report, review, or pull request exists
- [ ] Release assets have verified non-zero downloads
- [ ] Public outreach links are recorded in `OUTREACH_KIT.md`
- [ ] The GitHub metrics snapshot has been refreshed
- [ ] Applicant contact details are entered only in the official OpenAI form
- [ ] Every application claim has a public URL or is explicitly labeled as a future plan

## Final concise answer draft

> I am the owner and core maintainer of Video To MP3, a public MIT-licensed Windows desktop application built with C#/.NET 8 and WPF. It provides a transparent local workflow for converting permitted local videos and supported URLs to MP3 using FFmpeg, ffprobe, and yt-dlp, with queueing, cancellation, metadata, history, redacted logs, automated tests, Windows CI, and reproducible release assets. Codex has supported implementation, QA, dependency remediation, release automation, UAT planning, and contributor onboarding through focused public pull requests. Six months of ChatGPT Pro with Codex would help me triage real Windows failures, review outside contributions, maintain compatibility with changing media tools, expand regression coverage, and deliver stable releases. The project is early-stage; current usage and outside-contributor evidence are linked explicitly and are not overstated.

Replace the final sentence with refreshed, quantified evidence before submission if the submission gate passes.
