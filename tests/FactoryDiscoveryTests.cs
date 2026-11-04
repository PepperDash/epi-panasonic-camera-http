namespace PepperDash.Essentials.Plugins.Tests;

public class FactoryDiscoveryTests
{
    [Fact]
    public void Assembly_Loads_Successfully()
    {
        Assert.NotNull(AssemblyFixture.PluginAssembly);
    }

    [Fact]
    public void Assembly_Name_Matches_Expected()
    {
        Assert.Equal(AssemblyFixture.PluginAssemblyName, AssemblyFixture.PluginAssembly.GetName().Name);
    }

    // The shared CI workflow fails the build when the package id differs from the repo-derived name:
    // "PepperDash.Essentials.Plugins." + repoName minus "epi-", dashes to dots
    [Fact]
    public void Assembly_Name_Matches_Repo_Derived_Package_Name()
    {
        const string repoName = "epi-panasonic-camera-http";
        var expected = "PepperDash.Essentials.Plugins." + repoName.Replace("epi-", "").Replace('-', '.');

        Assert.Equal(expected, AssemblyFixture.PluginAssembly.GetName().Name, ignoreCase: true);
    }

    [Fact]
    public void Factory_Count_Matches_Expected()
    {
        Assert.Single(AssemblyFixture.FindFactoryTypes());
    }

    [Theory]
    [InlineData("PanasonicCameraFactory")]
    public void Factory_Exists_ByName(string factoryName)
    {
        Assert.Contains(AssemblyFixture.FindFactoryTypes(), t => t.Name == factoryName);
    }

    [Fact]
    public void All_Factories_Have_Parameterless_Constructor()
    {
        foreach (var factory in AssemblyFixture.FindFactoryTypes())
            Assert.True(factory.GetConstructor(Type.EmptyTypes) != null, $"{factory.Name} needs a parameterless constructor");
    }

    [Fact]
    public void Factory_Builds_The_Camera_Device_Type()
    {
        var factory = Assert.Single(AssemblyFixture.FindFactoryTypes());
        var deviceType = factory.BaseType!.GetGenericArguments().Single();

        Assert.Equal("PanasonicCamera", deviceType.Name);
    }
}
