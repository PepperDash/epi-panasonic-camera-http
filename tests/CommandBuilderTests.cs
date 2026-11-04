namespace PepperDash.Essentials.Plugins.Tests;

/// <summary>Command strings, checked against the Panasonic HD Integrated Camera interface specification.</summary>
public class CommandBuilderTests
{
    private static PanasonicCmdBuilder Builder(string? home = null, string? privacy = null) =>
        new(panSpeed: 12, zoomSpeed: 25, tiltSpeed: 12, home, privacy);

    [Theory]
    [InlineData("PowerOnCommand", "cgi-bin/aw_ptz?cmd=%23O1&res=1")]
    [InlineData("PowerOffCommand", "cgi-bin/aw_ptz?cmd=%23O0&res=1")]
    [InlineData("PanStopCommand", "cgi-bin/aw_ptz?cmd=%23P50&res=1")]
    [InlineData("TiltStopCommand", "cgi-bin/aw_ptz?cmd=%23T50&res=1")]
    [InlineData("ZoomStopCommand", "cgi-bin/aw_ptz?cmd=%23Z50&res=1")]
    [InlineData("HomeCommand", "cgi-bin/aw_ptz?cmd=%23APC80008000&res=1")]
    [InlineData("PrivacyCommand", "cgi-bin/aw_ptz?cmd=%23APC00000000&res=1")]
    public void Fixed_Commands(string property, string expected)
    {
        Assert.Equal(expected, (string)typeof(PanasonicCmdBuilder).GetProperty(property)!.GetValue(Builder())!);
    }

    [Fact]
    public void Configured_Home_And_Privacy_Commands_Override_The_Defaults()
    {
        var builder = Builder(home: "APC12341234", privacy: "R05");

        Assert.Equal("cgi-bin/aw_ptz?cmd=%23APC12341234&res=1", builder.HomeCommand);
        Assert.Equal("cgi-bin/aw_ptz?cmd=%23R05&res=1", builder.PrivacyCommand);
    }

    [Fact]
    public void Pan_Tilt_Zoom_Commands_Are_Centered_On_50()
    {
        var builder = Builder();

        Assert.Equal("cgi-bin/aw_ptz?cmd=%23P38&res=1", builder.PanLeftCommand);   // 50 - 12
        Assert.Equal("cgi-bin/aw_ptz?cmd=%23P62&res=1", builder.PanRightCommand);  // 50 + 12
        Assert.Equal("cgi-bin/aw_ptz?cmd=%23Z25&res=1", builder.ZoomOutCommand);
        Assert.Equal("cgi-bin/aw_ptz?cmd=%23Z75&res=1", builder.ZoomInCommand);
    }

    [Theory]
    [InlineData(1, "R00")]
    [InlineData(2, "R01")]
    [InlineData(10, "R09")]
    public void Preset_Recall_Is_Zero_Based_And_Padded(int preset, string expected)
    {
        Assert.Equal($"cgi-bin/aw_ptz?cmd=%23{expected}&res=1", Builder().PresetRecallCommand(preset));
    }

    [Theory]
    [InlineData(1, "M00")]
    [InlineData(10, "M09")]
    public void Preset_Save_Is_Zero_Based_And_Padded(int preset, string expected)
    {
        Assert.Equal($"cgi-bin/aw_ptz?cmd=%23{expected}&res=1", Builder().PresetSaveCommand(preset));
    }

    // Spec 3.1.5.2: #F[Data] 01-49 near, 50 stop, 51-99 far
    [Fact]
    public void Focus_Commands_Default_Speed()
    {
        var builder = Builder();

        Assert.Equal("cgi-bin/aw_ptz?cmd=%23F25&res=1", builder.FocusNearCommand);
        Assert.Equal("cgi-bin/aw_ptz?cmd=%23F75&res=1", builder.FocusFarCommand);
        Assert.Equal("cgi-bin/aw_ptz?cmd=%23F50&res=1", builder.FocusStopCommand);
    }

    [Theory]
    [InlineData(10, "F40", "F60")]
    [InlineData(1, "F49", "F51")]
    [InlineData(49, "F01", "F99")]
    public void Focus_Speed_Sets_Near_And_Far(int speed, string near, string far)
    {
        var builder = Builder();
        builder.FocusSpeed = speed;

        Assert.Equal($"cgi-bin/aw_ptz?cmd=%23{near}&res=1", builder.FocusNearCommand);
        Assert.Equal($"cgi-bin/aw_ptz?cmd=%23{far}&res=1", builder.FocusFarCommand);
    }

    // Spec data values are two digits (01-99): the slowest near speed is "01", not "1"
    [Fact]
    public void Slowest_Speeds_Are_Zero_Padded_To_Two_Digits()
    {
        var builder = new PanasonicCmdBuilder(panSpeed: 49, zoomSpeed: 49, tiltSpeed: 49, null, null);
        builder.FocusSpeed = 49;

        Assert.Equal("cgi-bin/aw_ptz?cmd=%23P01&res=1", builder.PanLeftCommand);
        Assert.Equal("cgi-bin/aw_ptz?cmd=%23T01&res=1", builder.TiltDownCommand);
        Assert.Equal("cgi-bin/aw_ptz?cmd=%23Z01&res=1", builder.ZoomOutCommand);
        Assert.Equal("cgi-bin/aw_ptz?cmd=%23F01&res=1", builder.FocusNearCommand);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(50)]
    [InlineData(99)]
    public void Invalid_Focus_Speed_Falls_Back_To_25(int speed)
    {
        var builder = Builder();
        builder.FocusSpeed = speed;

        Assert.Equal(25, builder.FocusSpeed);
        Assert.Equal("cgi-bin/aw_ptz?cmd=%23F25&res=1", builder.FocusNearCommand);
    }

    // Spec 3.1.5.2 (#D1) and 3.2.1.1 (OSE:69:1, which is an aw_cam command)
    [Fact]
    public void Auto_Focus_Commands()
    {
        var builder = Builder();

        Assert.Equal("cgi-bin/aw_ptz?cmd=%23D11&res=1", builder.AutoFocusOnCommand);
        Assert.Equal("cgi-bin/aw_ptz?cmd=%23D10&res=1", builder.AutoFocusOffCommand);
        Assert.Equal("cgi-bin/aw_ptz?cmd=%23D1&res=1", builder.AutoFocusQueryCommand);
        Assert.Equal("cgi-bin/aw_cam?cmd=OSE:69:1&res=1", builder.OneTouchAutoFocusCommand);
    }
}
