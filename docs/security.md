# Security

PixelForge is built for images you did not create: user uploads, scraped files, email attachments. This page explains what it protects against by default, what it does not, and how to use it safely.

## Threats and defaults

### Decompression bombs

A 50 KB PNG can declare 50000 × 50000 pixels and make a naive decoder allocate 10 GB.

PixelForge reads the size from the file header and compares it with `DecoderOptions.MaxPixels` **before** allocating any pixel memory. Above the limit it throws `ImageTooLargeException`. The default is 100,000,000 pixels (about 10000 × 10000, 400 MB of RGBA). The test suite checks this with a crafted 50000 × 50000 header.

Pick a limit that fits your use case. For avatars and product photos, 25–40 megapixels is plenty:

```csharp
var options = new DecoderOptions { MaxPixels = 40_000_000 };
```

### Oversized input

`DecoderOptions.MaxBytes` (default 256 MB) caps how much encoded data is read from a file, stream or byte array. For streams, PixelForge stops reading as soon as the limit is passed, so a slow or endless request body cannot fill memory. Also set your web server's request size limit (for example Kestrel `MaxRequestBodySize`); it applies earlier.

### Malformed files

Decoding is done by Skia's codecs (libjpeg-turbo, libpng, libwebp, and Skia's own GIF/BMP/ICO decoders), which Google fuzzes continuously. PixelForge never returns a partially decoded image:

- Not an image, or an unsupported format: `UnknownImageFormatException`.
- Recognized but truncated or corrupt: `InvalidImageContentException`.

All decode errors derive from `ImageFormatException`, so one `catch` maps them to `400 Bad Request`.

### Privacy: metadata stripping

Photos from phones carry EXIF metadata: GPS coordinates, capture time, device model and serial numbers. PixelForge **never writes metadata** when encoding, so none of it reaches your output. Call `AutoOrient()` before saving so the image still displays upright.

### Format confusion

The format comes from the file content, never from the file name or `Content-Type`. Re-encoding every upload (instead of storing the original bytes) also removes polyglot payloads hidden in the file.

## Safe upload recipe

```csharp
app.MapPost("/avatar", async (IFormFile file, CancellationToken ct) =>
{
    var options = new DecoderOptions
    {
        MaxPixels = 40_000_000,
        MaxBytes = 20 * 1024 * 1024,
        TargetSize = new Size(512, 512),
    };

    try
    {
        await using var input = file.OpenReadStream();
        using var image = await Image.LoadAsync(input, options, ct);
        image.Mutate(x => x.AutoOrient().Resize(new ResizeOptions { Size = new Size(512, 512), Mode = ResizeMode.Crop }));

        var output = new MemoryStream();
        await image.SaveAsWebpAsync(output, 80, cancellationToken: ct);
        // store output, never the original upload
        return Results.Ok();
    }
    catch (ImageTooLargeException) { return Results.StatusCode(413); }
    catch (ImageFormatException) { return Results.BadRequest("Not a valid image."); }
});
```

A runnable version is in [`samples/UploadApi`](../samples/UploadApi/Program.cs).

## What PixelForge does not do

- **CPU limits.** A valid 100-megapixel image takes real CPU time to decode and resize. Rate-limit upload endpoints and lower `MaxPixels` to what you need.
- **Concurrency limits.** Each decoded image holds `width × height × 4` bytes. Cap concurrent image jobs (for example with a `SemaphoreSlim`) so peak memory stays bounded.
- **Content scanning.** PixelForge does not detect malware, NSFW content or steganography.
- **Sandboxing.** Decoders run in your process. For hostile, high-volume input, consider processing in a separate worker process or container.

## Native code and updates

The decoders are native code in SkiaSharp. Security fixes in Skia reach you through SkiaSharp releases. PixelForge tracks them with Dependabot and ships a patch release when SkiaSharp updates a decoder. You can also reference a newer `SkiaSharp` package directly in your project; NuGet picks the highest version.

## Reporting a vulnerability

See [SECURITY.md](../SECURITY.md).
