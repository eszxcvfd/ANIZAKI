using Anizaki.Application.Features.Library;
using Anizaki.Application.Features.Library.Contracts;
using Xunit;

namespace Anizaki.Application.Tests.Features.Library;

public class GetDrawingListQueryValidatorTests
{
    private readonly GetDrawingListQueryValidator _validator = new();

    // ── SortBy/SortDir omitted (null) ─────────────────────────────────────────

    [Fact]
    public void Validate_WithNullSortByAndSortDir_ReturnsNoSortError()
    {
        var query = new GetDrawingListQuery(1, 10, null, null, null, null);

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }

    // ── Valid SortBy + SortDir combinations ──────────────────────────────────

    [Theory]
    [InlineData("createdAt", "asc")]
    [InlineData("createdAt", "desc")]
    [InlineData("title", "asc")]
    [InlineData("title", "desc")]
    [InlineData("code", "asc")]
    [InlineData("code", "desc")]
    public void Validate_WithValidSortByAndSortDir_ReturnsNoSortError(string sortBy, string sortDir)
    {
        var query = new GetDrawingListQuery(1, 10, null, null, sortBy, sortDir);

        var result = _validator.Validate(query);

        Assert.DoesNotContain(result.Errors, e => e.Code == "sortBy.invalid");
        Assert.DoesNotContain(result.Errors, e => e.Code == "sortDir.invalid");
    }

    [Theory]
    [InlineData("createdAt", null)]
    [InlineData("title",     null)]
    [InlineData("code",      null)]
    public void Validate_WithValidSortByAndNullSortDir_ReturnsNoSortError(string sortBy, string? sortDir)
    {
        var query = new GetDrawingListQuery(1, 10, null, null, sortBy, sortDir);

        var result = _validator.Validate(query);

        Assert.DoesNotContain(result.Errors, e => e.Code == "sortBy.invalid");
        Assert.DoesNotContain(result.Errors, e => e.Code == "sortDir.invalid");
    }

    [Theory]
    [InlineData(null, "asc")]
    [InlineData(null, "desc")]
    public void Validate_WithNullSortByAndValidSortDir_ReturnsNoSortError(string? sortBy, string sortDir)
    {
        var query = new GetDrawingListQuery(1, 10, null, null, sortBy, sortDir);

        var result = _validator.Validate(query);

        Assert.DoesNotContain(result.Errors, e => e.Code == "sortBy.invalid");
        Assert.DoesNotContain(result.Errors, e => e.Code == "sortDir.invalid");
    }

    // ── Invalid SortBy ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("invalid")]
    [InlineData("CREATEDAT")]   // case-sensitive: must be exact
    [InlineData("CreatedAt")]
    [InlineData("name")]
    [InlineData("date")]
    [InlineData("")]             // empty string is not in the allowed set
    public void Validate_WithInvalidSortBy_ReturnsSortByInvalidError(string sortBy)
    {
        var query = new GetDrawingListQuery(1, 10, null, null, sortBy, null);

        var result = _validator.Validate(query);

        Assert.Contains(result.Errors, e => e.Code == "sortBy.invalid" && e.Field == "sortBy");
    }

    // ── Invalid SortDir ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("ASC")]
    [InlineData("DESC")]
    [InlineData("Asc")]
    [InlineData("ascending")]
    [InlineData("descending")]
    [InlineData("1")]
    [InlineData("")]             // empty string is not in the allowed set
    public void Validate_WithInvalidSortDir_ReturnsSortDirInvalidError(string sortDir)
    {
        var query = new GetDrawingListQuery(1, 10, null, null, "createdAt", sortDir);

        var result = _validator.Validate(query);

        Assert.Contains(result.Errors, e => e.Code == "sortDir.invalid" && e.Field == "sortDir");
    }

    // ── Both invalid ──────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithBothInvalidSortByAndSortDir_ReturnsBothErrors()
    {
        var query = new GetDrawingListQuery(1, 10, null, null, "INVALID_SORT", "INVALID_DIR");

        var result = _validator.Validate(query);

        Assert.Contains(result.Errors, e => e.Code == "sortBy.invalid");
        Assert.Contains(result.Errors, e => e.Code == "sortDir.invalid");
    }

    // ── Sort errors do not interfere with existing validations ────────────────

    [Fact]
    public void Validate_WithInvalidPageAndInvalidSortBy_ReturnsBothErrors()
    {
        var query = new GetDrawingListQuery(0, 10, null, null, "INVALID_SORT", null);

        var result = _validator.Validate(query);

        Assert.Contains(result.Errors, e => e.Code == "page.invalid");
        Assert.Contains(result.Errors, e => e.Code == "sortBy.invalid");
    }
}
