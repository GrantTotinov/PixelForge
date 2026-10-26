using SkiaSharp;

namespace PixelForge.Tests;

public class NativeAssetsTests
{
    [Fact]
    public void Skia_native_library_loads()
    {
        using var bitmap = new SKBitmap(2, 2);

        bitmap.Erase(SKColors.Red);

        Assert.Equal(SKColors.Red, bitmap.GetPixel(1, 1));
    }
}
