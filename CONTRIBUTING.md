# Contributing

Thanks for helping. PixelForge stays small on purpose, so please read this before opening a pull request.

## Before you code

- **Bugs:** open an issue with a minimal input file or code sample.
- **Features:** open an issue first and describe the real use case. Features are added when real users need them, not "for completeness".

## Rules

- **Do not read, copy or paraphrase ImageSharp 3+ source code.** It is under the Six Labors Split License. Matching public API names (`Load`, `Mutate`, `Resize`) is fine; implementations must be written from scratch on top of SkiaSharp.
- No new runtime dependencies. The package depends only on `SkiaSharp` and `SkiaSharp.NativeAssets.Linux.NoDependencies`.
- Decoding is a trust boundary: check limits against the header before allocating, dispose every Skia object, never swallow decode errors.
- If SkiaSharp behaves unexpectedly, add a test to `SkiaSharpBehaviorTests` that pins the real behavior.
- Test images must have a known license. Prefer generating them in `TestImageFactory`. Record any file you add in `tests/PixelForge.Tests/TestImages/README.md`.

## Build and test

Requires the .NET 10 SDK.

```bash
dotnet build -warnaserror
dotnet test
dotnet format --verify-no-changes
```

All three must pass. CI runs them on Linux x64/arm64, Windows, macOS and Alpine, and builds the sample in Docker.

Benchmarks (slow; close other apps first):

```bash
dotnet run -c Release --project benchmarks/PixelForge.Benchmarks -- --filter '*'
```

## Commits

Use [Conventional Commits](https://www.conventionalcommits.org/): `feat(resize): ...`, `fix: ...`, `docs: ...`, `test: ...`, `chore: ...`.

## License

By contributing you agree that your contribution is licensed under the MIT License.
