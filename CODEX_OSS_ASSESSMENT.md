# Codex for Open Source Readiness Assessment

Assessment date: 2026-08-13  
Official program page: https://developers.openai.com/community/codex-for-oss

## Official criteria

OpenAI currently says that core maintainers or maintainers of widely used public projects should apply. Projects outside that description may still apply when they play an important ecosystem role and explain why.

The program currently lists:

- six months of ChatGPT Pro with Codex
- conditional Codex Security access
- possible API credits for Codex-powered pull-request review, maintainer automation, release workflows, or other core OSS work

OpenAI does not publish a numeric acceptance score. The assessment below is an internal evidence review, not an OpenAI rubric or acceptance prediction.

## Evidence snapshot

| Area | Status | Public evidence |
| --- | --- | --- |
| Public open-source repository | Ready | Public GitHub repository with MIT license |
| Maintainer ownership | Ready | Repository is owned and maintained by the applicant account |
| Working software | Ready | Published Windows x64 release candidate with ZIP and SHA-256 assets |
| Automated quality gates | Ready | Windows CI builds, tests, publishes, and packages the app |
| Test evidence | Ready | 131 tests pass; Release build has zero warnings and errors |
| Security and governance | Ready | SECURITY, CONTRIBUTING, issue forms, PR template, dependency audit, and redacted logs |
| Release workflow | Ready | Tagged pre-release and automated asset upload workflow |
| Public roadmap and maintenance process | Ready | Roadmap, changelog, project state, UAT, adoption, and outreach documents |
| Codex maintainer workflow evidence | Ready | Public PR history includes review, CI approval, and merge of an outside contribution |
| External UAT | Not yet demonstrated | UAT issue #28 has no outside results yet |
| External contributors | Demonstrated | `floze-the-genius` completed issue #30 through merged PR #36 |
| Usage/adoption | Early | Snapshot: 0 stars, 1 fork, and 0 release-asset downloads |
| Project history | Weak | Repository and first public release were created on the assessment date |
| Ecosystem importance | Unproven | The use case is understandable, but impact is not yet supported by external evidence |

## Strengths

- The repository is public, licensed, reproducible, tested, and releasable.
- The Windows desktop scope is clear and the code has meaningful Core/App/Infrastructure/Test boundaries.
- CI and release automation are real maintainer workflows rather than application-only claims.
- Security boundaries explicitly reject DRM bypass, credentials, telemetry, and sensitive-log disclosure.
- The project has transparent UAT and contributor entry points.
- The contributor path produced a real first-time contribution: PR #36 passed Windows CI, received maintainer approval, and was merged.

## Material gaps

1. No real-user evidence exists yet: no release downloads or UAT replies.
2. The repository is too new to show sustained maintenance.
3. Public ecosystem impact has not been demonstrated.

## Recommendation

The application was submitted on 2026-08-13 with its zero-adoption snapshot disclosed. Do not submit a duplicate application solely because PR #36 was merged. Continue accumulating public evidence and provide an update only if OpenAI requests one or a future official workflow supports it.

Ongoing evidence targets (maintainer guidance, not official OpenAI thresholds):

- at least three independent Windows UAT results
- at least one reproducible external bug report, review, or pull request (achieved by PR #36)
- non-zero verified release downloads
- two to four weeks of visible issue triage and maintenance activity
- public outreach links recorded in `OUTREACH_KIT.md`
- an application profile linking release, CI, UAT, security, and Codex-assisted maintainer workflows

The submitted application truthfully explained a potential ecosystem role despite low usage, as the official program page permits. Later evidence must remain clearly separated from the facts available at submission time.

## Next review

Re-run this assessment after external UAT evidence arrives or when OpenAI requests an application update. Refresh all GitHub metrics at that time; do not reuse this snapshot as a current claim.
