namespace PepperDash.Essentials.Plugins.Tests;

/// <summary>Config contract checked against the compiled assembly (MetadataLoadContext).</summary>
public class ConfigContractTests
{
    private static Type ConfigType =>
        AssemblyFixture.PluginAssembly.GetType("PepperDash.Essentials.Plugins.PanasonicCameraPropsConfig", throwOnError: true)!;

    [Fact]
    public void Config_Class_Exists()
    {
        Assert.NotNull(ConfigType);
    }

    [Fact]
    public void Config_Has_Parameterless_Constructor()
    {
        Assert.NotNull(ConfigType.GetConstructor(Type.EmptyTypes));
    }

    [Theory]
    [InlineData("Control")]
    [InlineData("CommunicationMonitor")]
    [InlineData("Presets")]
    [InlineData("HomeCommand")]
    [InlineData("PrivacyCommand")]
    public void Config_Exposes_Property(string name)
    {
        Assert.NotNull(ConfigType.GetProperty(name));
    }

    [Theory]
    [InlineData("PanSpeed")]
    [InlineData("TiltSpeed")]
    [InlineData("ZoomSpeed")]
    [InlineData("FocusSpeed")]
    [InlineData("Pacing")]
    public void Config_Numeric_Property_Is_Int(string name)
    {
        Assert.Equal("Int32", ConfigType.GetProperty(name)!.PropertyType.Name);
    }

    [Theory]
    [InlineData("control")]
    [InlineData("communicationMonitor")]
    [InlineData("presets")]
    public void Config_Property_Has_JsonPropertyAttribute(string jsonName)
    {
        var found = ConfigType.GetProperties().Any(p => p.CustomAttributes.Any(a =>
            a.AttributeType.Name == "JsonPropertyAttribute" &&
            a.ConstructorArguments.Any(arg => string.Equals(arg.Value?.ToString(), jsonName, StringComparison.Ordinal))));

        Assert.True(found, $"No [JsonProperty(\"{jsonName}\")] on {ConfigType.Name}");
    }
}
