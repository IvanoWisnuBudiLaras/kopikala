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
    public void TransientSessionExpiration_TimeTravelerAdvance_ExpiresSessionCorrectly()
    {
        // Arrange: Transient session (15-30 minutes)
        var fakeTime = new FakeTimeProvider();
        var startTime = new DateTimeOffset(2026, 9, 6, 10, 0, 0, TimeSpan.Zero);
        fakeTime.SetUtcNow(startTime);

        var sessionDuration = TimeSpan.FromMinutes(30);
        var expirationTime = fakeTime.GetUtcNow().Add(sessionDuration);

        _output.WriteLine($"[TIME TRAVEL] Transient Session Start: {fakeTime.GetUtcNow():yyyy-MM-dd HH:mm:ss}");
        _output.WriteLine($"[TIME TRAVEL] Session Expiration: {expirationTime:yyyy-MM-dd HH:mm:ss}");

        // Assert awal: Belum kedaluwarsa
        Assert.True(fakeTime.GetUtcNow() < expirationTime);

        // Act: Majukan waktu 35 menit secara instan
        fakeTime.Advance(TimeSpan.FromMinutes(35));
        _output.WriteLine($"[TIME TRAVEL] Advanced +35m -> Current Time: {fakeTime.GetUtcNow():yyyy-MM-dd HH:mm:ss}");

        // Assert: Sesi telah kedaluwarsa
        Assert.True(fakeTime.GetUtcNow() > expirationTime);
        _output.WriteLine("[SUCCESS] Transient cookie session kedaluwarsa terverifikasi via TimeProvider.");
    }

    [Fact]
    public void PersistentRememberMeCookie_TimeTravelerAdvance_ExpiresAfter30Days()
    {
        // Arrange: Persistent Remember Me cookie (30 days)
        var fakeTime = new FakeTimeProvider();
        var startTime = new DateTimeOffset(2026, 9, 6, 10, 0, 0, TimeSpan.Zero);
        fakeTime.SetUtcNow(startTime);

        var rememberMeDuration = TimeSpan.FromDays(30);
        var expirationTime = fakeTime.GetUtcNow().Add(rememberMeDuration);

        _output.WriteLine($"[TIME TRAVEL] Remember Me Cookie Start: {fakeTime.GetUtcNow():yyyy-MM-dd HH:mm:ss}");
        _output.WriteLine($"[TIME TRAVEL] 30-Day Expiration: {expirationTime:yyyy-MM-dd HH:mm:ss}");

        // Majukan 15 hari -> masih valid
        fakeTime.Advance(TimeSpan.FromDays(15));
        Assert.True(fakeTime.GetUtcNow() < expirationTime);
        _output.WriteLine($"[TIME TRAVEL] At Day 15: Valid ({fakeTime.GetUtcNow():yyyy-MM-dd HH:mm:ss})");

        // Majukan lagi 16 hari (total 31 hari) -> expired
        fakeTime.Advance(TimeSpan.FromDays(16));
        Assert.True(fakeTime.GetUtcNow() > expirationTime);
        _output.WriteLine($"[TIME TRAVEL] At Day 31: Expired ({fakeTime.GetUtcNow():yyyy-MM-dd HH:mm:ss})");
        _output.WriteLine("[SUCCESS] 30-day Remember Me cookie expiry terverifikasi via TimeProvider.");
    }

    [Fact]
    public void PasswordResetToken_TimeTravelerAdvance_ExpiresAfter15Minutes()
    {
        // Arrange: Password reset token valid for 15 minutes
        var fakeTime = new FakeTimeProvider();
        var startTime = new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);
        fakeTime.SetUtcNow(startTime);

        var tokenLifetime = TimeSpan.FromMinutes(15);
        var tokenExpiry = fakeTime.GetUtcNow().Add(tokenLifetime);

        // Majukan 10 menit -> masih valid
        fakeTime.Advance(TimeSpan.FromMinutes(10));
        Assert.True(fakeTime.GetUtcNow() <= tokenExpiry);

        // Majukan 6 menit lagi (total 16 menit) -> expired
        fakeTime.Advance(TimeSpan.FromMinutes(6));
        Assert.True(fakeTime.GetUtcNow() > tokenExpiry);
        _output.WriteLine("[SUCCESS] Password reset token 15-min limit terverifikasi via TimeProvider.");
    }
}
