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
