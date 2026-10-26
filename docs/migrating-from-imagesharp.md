# Migrating from ImageSharp

PixelForge copies the shape of ImageSharp's most-used API: `Image.Load`, `Mutate`, `Resize`, `SaveAsJpeg`. For typical upload and thumbnail code, migrating means changing the `using` lines and a few encoder calls.

PixelForge is much smaller than ImageSharp. Check [Not supported](#not-supported) before you start.

## 1. Swap the package

```bash
dotnet remove package SixLabors.ImageSharp
dotnet add package PixelForge
```

## 2. Swap the namespaces

| ImageSharp | PixelForge |
|---|---|
| `using SixLabors.ImageSharp;` | `using PixelForge;` |
| `using SixLabors.ImageSharp.Processing;` | `using PixelForge.Processing;` |
| `using SixLabors.ImageSharp.Formats.*;` | remove (encoder settings are method parameters) |
| `using SixLabors.ImageSharp.PixelFormats;` | remove (no pixel types) |

## 3. API mapping

### Loading

| ImageSharp | PixelForge | Note |
|---|---|---|
| `Image.Load(path)` / `(stream)` / `(bytes)` | same | Streams are read to the end and left open. |
| `await Image.LoadAsync(stream)` | same | Use this for ASP.NET Core request bodies. |
| `Image.Load<Rgba32>(...)` | `Image.Load(...)` | Every image is 8-bit RGBA internally. |
| `Image.Load(decoderOptions, stream)` | `Image.Load(stream, decoderOptions)` | **Options come second.** |
| `new DecoderOptions { TargetSize = size }` | same | The decoded image is never smaller than `TargetSize`. Resize afterwards. |
| `Image.Identify(...)` | same | Returns `ImageInfo(Width, Height, Format)`. |
| `image.Metadata.DecodedImageFormat` | `Image.Identify(...).Format` | `Image` itself does not keep the source format. |

### Processing

| ImageSharp | PixelForge | Note |
|---|---|---|
| `image.Mutate(x => ...)` / `image.Clone(x => ...)` | same | |
| `x.Resize(width, height)` | same | A zero width or height keeps the aspect ratio. |
| `x.Resize(new ResizeOptions { Size, Mode, Position, PadColor })` | same | `ResizeMode`: `Crop` (default), `Pad`, `Max`, `Min`, `Stretch`. |
| `AnchorPositionMode.*` | same | |
| `x.Crop(rectangle)` / `x.Crop(width, height)` | same | |
| `x.Rotate(degrees)` / `x.Rotate(RotateMode)` | same | Arbitrary angles grow the canvas; corners are transparent, or `Rotate(degrees, background)`. |
| `x.Flip(FlipMode)` | same | |
| `x.RotateFlip(rotate, flip)` | `x.Rotate(rotate).Flip(flip)` | |
| `x.Grayscale()` | same | BT.709 only. |
| `x.AutoOrient()` | same | **Call it on every upload.** See [Metadata](#metadata-is-always-removed). |
| `x.GetCurrentSize()` | same | |

### Saving

| ImageSharp | PixelForge |
|---|---|
| `image.SaveAsJpeg(stream, new JpegEncoder { Quality = 80 })` | `image.SaveAsJpeg(stream, 80)` |
| `image.SaveAsPng(stream)` | same |
| `image.SaveAsWebp(stream, new WebpEncoder { Quality = 80 })` | `image.SaveAsWebp(stream, 80)` |
| `new WebpEncoder { FileFormat = WebpFileFormatType.Lossless }` | `image.SaveAsWebp(stream, lossless: true)` |
| `image.Save(path)` | same (`.jpg`, `.jpeg`, `.png`, `.webp`) |
| `image.Save(stream, encoder)` | `image.Save(stream, ImageFormat.Jpeg)` or a `SaveAs*` method |
| `...Async(...)` variants | same, with `CancellationToken` |

The default JPEG and WebP quality is 75.

### Primitives

| ImageSharp | PixelForge |
|---|---|
| `Size`, `Rectangle` | same names in `PixelForge` |
| `Color.White`, `Color.Black`, `Color.Transparent` | same |
| `Color.FromRgb(r, g, b)`, `Color.FromRgba(r, g, b, a)` | same |
| Other named colors, `Color.ParseHex` | `new Color(r, g, b)` |

### Exceptions

| ImageSharp | PixelForge |
|---|---|
| `UnknownImageFormatException` | same |
| `InvalidImageContentException` | same |
| `ImageFormatException` | same; the base of all decode errors below |
| — | `ImageTooLargeException`: input exceeded `DecoderOptions.MaxPixels` or `MaxBytes` |

## Behavior differences

### Metadata is always removed

PixelForge never writes EXIF, XMP, ICC or comments. GPS coordinates and camera serial numbers in user uploads do not leak into your output. The flip side: a phone photo saved without `AutoOrient()` loses its orientation flag and shows sideways. Always start with `x.AutoOrient()`.

### Decode limits are on by default

`DecoderOptions.Default` rejects images above 100 megapixels or 256 MB of input with `ImageTooLargeException`. If you process large scans or panoramas, raise the limits:

```csharp
var options = new DecoderOptions { MaxPixels = 400_000_000, MaxBytes = 1L << 30 };
using var image = Image.Load(path, options);
```

### Output pixels differ slightly

PixelForge resamples with Mitchell (down) and Catmull-Rom (up) after box pre-reduction. Output is visually equivalent but not byte-identical to ImageSharp. Tests that compare output hashes need new baselines.

### Colors are converted to sRGB

Images with an embedded profile (for example Display P3 from phones) are converted to sRGB on load and saved untagged.

## Not supported

These have no PixelForge equivalent today. If you need one, open an issue with your use case; that is how features get added.

- `Image<TPixel>`, pixel access (`image[x, y]`, `ProcessPixelRows`, `CopyPixelDataTo`, `DangerousTryGetSinglePixelMemory`).
- Creating blank images (`new Image<Rgba32>(width, height)`).
- Drawing shapes or text (`SixLabors.ImageSharp.Drawing`, `SixLabors.Fonts`).
- Reading or writing metadata (EXIF, IPTC, XMP, ICC profiles).
- Animation and multi-frame images. GIF decodes the first frame only.
- Encoding GIF, BMP, TIFF, TGA, PBM, QOI. Decoding TIFF, TGA, PBM, QOI.
- Effects: blur, sharpen, brightness, contrast, opacity, `DrawImage`. Planned for v0.2 based on demand.
- `ResizeMode.BoxPad` and `Manual`; `ResizeOptions.Sampler`, `Compand`, `PremultiplyAlpha`, `TargetRectangle`.
- `Configuration` and custom codecs.
- ASP.NET Core middleware (`SixLabors.ImageSharp.Web`).
- .NET Framework and .NET Standard. PixelForge needs .NET 8 or later.
