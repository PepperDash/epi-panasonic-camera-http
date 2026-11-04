using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PepperDash.Essentials.Plugins.Tests;

public class FactoryMetadataTests
{
    private const string FactoryName = "PanasonicCameraFactory";

    private static string FactorySource =>
        AssemblyFixture.FindSourceForClass(FactoryName)
        ?? throw new InvalidOperationException($"Source for {FactoryName} not found");

    // Numeric core of the PepperDashEssentials version pinned in the plugin csproj ("3.0.0" for "3.0.0-rc.20")
    private static string PinnedEssentialsCore()
    {
        var csproj = XDocument.Load(Path.Combine(AssemblyFixture.SourceDirectory, "epi-panasonic-camera.4series.csproj"));
        var version = csproj.Descendants("PackageReference")
            .Single(e => (string?)e.Attribute("Include") == "PepperDashEssentials")
            .Attribute("Version")!.Value;

        return Regex.Match(version, @"^\d+\.\d+\.\d+").Value;
    }

    [Fact]
    public void Factory_Sets_MinimumEssentialsFrameworkVersion()
    {
        Assert.Matches(@"MinimumEssentialsFrameworkVersion\s*=", FactorySource);
    }

    // A prerelease suffix makes System.Version throw in the loader, and the plugin is then skipped silently
    [Fact]
    public void MinimumEssentialsFrameworkVersion_Is_Plain_Numeric_And_Matches_Pinned_Essentials()
    {
        var value = Regex.Match(FactorySource, @"MinimumEssentialsFrameworkVersion\s*=\s*""([^""]*)""").Groups[1].Value;

        Assert.Matches(@"^\d+\.\d+\.\d+$", value);
        Assert.Equal(PinnedEssentialsCore(), value);
    }

    [Fact]
    public void Factory_Sets_TypeNames()
    {
        Assert.Matches(@"TypeNames\s*=", FactorySource);
    }

    [Theory]
    [InlineData("panasonicHttpCamera")]
    public void Factory_Source_Contains_TypeName(string typeName)
    {
        Assert.Contains($"\"{typeName}\"", FactorySource);
    }

    [Fact]
    public void No_Duplicate_TypeNames()
    {
        var block = Regex.Match(FactorySource, @"TypeNames\s*=\s*new List<string>\s*\{([^}]*)\}").Groups[1].Value;
        var names = Regex.Matches(block, "\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();

        Assert.NotEmpty(names);
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
