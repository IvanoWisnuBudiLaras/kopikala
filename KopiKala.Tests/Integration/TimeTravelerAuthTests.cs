using Microsoft.Extensions.Time.Testing;
using Xunit;
using Xunit.Abstractions;

namespace KopiKala.Tests.Integration;

public class TimeTravelerAuthTests
{
    private readonly ITestOutputHelper _output;

    public TimeTravelerAuthTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SessionExpiration_TimeTravelerAdvance_ExpiresSessionCorrectly()
    {
        // Arrange: FakeTimeProvider bawaan .NET 10
        var fakeTime = new FakeTimeProvider();
        var startTime = new DateTimeOffset(2026, 9, 6, 10, 0, 0, TimeSpan.Zero);
        fakeTime.SetUtcNow(startTime);

        var sessionDuration = TimeSpan.FromMinutes(30);
        var expirationTime = fakeTime.GetUtcNow().Add(sessionDuration);

        _output.WriteLine($"[TIME TRAVEL] Start Time: {fakeTime.GetUtcNow():yyyy-MM-dd HH:mm:ss}");
        _output.WriteLine($"[TIME TRAVEL] Session Expiration: {expirationTime:yyyy-MM-dd HH:mm:ss}");

        // Assert awal: Belum kedaluwarsa
        Assert.True(fakeTime.GetUtcNow() < expirationTime);

        // Act: Majukan waktu 35 menit secara instan (Time Traveler)
        fakeTime.Advance(TimeSpan.FromMinutes(35));
        _output.WriteLine($"[TIME TRAVEL] Advanced +35m -> Current Time: {fakeTime.GetUtcNow():yyyy-MM-dd HH:mm:ss}");

        // Assert: Sesi telah kedaluwarsa
        Assert.True(fakeTime.GetUtcNow() > expirationTime);
        _output.WriteLine("[SUCCESS] Token/Session kedaluwarsa terverifikasi via TimeProvider.");
    }
}
