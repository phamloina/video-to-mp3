# Project State

- Status: Published release candidate
- Version: `0.2.0-preview.1`
- Last completed step: 33 (Codex for Open Source application submitted)
- Next: Await the application review while collecting Windows 10/11 results in UAT issue #28
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

## Adoption

- Public tester call: UAT issue #28
- Contributor onboarding: README, CONTRIBUTING.md, issue templates, and `ADOPTION.md`
- Real external users or contributors: pending public evidence
- Policy: no purchased, exchanged, spammed, or fabricated engagement
- Outreach kit: Vietnamese and English tester/contributor messages ready; no external post claimed yet

## Codex for Open Source readiness

- Assessment: `CODEX_OSS_ASSESSMENT.md`
- Technical OSS readiness: strong
- External usage and ecosystem evidence: not yet demonstrated
- Application: submitted to OpenAI on 2026-08-13; review pending
- Application record: `CODEX_OSS_APPLICATION_DRAFT.md`
- Evidence note: the submission explicitly disclosed that stars, forks, downloads, outside UAT responses, and outside contributions were all zero at submission time

External conversion tools are intentionally not bundled. Users must supply FFmpeg, ffprobe, and yt-dlp in the documented `tools` directories before converting media.
