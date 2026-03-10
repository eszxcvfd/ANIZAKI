using Anizaki.Application.Features.SystemStatus;
using Anizaki.Application.Features.SystemStatus.Contracts;

namespace Anizaki.Application.Tests.Features.SystemStatus;

public class GetSystemStatusQueryValidatorTests
{
    private readonly GetSystemStatusQueryValidator _validator = new();

    [Fact]
    public void Validate_WithNullCorrelationId_ShouldReturnSuccess()
    {
        var query = new GetSystemStatusQuery();

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithWhitespaceCorrelationId_ShouldReturnValidationError()
    {
        var query = new GetSystemStatusQuery("   ");

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("correlationId", error.Field);
        Assert.Equal("correlationId.empty", error.Code);
    }

    [Fact]
    public void Validate_WithCorrelationIdExceedingLimit_ShouldReturnValidationError()
    {
        var correlationId = new string('x', GetSystemStatusQueryValidator.MaxCorrelationIdLength + 1);
        var query = new GetSystemStatusQuery(correlationId);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("correlationId", error.Field);
        Assert.Equal("correlationId.tooLong", error.Code);
    }
}
