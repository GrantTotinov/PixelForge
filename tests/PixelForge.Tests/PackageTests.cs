using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;

namespace PixelForge.Tests;

public sealed class PackageTests : IDisposable
{
    private readonly string _output = Directory.CreateTempSubdirectory("pixelforge-pack-").FullName;

    public void Dispose() => Directory.Delete(_output, recursive: true);

    [Fact]
    public void Package_has_only_skia_dependencies_and_required_files()
    {
        using var package = ZipFile.OpenRead(Pack());
        var entries = package.Entries.Select(e => e.FullName).ToHashSet();
        var nuspec = package.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using var nuspecStream = nuspec.Open();
        var metadata = XDocument.Load(nuspecStream).Root!.Elements().Single(e => e.Name.LocalName == "metadata");
        string Value(string name) => metadata.Elements().Single(e => e.Name.LocalName == name).Value;

        var groups = metadata.Descendants().Where(e => e.Name.LocalName == "group").ToList();
        var group = Assert.Single(groups);
        Assert.Equal("net8.0", group.Attribute("targetFramework")!.Value);
        var dependencies = group.Elements().Select(e => e.Attribute("id")!.Value).Order().ToArray();
        Assert.Equal(["SkiaSharp", "SkiaSharp.NativeAssets.Linux.NoDependencies"], dependencies);

        Assert.Equal("MIT", Value("license"));
        Assert.Equal("icon.png", Value("icon"));
        Assert.Equal("README.md", Value("readme"));
        Assert.Contains("lib/net8.0/PixelForge.dll", entries);
        Assert.Contains("lib/net8.0/PixelForge.xml", entries);
        Assert.Contains("icon.png", entries);
        Assert.Contains("README.md", entries);
    }

    private string Pack()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "PixelForge.slnx")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Repository root not found.");
        }

        var configuration = AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            ? "Release"
            : "Debug";
        var project = Path.Combine(root, "src", "PixelForge", "PixelForge.csproj");
        var start = new ProcessStartInfo("dotnet", ["pack", project, "-c", configuration, "--no-build", "-o", _output])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var error = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"dotnet pack failed:\n{output.Result}\n{error.Result}");

        return Directory.GetFiles(_output, "PixelForge.*.nupkg").Single(p => !p.EndsWith(".snupkg", StringComparison.Ordinal));
    }
}
