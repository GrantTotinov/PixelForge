using System.Buffers.Binary;
using SkiaSharp;

namespace PixelForge.Tests;

/// <summary>
/// Generates test images in memory so the suite needs no third-party image files.
/// </summary>
internal static class TestImageFactory
{
    /// <summary>
    /// Opaque image with four colored quadrants: red top-left, green top-right,
    /// blue bottom-left, white bottom-right. Lets tests see orientation changes.
    /// </summary>
    public static SKBitmap Quadrants(int width, int height)
    {
        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        int hw = width / 2, hh = height / 2;
        using var paint = new SKPaint();
        paint.Color = SKColors.Red;
        canvas.DrawRect(0, 0, hw, hh, paint);
        paint.Color = SKColors.Lime;
        canvas.DrawRect(hw, 0, width - hw, hh, paint);
        paint.Color = SKColors.Blue;
        canvas.DrawRect(0, hh, hw, height - hh, paint);
        paint.Color = SKColors.White;
        canvas.DrawRect(hw, hh, width - hw, height - hh, paint);
        return bitmap;
    }

    public static byte[] Encode(SKBitmap bitmap, SKEncodedImageFormat format, int quality = 90)
    {
        using var data = bitmap.Encode(format, quality);
        return data.ToArray();
    }

    public static byte[] Png(int width, int height)
    {
        using var bitmap = Quadrants(width, height);
        return Encode(bitmap, SKEncodedImageFormat.Png);
    }

    public static byte[] Jpeg(int width, int height, int quality = 90)
    {
        using var bitmap = Quadrants(width, height);
        return Encode(bitmap, SKEncodedImageFormat.Jpeg, quality);
    }

    public static byte[] Webp(int width, int height, int quality = 90)
    {
        using var bitmap = Quadrants(width, height);
        return Encode(bitmap, SKEncodedImageFormat.Webp, quality);
    }

    /// <summary>24-bit bottom-up BMP filled with one color.</summary>
    public static byte[] Bmp(int width, int height, SKColor color)
    {
        var rowSize = (width * 3 + 3) & ~3;
        var pixelBytes = rowSize * height;
        var bmp = new byte[54 + pixelBytes];
        bmp[0] = (byte)'B';
        bmp[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(2), bmp.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10), 54);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18), width);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22), height);
        BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(26), 1);
        BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(28), 24);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(34), pixelBytes);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = 54 + (y * rowSize) + (x * 3);
                bmp[i] = color.Blue;
                bmp[i + 1] = color.Green;
                bmp[i + 2] = color.Red;
            }
        }
        return bmp;
    }

    /// <summary>1x1 GIF with two frames: white, then black.</summary>
    public static byte[] TwoFrameGif() =>
    [
        .. "GIF89a"u8,
        0x01, 0x00, 0x01, 0x00, 0x80, 0x00, 0x00,       // 1x1, global palette of 2 colors
        0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00,             // white, black
        .. GifFrame(0x44),                              // LZW: clear, index 0, end
        .. GifFrame(0x4C),                              // LZW: clear, index 1, end
        0x3B,
    ];

    private static byte[] GifFrame(byte lzw) =>
    [
        0x21, 0xF9, 0x04, 0x00, 0x0A, 0x00, 0x00, 0x00, // graphic control: 100 ms delay
        0x2C, 0, 0, 0, 0, 0x01, 0x00, 0x01, 0x00, 0x00, // image descriptor 1x1
        0x02, 0x02, lzw, 0x01, 0x00,
    ];

    /// <summary>ICO with a single PNG-encoded entry.</summary>
    public static byte[] Ico(int size)
    {
        var png = Png(size, size);
        var header = new byte[22];
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(2), 1);   // type: icon
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(4), 1);   // one image
        header[6] = (byte)size;
        header[7] = (byte)size;
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(10), 1);  // planes
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(12), 32); // bits per pixel
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(14), png.Length);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(18), 22);
        return [.. header, .. png];
    }

    /// <summary>
    /// Inserts a minimal EXIF APP1 segment with the given orientation (1-8) right after SOI.
    /// </summary>
    public static byte[] WithExifOrientation(byte[] jpeg, ushort orientation)
    {
        if (jpeg is not [0xFF, 0xD8, ..])
        {
            throw new ArgumentException("Not a JPEG.", nameof(jpeg));
        }

        byte[] app1 =
        [
            0xFF, 0xE1, 0x00, 0x22,                         // APP1, length 34
            (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0,
            (byte)'M', (byte)'M', 0x00, 0x2A,               // big-endian TIFF header
            0x00, 0x00, 0x00, 0x08,                         // offset of IFD0
            0x00, 0x01,                                     // 1 entry
            0x01, 0x12, 0x00, 0x03,                         // tag Orientation, type SHORT
            0x00, 0x00, 0x00, 0x01,                         // count 1
            (byte)(orientation >> 8), (byte)orientation, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,                         // no next IFD
        ];
        return [.. jpeg.AsSpan(0, 2), .. app1, .. jpeg.AsSpan(2)];
    }

    /// <summary>
    /// A PNG whose IHDR claims the given size but carries only a few bytes of pixel data.
    /// Simulates a decompression bomb header.
    /// </summary>
    public static byte[] PngHeaderOnly(int width, int height)
    {
        using var ms = new MemoryStream();
        ms.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);

        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), height);
        ihdr[8] = 8;  // bit depth
        ihdr[9] = 6;  // color type RGBA
        WriteChunk(ms, "IHDR", ihdr);
        WriteChunk(ms, "IDAT", [0x78, 0x9C, 0x63, 0x00, 0x00]);
        WriteChunk(ms, "IEND", []);
        return ms.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, data.Length);
        stream.Write(buffer);

        byte[] typeAndData = [.. type.Select(c => (byte)c), .. data];
        stream.Write(typeAndData);

        BinaryPrimitives.WriteUInt32BigEndian(buffer, Crc32(typeAndData));
        stream.Write(buffer);
    }

    private static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }
        return ~crc;
    }
}
