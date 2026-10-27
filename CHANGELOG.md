# Changelog

All notable changes are listed here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow [Semantic Versioning](https://semver.org/).

## [0.1.0] - 2026-10-27

First release.

### Added

- `Image.Load` from a path, `Stream`, `byte[]` or `ReadOnlySpan<byte>`, and `Image.LoadAsync(Stream)`.
- `Image.Identify` returns width, height and format from the header, without decoding pixels.
- Decoding of PNG, JPEG, WebP, GIF (first frame), BMP, ICO and WBMP into 8-bit sRGB RGBA.
- `DecoderOptions`:
  - `MaxPixels` (default 100 MP), checked against the header before allocation.
  - `MaxBytes` (default 256 MB) for every input type.
  - `TargetSize` for fast JPEG/WebP decode at reduced size.
- `ImageFormatException` as the base of `UnknownImageFormatException`, `InvalidImageContentException` and `ImageTooLargeException`.
- `Mutate` and `Clone` with:
  - `AutoOrient` for all eight EXIF orientations.
  - `Resize`: exact size, or `ResizeOptions` with `Crop`, `Pad`, `Max`, `Min` and `Stretch`, plus anchor and pad color.
  - `Crop`, `Rotate` (lossless multiples of 90, or any angle with background), `Flip` and `Grayscale`.
- `SaveAsJpeg`, `SaveAsPng`, `SaveAsWebp` (lossy or lossless), and `Save` by file extension or `ImageFormat`. Every method has an async variant.
- Encoding never writes metadata (EXIF, GPS, ICC).
- Runs on Windows, macOS, Linux glibc and Alpine (musl), x64 and arm64, with no extra system packages.

[0.1.0]: https://github.com/GrantTotinov/PixelForge/releases/tag/v0.1.0
