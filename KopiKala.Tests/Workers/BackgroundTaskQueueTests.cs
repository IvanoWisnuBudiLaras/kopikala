using KopiKala.Workers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KopiKala.Tests.Workers;

public class BackgroundTaskQueueTests
{
    [Fact]
    public async Task QueueBackgroundWorkItemAsync_EnqueuesAndDequeues_InFifoOrder()
    {
        var queue = new BackgroundTaskQueue(10);
        var executionOrder = new List<int>();

        await queue.QueueBackgroundWorkItemAsync(ct =>
        {
            executionOrder.Add(1);
            return ValueTask.CompletedTask;
        });

        await queue.QueueBackgroundWorkItemAsync(ct =>
        {
            executionOrder.Add(2);
            return ValueTask.CompletedTask;
        });

        var workItem1 = await queue.DequeueAsync(CancellationToken.None);
        await workItem1(CancellationToken.None);

        var workItem2 = await queue.DequeueAsync(CancellationToken.None);
        await workItem2(CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, executionOrder);
    }

    [Fact]
    public async Task QueueBackgroundWorkItemAsync_NullWorkItem_ThrowsArgumentNullException()
    {
        var queue = new BackgroundTaskQueue(10);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await queue.QueueBackgroundWorkItemAsync(null!));
    }

    [Fact]
    public async Task QueuedHostedService_ExecutesWorkItems_UntilCancelled()
    {
        var queue = new BackgroundTaskQueue(10);
        var loggerMock = new Mock<ILogger<QueuedHostedService>>();
        var hostedService = new QueuedHostedService(queue, loggerMock.Object);

        var executed = false;
        await queue.QueueBackgroundWorkItemAsync(ct =>
        {
            executed = true;
            return ValueTask.CompletedTask;
        });

        using var cts = new CancellationTokenSource();
        var serviceTask = hostedService.StartAsync(cts.Token);

        // Wait a short time for execution
        await Task.Delay(100);

        Assert.True(executed);

        // Cancel and stop service
        cts.Cancel();
        await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task QueuedHostedService_WhenWorkItemThrows_LogsErrorAndContinues()
    {
        var queue = new BackgroundTaskQueue(10);
        var loggerMock = new Mock<ILogger<QueuedHostedService>>();
        var hostedService = new QueuedHostedService(queue, loggerMock.Object);

        var secondExecuted = false;

        // First item throws
        await queue.QueueBackgroundWorkItemAsync(ct =>
        {
            throw new InvalidOperationException("Test error");
        });

        // Second item should still execute
        await queue.QueueBackgroundWorkItemAsync(ct =>
        {
            secondExecuted = true;
            return ValueTask.CompletedTask;
        });

        using var cts = new CancellationTokenSource();
        var serviceTask = hostedService.StartAsync(cts.Token);

        await Task.Delay(150);

        Assert.True(secondExecuted);

        cts.Cancel();
        await hostedService.StopAsync(CancellationToken.None);
    }
}
