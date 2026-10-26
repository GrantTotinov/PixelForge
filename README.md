<p align="center"><img src="icon.png" width="96" alt="PixelForge logo"></p>

# PixelForge

**Familiar API, MIT, no license key, safe defaults for user uploads.**

PixelForge is an image processing library for .NET 8+ built on [SkiaSharp](https://github.com/mono/SkiaSharp). It covers the everyday jobs: load an upload, fix its orientation, resize or crop it, and save it as JPEG, PNG or WebP. If you have used ImageSharp, you already know the API.

[![CI](https://github.com/GrantTotinov/PixelForge/actions/workflows/ci.yml/badge.svg)](https://github.com/GrantTotinov/PixelForge/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/PixelForge.svg)](https://www.nuget.org/packages/PixelForge)

```bash
dotnet add package PixelForge
```

## Quick start

```csharp
using PixelForge;
using PixelForge.Processing;

using var image = Image.Load("photo.jpg");
image.Mutate(x => x.AutoOrient().Resize(800, 0));
image.SaveAsWebp("photo.webp", quality: 80);
```

## Why PixelForge

- **MIT, no license key, no build-time checks.** Use it in open source, closed source and CI without registration.
- **Safe by default for uploads.**
  - Decompression bombs are rejected from the header before any pixel memory is allocated (`MaxPixels`, default 100 MP).
  - Input size is capped (`MaxBytes`).
  - EXIF metadata, including GPS location, is never written to output.
  - See [docs/security.md](docs/security.md).
- **Fast thumbnails.** `DecoderOptions.TargetSize` decodes JPEG and WebP directly at reduced size. See [benchmarks](#benchmarks).
- **Runs everywhere .NET runs on servers.** Windows, macOS, Debian/Ubuntu and Alpine containers, x64 and arm64, with no `apt-get` or `apk add`. CI checks every one of these.
- **Two dependencies:** `SkiaSharp` and its Linux native assets.

## Thumbnails from uploads (ASP.NET Core)

```csharp
app.MapPost("/thumbnail", async (IFormFile file, CancellationToken ct) =>
{
    var size = new Size(200, 200);
    var options = new DecoderOptions { MaxPixels = 40_000_000, TargetSize = size };

    try
    {
        await using var input = file.OpenReadStream();
        using var image = await Image.LoadAsync(input, options, ct);
        image.Mutate(x => x.AutoOrient().Resize(new ResizeOptions { Size = size, Mode = ResizeMode.Max }));

        var output = new MemoryStream();
        await image.SaveAsWebpAsync(output, 80, cancellationToken: ct);
        output.Position = 0;
        return Results.File(output, "image/webp");
    }
    catch (ImageTooLargeException) { return Results.StatusCode(413); }
    catch (ImageFormatException) { return Results.BadRequest(); }
}).DisableAntiforgery();
```

Full sample with Dockerfiles for Debian and Alpine: [samples/UploadApi](samples/UploadApi).

## Features

| | |
|---|---|
| **Load** | `Load(path \| Stream \| byte[] \| ReadOnlySpan<byte>)`, `LoadAsync(Stream)`, `Identify` (header only) |
| **Formats in** | PNG, JPEG, WebP, GIF (first frame), BMP, ICO, WBMP |
| **Formats out** | JPEG, PNG, WebP (lossy and lossless) |
| **Operations** | `AutoOrient`, `Resize` (`Crop`, `Pad`, `Max`, `Min`, `Stretch` + anchor), `Crop`, `Rotate`, `Flip`, `Grayscale` |
| **Limits** | `MaxPixels`, `MaxBytes`, `TargetSize` |

Not included: pixel access, drawing and text, metadata editing, animation, TIFF. See the [full list](docs/migrating-from-imagesharp.md#not-supported). Open an issue if you need one of these.

## Coming from ImageSharp?

For most upload and thumbnail code you change only the `using` lines and the encoder calls:

```diff
-using SixLabors.ImageSharp;
-using SixLabors.ImageSharp.Formats.Jpeg;
-using SixLabors.ImageSharp.Processing;
+using PixelForge;
+using PixelForge.Processing;

 using var image = await Image.LoadAsync(stream);
 image.Mutate(x => x.AutoOrient().Resize(new ResizeOptions { Size = new Size(800, 600), Mode = ResizeMode.Max }));
-await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 80 });
+await image.SaveAsJpegAsync(output, 80);
```

The [migration guide](docs/migrating-from-imagesharp.md) has the full mapping table and the differences in behavior.

## Benchmarks

Decode a 4000×3000 JPEG (2.9 MB, quality 90), resize, encode JPEG quality 75 to memory. Each library uses its documented API.

- **Resize:** plain decode, then resize to 800×600.
- **Thumbnail:** fit into 200×200, using each library's decode-time downscaling where it has one:
  - PixelForge and ImageSharp: `TargetSize`.
  - NetVips: `ThumbnailBuffer`.
  - Magick.NET: `jpeg:size`.

| Library | Resize 4000×3000 → 800×600 | Thumbnail 200px | Allocated (managed, thumbnail) |
|---|---:|---:|---:|
| **PixelForge** 0.1 | 158.1 ms | **32.6 ms** | 8.9 KB |
| ImageSharp 3.1.12 | 100.6 ms | 40.7 ms | 58.2 KB |
| NetVips 3.2.0 | **85.6 ms** | 33.3 ms | 9.6 KB |
| Magick.NET-Q8 15.0.0 | 444.3 ms | 36.8 ms | 12.8 KB |

BenchmarkDotNet 0.15.8, .NET 10.0.11, Windows 11, AMD Ryzen 5 5500U (6 cores / 12 threads). Source: [benchmarks/PixelForge.Benchmarks](benchmarks/PixelForge.Benchmarks/ResizeBenchmarks.cs).

How to read this:

- **Thumbnails, the common upload case:** PixelForge is the fastest of the four.
- **Full-size resize:** PixelForge is about 1.6× slower than ImageSharp and 1.8× slower than NetVips. It is still about 2.8× faster than Magick.NET.
  - About 106 of the 158 ms is Skia's single-threaded JPEG decode of the full 12 MP image. ImageSharp and libvips parallelize more of the pipeline.
  - If you know the output size, set `DecoderOptions.TargetSize` to it. The decode then happens at reduced size, which is what makes the thumbnail column fast.
- "Allocated" is managed memory only. Pixel buffers in all four libraries live in native or pooled memory and are not counted.

Run it yourself: `dotnet run -c Release --project benchmarks/PixelForge.Benchmarks -- --filter '*'`.

## Contributing

Issues and pull requests are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) first. Report security problems privately as described in [SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE). SkiaSharp is MIT; Skia is BSD-3-Clause.
