using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace PepperDash.Essentials.Plugins.Tests;

/// <summary>HttpCommandQueue against a real local HTTP listener (no camera, no Crestron runtime).</summary>
public class HttpCommandQueueTests
{
    private sealed class TestServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly Func<string, (int Status, string Body)> _respond;

        public ConcurrentQueue<(string Url, long Timestamp)> Requests { get; } = new();
        public string Host { get; }

        public TestServer(Func<string, (int Status, string Body)>? respond = null)
        {
            _respond = respond ?? (_ => (200, "ok"));

            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            Host = $"localhost:{port}";
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();
            _ = Task.Run(Loop);
        }

        private async Task Loop()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try { context = await _listener.GetContextAsync(); }
                catch { return; }

                var url = context.Request.RawUrl ?? string.Empty;
                Requests.Enqueue((url, Stopwatch.GetTimestamp()));

                var (status, body) = _respond(url);
                var bytes = System.Text.Encoding.UTF8.GetBytes(body);
                context.Response.StatusCode = status;
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
            }
        }

        public void Dispose() => _listener.Close();
    }

    private static async Task WaitFor(Func<bool> condition, int timeoutMs = 5000)
    {
        var deadline = Stopwatch.StartNew();
        while (!condition())
        {
            if (deadline.ElapsedMilliseconds > timeoutMs)
                throw new TimeoutException("Condition not met in time");
            await Task.Delay(10);
        }
    }

    [Fact]
    public void Default_Pacing_Is_The_Spec_Recommended_130ms()
    {
        Assert.Equal(130, HttpCommandQueue.DefaultPacingMs);
    }

    [Fact]
    public async Task Commands_Are_Sent_In_The_Order_They_Were_Enqueued()
    {
        using var server = new TestServer();
        using var queue = new HttpCommandQueue(server.Host, "cam", pacingMs: 0);

        var paths = Enumerable.Range(1, 8).Select(i => $"cmd{i}").ToList();
        foreach (var path in paths)
            queue.EnqueueCmd(path);

        await WaitFor(() => server.Requests.Count == paths.Count);

        Assert.Equal(paths.Select(p => "/" + p), server.Requests.Select(r => r.Url));
    }

    [Fact]
    public async Task Pacing_Keeps_A_Minimum_Gap_Between_Command_Starts()
    {
        const int pacing = 150;
        using var server = new TestServer();
        using var queue = new HttpCommandQueue(server.Host, "cam", pacing);

        for (var i = 0; i < 4; i++)
            queue.EnqueueCmd($"cmd{i}");

        await WaitFor(() => server.Requests.Count == 4, timeoutMs: 8000);

        var stamps = server.Requests.Select(r => r.Timestamp).ToList();
        for (var i = 1; i < stamps.Count; i++)
        {
            var gapMs = (stamps[i] - stamps[i - 1]) * 1000.0 / Stopwatch.Frequency;
            // arrival times carry some scheduling jitter, so allow a little under the configured gap
            Assert.True(gapMs >= pacing - 25, $"gap {i} was {gapMs:F0} ms, expected about {pacing} ms or more");
        }
    }

    [Fact]
    public async Task Successful_Response_Raises_ResponseReceived_With_Status_And_Content()
    {
        using var server = new TestServer(_ => (200, "p1"));
        using var queue = new HttpCommandQueue(server.Host, "cam", pacingMs: 0);
        var received = new ConcurrentQueue<HttpResponse>();
        queue.ResponseReceived += (_, response) => received.Enqueue(response);

        queue.EnqueueCmd("cgi-bin/aw_ptz?cmd=%23O&res=1");
        await WaitFor(() => !received.IsEmpty);

        Assert.True(received.TryDequeue(out var response));
        Assert.Equal(200, response!.StatusCode);
        Assert.Equal("p1", response.Content);
        Assert.Equal("/cgi-bin/aw_ptz?cmd=%23O&res=1", server.Requests.Single().Url);
    }

    [Fact]
    public async Task Non_Success_Response_Does_Not_Raise_ResponseReceived_And_Queue_Continues()
    {
        using var server = new TestServer(url => url == "/bad" ? (500, "boom") : (200, "good"));
        using var queue = new HttpCommandQueue(server.Host, "cam", pacingMs: 0);
        var received = new ConcurrentQueue<HttpResponse>();
        queue.ResponseReceived += (_, response) => received.Enqueue(response);

        queue.EnqueueCmd("bad");
        queue.EnqueueCmd("next");
        await WaitFor(() => !received.IsEmpty);

        var response = Assert.Single(received);
        Assert.Equal("good", response.Content);
    }

    [Fact]
    public async Task Unreachable_Device_Does_Not_Throw_Or_Stop_The_Queue()
    {
        // Nothing is listening on this port
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var closedPort = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        using var queue = new HttpCommandQueue($"localhost:{closedPort}", "cam", pacingMs: 0);

        var ex = Record.Exception(() =>
        {
            queue.EnqueueCmd("one");
            queue.EnqueueCmd("two");
        });
        await Task.Delay(300);

        Assert.Null(ex);
    }

    [Fact]
    public void Enqueue_After_Dispose_Is_Ignored()
    {
        var queue = new HttpCommandQueue("localhost:1", "cam", pacingMs: 0);
        queue.Dispose();

        var ex = Record.Exception(() => queue.EnqueueCmd("late"));

        Assert.Null(ex);
        Assert.True(queue.Disposed);
    }

    [Fact]
    public void Dispose_Twice_Is_Safe()
    {
        var queue = new HttpCommandQueue("localhost:1", "cam");

        queue.Dispose();

        Assert.Null(Record.Exception(queue.Dispose));
    }

    [Fact]
    public void Empty_Path_Is_Rejected()
    {
        using var queue = new HttpCommandQueue("localhost:1", "cam");

        Assert.Throws<ArgumentException>(() => queue.EnqueueCmd(""));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Empty_Hostname_Is_Rejected(string? hostname)
    {
        Assert.Throws<ArgumentException>(() => new HttpCommandQueue(hostname!, "cam"));
    }

    [Fact]
    public void Key_Uses_The_Parent_Device_Key()
    {
        using var queue = new HttpCommandQueue("localhost:1", "camera-1");

        Assert.Equal("http-camera-1", queue.Key);
    }
}
