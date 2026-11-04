using Newtonsoft.Json.Linq;
using PepperDash.Essentials.Core.Config;

namespace PepperDash.Essentials.Plugins.Tests;

/// <summary>Real JSON through the real config class.</summary>
public class ConfigDeserializationTests
{
    private static DeviceConfig ConfigWith(string propertiesJson) => new()
    {
        Key = "camera-1",
        Name = "Camera 1",
        Type = "panasonicHttpCamera",
        Group = "cameras",
        Properties = JObject.Parse(propertiesJson)
    };

    private static JObject ExampleConfig() =>
        JObject.Parse(File.ReadAllText(Path.Combine(AssemblyFixture.RepoRoot, "exampleConfig.json")));

    [Fact]
    public void Full_Config_Deserializes()
    {
        var config = PanasonicCameraPropsConfig.FromDeviceConfig(ConfigWith(@"{
            ""control"": { ""method"": ""http"", ""tcpSshProperties"": { ""address"": ""10.0.0.5"", ""port"": 80 } },
            ""communicationMonitor"": { ""pollInterval"": 10000, ""pollString"": ""cgi-bin/aw_ptz?cmd=%23O&res=1"", ""timeToWarning"": 120000, ""timeToError"": 60000 },
            ""presets"": [ { ""id"": 1, ""name"": ""Wide"" }, { ""id"": 2, ""name"": ""Zoom"" } ],
            ""panSpeed"": 20, ""tiltSpeed"": 21, ""zoomSpeed"": 22, ""focusSpeed"": 23,
            ""homeCommand"": ""APC80008000"", ""privacyCommand"": ""APC00000000"", ""pacing"": 200
        }"));

        Assert.Equal("http", config.Control.Method);
        Assert.Equal("10.0.0.5", config.Control.TcpSshProperties.Address);
        Assert.Equal("cgi-bin/aw_ptz?cmd=%23O&res=1", config.CommunicationMonitor.PollString);
        Assert.Equal(new[] { "Wide", "Zoom" }, config.Presets.Select(p => p.Name));
        Assert.Equal(20, config.PanSpeed);
        Assert.Equal(21, config.TiltSpeed);
        Assert.Equal(22, config.ZoomSpeed);
        Assert.Equal(23, config.FocusSpeed);
        Assert.Equal(200, config.Pacing);
        Assert.Equal("APC80008000", config.HomeCommand);
    }

    [Fact]
    public void Missing_Optional_Values_Default_To_Zero_So_The_Plugin_Defaults_Apply()
    {
        var config = PanasonicCameraPropsConfig.FromDeviceConfig(ConfigWith(@"{
            ""control"": { ""method"": ""http"", ""tcpSshProperties"": { ""address"": ""10.0.0.5"" } }
        }"));

        Assert.Equal(0, config.Pacing);
        Assert.Equal(0, config.FocusSpeed);
        Assert.Empty(config.Presets);
        Assert.Null(config.CommunicationMonitor);
    }

    [Fact]
    public void Example_Config_Deserializes()
    {
        var example = ExampleConfig();
        var config = PanasonicCameraPropsConfig.FromDeviceConfig(ConfigWith(example["properties"]!.ToString()));

        Assert.Equal("http", config.Control.Method);
        Assert.False(string.IsNullOrEmpty(config.Control.TcpSshProperties.Address));
        Assert.NotEmpty(config.Presets);
    }

    // Catches a key in the documented example that the config class would silently ignore
    [Fact]
    public void Every_Example_Config_Key_Maps_To_A_Config_Property()
    {
        var names = typeof(PanasonicCameraPropsConfig).GetProperties()
            .SelectMany(p => new[] { p.Name }.Concat(
                p.GetCustomAttributes(typeof(Newtonsoft.Json.JsonPropertyAttribute), false)
                 .Cast<Newtonsoft.Json.JsonPropertyAttribute>().Select(a => a.PropertyName!)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var keys = ((JObject)ExampleConfig()["properties"]!).Properties().Select(p => p.Name);

        foreach (var key in keys)
            Assert.True(names.Contains(key), $"exampleConfig.json key '{key}' does not map to a PanasonicCameraPropsConfig property");
    }

    [Fact]
    public void Example_Config_Poll_String_Is_A_Real_Camera_Command()
    {
        var pollString = (string?)ExampleConfig()["properties"]!["communicationMonitor"]!["pollString"];

        Assert.StartsWith("cgi-bin/aw_ptz?cmd=", pollString);
    }
}
