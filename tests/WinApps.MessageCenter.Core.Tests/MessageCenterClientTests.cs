using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using WinApps.MessageCenter.Core;
using Xunit;

namespace WinApps.MessageCenter.Core.Tests;

public sealed class MessageCenterClientTests
{
    [Fact]
    public async Task TrySend_WritesJsonPayload_ToPipe()
    {
        var pipeName = $"WinApps.MessageCenter.Tests.{Guid.NewGuid():N}";
        var notification = new OverlayNotification(
            Title: "T",
            Message: "M",
            DurationMs: 1234);

        using var ready = new ManualResetEventSlim(false);
        OverlayNotification? received = null;

        var serverTask = Task.Run(async () =>
        {
            await using var server = new NamedPipeServerStream(
                pipeName: pipeName,
                direction: PipeDirection.In,
                maxNumberOfServerInstances: 1,
                transmissionMode: PipeTransmissionMode.Byte,
                options: PipeOptions.Asynchronous);

            ready.Set();
            await server.WaitForConnectionAsync();

            using var ms = new MemoryStream();
            var buffer = new byte[4096];
            int read;
            while ((read = await server.ReadAsync(buffer)) > 0)
            {
                ms.Write(buffer, 0, read);
            }

            var json = Encoding.UTF8.GetString(ms.ToArray());
            received = JsonSerializer.Deserialize<OverlayNotification>(json);
        });

        Assert.True(ready.Wait(TimeSpan.FromSeconds(2)));

        var ok = MessageCenterClient.TrySend(pipeName, notification, out var error);
        Assert.True(ok, error);

        await serverTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.NotNull(received);
        Assert.Equal(notification.Title, received!.Title);
        Assert.Equal(notification.Message, received!.Message);
        Assert.Equal(notification.DurationMs, received!.DurationMs);
    }

    [Fact]
    public void TrySend_RejectsBlankTitle()
    {
        var ok = MessageCenterClient.TrySend(
            pipeName: "does-not-matter",
            notification: new OverlayNotification("", "x"),
            out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }
}

