using System.ComponentModel.DataAnnotations;
using KopiKala.DTOs.Staff;
using Xunit;

namespace KopiKala.Tests.DTOs;

public class StaffDtoTests
{
    [Fact]
    public void WalkInBookingRequestDto_Validation_WorksCorrectly()
    {
        var invalidDto = new WalkInBookingRequestDto
        {
            RepresentativeName = "", // Required
            DurationHours = 5        // Range 1-3
        };

        var context = new ValidationContext(invalidDto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(invalidDto, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(WalkInBookingRequestDto.RepresentativeName)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(WalkInBookingRequestDto.DurationHours)));
    }

    [Fact]
    public void ExtendDurationRequestDto_Validation_WorksCorrectly()
    {
        var invalidDto = new ExtendDurationRequestDto
        {
            AdditionalHours = 5 // Range 1-2
        };

        var context = new ValidationContext(invalidDto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(invalidDto, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ExtendDurationRequestDto.AdditionalHours)));
    }
}
