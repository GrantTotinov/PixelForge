# Security policy

## Supported versions

Only the latest release receives security fixes. Before 1.0, that is the latest `0.x` version.

## Reporting a vulnerability

Please do **not** open a public issue.

Report privately through GitHub: **Security → Report a vulnerability** on this repository ([private vulnerability reporting](https://docs.github.com/code-security/security-advisories/guidance-on-reporting-and-writing-information-about-vulnerabilities/privately-reporting-a-security-vulnerability)).

Include:

- the PixelForge and SkiaSharp versions,
- the OS and architecture,
- a minimal input file or code that reproduces the problem,
- the impact you observed (crash, hang, memory growth, wrong output).

You should get a reply within 7 days. Fixes ship as a patch release with a GitHub Security Advisory that credits you, unless you prefer otherwise.

If the bug is in a Skia decoder rather than in PixelForge, we will help report it upstream to SkiaSharp and Skia.

See [docs/security.md](docs/security.md) for what PixelForge protects against by default.
