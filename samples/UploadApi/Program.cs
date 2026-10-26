using PixelForge;
using PixelForge.Processing;

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = 20 * 1024 * 1024);
var app = builder.Build();

app.MapGet("/", () => "PixelForge UploadApi. POST an image as form field 'file' to /thumbnail.");

app.MapPost("/thumbnail", async (IFormFile file, CancellationToken cancellationToken) =>
{
    var thumbnail = new Size(200, 200);
    var options = new DecoderOptions
    {
        MaxPixels = 40_000_000,       // reject anything above ~40 MP before allocating
        MaxBytes = 20 * 1024 * 1024,
        TargetSize = thumbnail,       // JPEG/WebP decode straight to a small size
    };

    try
    {
        await using var input = file.OpenReadStream();
        using var image = await Image.LoadAsync(input, options, cancellationToken);
        image.Mutate(x => x
            .AutoOrient()
            .Resize(new ResizeOptions { Size = thumbnail, Mode = ResizeMode.Max }));

        var output = new MemoryStream();
        await image.SaveAsWebpAsync(output, quality: 80, cancellationToken: cancellationToken);
        output.Position = 0;
        return Results.File(output, "image/webp");
    }
    catch (ImageTooLargeException e)
    {
        return Results.Problem(e.Message, statusCode: StatusCodes.Status413PayloadTooLarge);
    }
    catch (ImageFormatException e)
    {
        return Results.Problem(e.Message, statusCode: StatusCodes.Status400BadRequest);
    }
}).DisableAntiforgery();

app.Run();
