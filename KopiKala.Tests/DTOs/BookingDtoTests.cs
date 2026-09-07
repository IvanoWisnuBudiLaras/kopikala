using KopiKala.DTOs.Booking;
using KopiKala.Helpers;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace KopiKala.Tests.DTOs;

public class BookingDtoTests
{
    [Fact]
    public void CreateBookingRequestDto_Validation_WorksCorrectly()
    {
        var validDto = new CreateBookingRequestDto
        {
            TableId = Guid.NewGuid(),
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            TimeslotId = 1,
            DurationHours = 2,
            RepresentativeName = "Ivano Wisnu",
            CustomerPhone = "081234567890",
            PaymentMethod = "TransferBank"
        };

        var validationContext = new ValidationContext(validDto);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(validDto, validationContext, validationResults, true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void CreateBookingRequestDto_InvalidDuration_FailsValidation(int invalidDuration)
    {
        var invalidDto = new CreateBookingRequestDto
        {
            TableId = Guid.NewGuid(),
            BookingDate = DateOnly.FromDateTime(DateTime.Today),
            TimeslotId = 1,
            DurationHours = invalidDuration,
            RepresentativeName = "Ivano",
            CustomerPhone = "081234567890"
        };

        var validationContext = new ValidationContext(invalidDto);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(invalidDto, validationContext, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CreateBookingRequestDto.DurationHours)));
    }

    [Fact]
    public void OrderItemDto_SubTotal_CalculatesAccurately()
    {
        var item = new OrderItemDto
        {
            MenuItemId = Guid.NewGuid(),
            Name = "Kopi Susu",
            Quantity = 3,
            UnitPrice = 22000
        };

        Assert.Equal(66000, item.SubTotal);
    }

    [Fact]
    public void CurrencyHelper_ToRupiah_FormatsCorrectly()
    {
        decimal price = 25000;
        Assert.Equal("Rp 25.000", price.ToRupiah());

        decimal zero = 0;
        Assert.Equal("Rp 0", zero.ToRupiah());
    }

    [Fact]
    public void InvoiceCodeHelper_GenerateInvoiceCode_MatchesExpectedPattern()
    {
        var date = new DateOnly(2026, 9, 7);
        var code = InvoiceCodeHelper.GenerateInvoiceCode(date);

        Assert.StartsWith("INV/20260907/", code);
        Assert.True(code.Length >= 18);
    }

    [Fact]
    public void FileSecurityHelper_ValidatesExtensionsAndFilenames()
    {
        Assert.True(FileSecurityHelper.IsValidImageExtension("receipt.jpg"));
        Assert.True(FileSecurityHelper.IsValidImageExtension("receipt.JPEG"));
        Assert.True(FileSecurityHelper.IsValidImageExtension("receipt.png"));
        Assert.False(FileSecurityHelper.IsValidImageExtension("receipt.pdf"));
        Assert.False(FileSecurityHelper.IsValidImageExtension("malicious.exe"));

        var safeName = FileSecurityHelper.GenerateSafeFileName("my photo.PNG");
        Assert.EndsWith(".png", safeName);
        Assert.DoesNotContain("my photo", safeName);

        Assert.False(FileSecurityHelper.IsValidImageExtension(""));
        Assert.False(FileSecurityHelper.IsValidImageExtension(null!));

        // Magic bytes validation
        byte[] validJpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];
        byte[] validPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        byte[] corrupted = [0x00, 0x00, 0x00, 0x00];

        Assert.True(FileSecurityHelper.ValidateMagicBytes(validJpeg, "test.jpg"));
        Assert.True(FileSecurityHelper.ValidateMagicBytes(validJpeg, "test.jpeg"));
        Assert.True(FileSecurityHelper.ValidateMagicBytes(validPng, "test.png"));
        Assert.False(FileSecurityHelper.ValidateMagicBytes(corrupted, "test.jpg"));
        Assert.False(FileSecurityHelper.ValidateMagicBytes(corrupted, "test.png"));
        Assert.False(FileSecurityHelper.ValidateMagicBytes(null!, "test.jpg"));
        Assert.False(FileSecurityHelper.ValidateMagicBytes(validJpeg, "test.pdf"));
    }
}
