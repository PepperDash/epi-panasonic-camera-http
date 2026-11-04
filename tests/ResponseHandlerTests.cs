namespace PepperDash.Essentials.Plugins.Tests;

public class ResponseHandlerTests
{
    private static void Send(PanasonicResponseHandler handler, string content) =>
        handler.HandleHttpResponse(null!, new HttpResponse { StatusCode = 200, Content = content });

    // The monitor's #O poll reply is how power state is learned at startup
    [Fact]
    public void Power_Reply_P1_Raises_PoweredOn()
    {
        var handler = new PanasonicResponseHandler();
        var on = 0;
        handler.CameraPoweredOn += (_, _) => on++;

        Send(handler, "p1");

        Assert.Equal(1, on);
    }

    [Fact]
    public void Power_Reply_P0_Raises_PoweredOff()
    {
        var handler = new PanasonicResponseHandler();
        var off = 0;
        handler.CameraPoweredOff += (_, _) => off++;

        Send(handler, "p0");

        Assert.Equal(1, off);
    }

    [Theory]
    [InlineData("d11", true)]
    [InlineData("d10", false)]
    [InlineData("d11\r\n", true)]
    [InlineData(" d10 ", false)]
    public void Auto_Focus_Reply_Raises_AutoFocusChanged(string content, bool expected)
    {
        var handler = new PanasonicResponseHandler();
        bool? seen = null;
        handler.AutoFocusChanged += (_, on) => seen = on;

        Send(handler, content);

        Assert.Equal(expected, seen);
    }

    [Theory]
    [InlineData("axf555")]
    [InlineData("fS99")]
    [InlineData("ER3")]
    [InlineData("d12")]
    [InlineData("")]
    public void Unrelated_Replies_Raise_Nothing(string content)
    {
        var handler = new PanasonicResponseHandler();
        var raised = 0;
        handler.CameraPoweredOn += (_, _) => raised++;
        handler.CameraPoweredOff += (_, _) => raised++;
        handler.AutoFocusChanged += (_, _) => raised++;
        handler.ResponseDeviceInfo += (_, _) => raised++;

        Send(handler, content);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Device_Info_Reply_Is_Parsed()
    {
        var handler = new PanasonicResponseHandler();
        ResponseDeviceInfoEventArgs? info = null;
        handler.ResponseDeviceInfo += (_, args) => info = args;

        Send(handler, "NAME=AW-UE150\r\nMAC=00:80:45:AA:BB:CC\r\nSERIAL=ABC123\r\nVERSION=1.2.3\r\n");

        Assert.NotNull(info);
        Assert.Equal("00:80:45:AA:BB:CC", info!.MacAddress);
        Assert.Equal("ABC123", info.SerialNumber);
        Assert.Equal("1.2.3", info.FirmwareVersion);
    }

    // A getinfo reply must never be mistaken for a power reply
    [Fact]
    public void Device_Info_Reply_Is_Not_Treated_As_A_Power_Reply()
    {
        var handler = new PanasonicResponseHandler();
        var power = 0;
        handler.CameraPoweredOn += (_, _) => power++;
        handler.CameraPoweredOff += (_, _) => power++;

        Send(handler, "MAC=00:80:45:AA:BB:CC\r\nSERIAL=p1p0\r\nVERSION=p1\r\n");

        Assert.Equal(0, power);
    }

    [Fact]
    public void Latest_Reply_Is_Exposed_As_Coms_Feedback()
    {
        var handler = new PanasonicResponseHandler();

        Send(handler, "p1");

        Assert.Equal("p1", handler.ComsFb.StringValue);
    }
}
