using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Anizaki.Application;
using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Library;
using Anizaki.Application.Features.Library.Contracts;
using Anizaki.Infrastructure;
using Anizaki.Infrastructure.Library;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Anizaki.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the /api/v1/library endpoint group.
/// Exercises the full request pipeline: routing → handler → InMemoryLibraryRepository → response.
/// </summary>
public class LibraryEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LibraryEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/v1/library/categories ────────────────────────────────────────

    [Fact]
    public async Task GetCategories_Returns200_WithFourCategories()
    {
        var response = await _client.GetAsync("/api/v1/library/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CategoriesPayload>();
        Assert.NotNull(payload);
        Assert.Equal(4, payload.Items.Count);
    }

    [Fact]
    public async Task GetCategories_Items_AreOrderedByOrder()
    {
        var response = await _client.GetAsync("/api/v1/library/categories");

        var payload = await response.Content.ReadFromJsonAsync<CategoriesPayload>();
        var orders = payload!.Items.Select(c => c.Order).ToList();
        Assert.Equal(orders.OrderBy(x => x).ToList(), orders);
    }

    [Fact]
    public async Task GetCategories_KienTruc_HasThreeDrawings()
    {
        var response = await _client.GetAsync("/api/v1/library/categories");

        var payload = await response.Content.ReadFromJsonAsync<CategoriesPayload>();
        var kienTruc = payload!.Items.Single(c => c.Slug == "kien-truc");
        Assert.Equal(3, kienTruc.DrawingCount);
    }

    // ── GET /api/v1/library/drawings ─────────────────────────────────────────

    [Fact]
    public async Task GetDrawings_NoFilters_ReturnsAllSixDrawings()
    {
        var response = await _client.GetAsync("/api/v1/library/drawings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<DrawingListPayload>();
        Assert.NotNull(payload);
        Assert.Equal(6, payload.Pagination.TotalItems);
        Assert.Equal(6, payload.Items.Count);
    }

    [Fact]
    public async Task GetDrawings_CategoryFilter_kienTruc_ReturnsThreeItems()
    {
        var response = await _client.GetAsync("/api/v1/library/drawings?category=kien-truc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<DrawingListPayload>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload.Pagination.TotalItems);
        Assert.All(payload.Items, d => Assert.Equal("kien-truc", d.CategorySlug));
    }

    [Fact]
    public async Task GetDrawings_CategoryFilter_NoiThat_ReturnsZeroItems()
    {
        var response = await _client.GetAsync("/api/v1/library/drawings?category=noi-that");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<DrawingListPayload>();
        Assert.NotNull(payload);
        Assert.Equal(0, payload.Pagination.TotalItems);
        Assert.Empty(payload.Items);
        Assert.Equal(0, payload.Pagination.TotalPages);
    }

    [Fact]
    public async Task GetDrawings_SearchByTitle_MatBang_ReturnsTwoItems()
    {
        var response = await _client.GetAsync("/api/v1/library/drawings?search=M%E1%BA%B7t");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<DrawingListPayload>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.Pagination.TotalItems);
    }

    [Fact]
    public async Task GetDrawings_SearchByCode_STR_ReturnsTwoItems()
    {
        var response = await _client.GetAsync("/api/v1/library/drawings?search=STR");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<DrawingListPayload>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.Pagination.TotalItems);
        Assert.All(payload.Items, d => Assert.StartsWith("STR", d.Code));
    }

    [Fact]
    public async Task GetDrawings_PageSize2_ReturnsCorrectPage()
    {
        var responsePage1 = await _client.GetAsync("/api/v1/library/drawings?page=1&pageSize=2");
        var responsePage3 = await _client.GetAsync("/api/v1/library/drawings?page=3&pageSize=2");

        var page1 = await responsePage1.Content.ReadFromJsonAsync<DrawingListPayload>();
        var page3 = await responsePage3.Content.ReadFromJsonAsync<DrawingListPayload>();

        Assert.Equal(3, page1!.Pagination.TotalPages);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page3!.Items.Count);
    }

    [Fact]
    public async Task GetDrawings_InvalidPage0_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/library/drawings?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>();
        Assert.Equal("validation_failed", payload!.Error);
    }

    [Fact]
    public async Task GetDrawings_PageSizeTooLarge_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/library/drawings?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>();
        Assert.Equal("validation_failed", payload!.Error);
    }

    // ── GET /api/v1/library/drawings/{id} ─────────────────────────────────────

    [Fact]
    public async Task GetDrawingDetail_KnownId_Returns200WithCorrectCode()
    {
        var id = "d1000000-0000-0000-0000-000000000001";
        var response = await _client.GetAsync($"/api/v1/library/drawings/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<DrawingDetailPayload>();
        Assert.NotNull(payload);
        Assert.Equal(new Guid(id), payload.Id);
        Assert.Equal("ARC-001", payload.Code);
        Assert.Equal("published", payload.Status);
        Assert.NotNull(payload.FileInfo);
        Assert.Equal("image/png", payload.FileInfo.MimeType);
    }

    [Fact]
    public async Task GetDrawingDetail_UnknownId_Returns404()
    {
        var unknownId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/library/drawings/{unknownId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>();
        Assert.Equal("not_found", payload!.Error);
    }

    [Fact]
    public async Task GetDrawingDetail_CadDrawing_HasUnavailablePreviewInFileInfo()
    {
        var id = "d1000000-0000-0000-0000-000000000003"; // ARC-003 CAD file
        var response = await _client.GetAsync($"/api/v1/library/drawings/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<DrawingDetailPayload>();
        Assert.Equal("unavailable", payload!.FileInfo.PreviewAvailability);
    }

    // ── GET /api/v1/library/drawings/{id}/preview ─────────────────────────────

    [Fact]
    public async Task GetDrawingPreview_ImageAvailable_Returns200WithAvailableState()
    {
        var id = "d1000000-0000-0000-0000-000000000001";
        var response = await _client.GetAsync($"/api/v1/library/drawings/{id}/preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PreviewPayload>();
        Assert.NotNull(payload);
        Assert.Equal("available", payload.PreviewAvailability);
        Assert.Equal("image", payload.PreviewType);
        Assert.NotNull(payload.PreviewUrl);
        Assert.Null(payload.Message);
    }

    [Fact]
    public async Task GetDrawingPreview_PdfAvailable_Returns200WithPdfType()
    {
        var id = "d1000000-0000-0000-0000-000000000002";
        var response = await _client.GetAsync($"/api/v1/library/drawings/{id}/preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PreviewPayload>();
        Assert.Equal("available", payload!.PreviewAvailability);
        Assert.Equal("pdf", payload.PreviewType);
    }

    [Fact]
    public async Task GetDrawingPreview_Unavailable_Returns200WithMessageAndNoUrl()
    {
        var id = "d1000000-0000-0000-0000-000000000003"; // CAD — unavailable
        var response = await _client.GetAsync($"/api/v1/library/drawings/{id}/preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PreviewPayload>();
        Assert.Equal("unavailable", payload!.PreviewAvailability);
        Assert.Null(payload.PreviewType);
        Assert.Null(payload.PreviewUrl);
        Assert.NotNull(payload.Message);
    }

    [Fact]
    public async Task GetDrawingPreview_Generating_Returns200WithGeneratingAndMessage()
    {
        var id = "d1000000-0000-0000-0000-000000000004"; // STR-001 — generating
        var response = await _client.GetAsync($"/api/v1/library/drawings/{id}/preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PreviewPayload>();
        Assert.Equal("generating", payload!.PreviewAvailability);
        Assert.NotNull(payload.Message);
    }

    [Fact]
    public async Task GetDrawingPreview_UnknownId_Returns404()
    {
        var unknownId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/library/drawings/{unknownId}/preview");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>();
        Assert.Equal("not_found", payload!.Error);
    }

    // ── DI registration check ─────────────────────────────────────────────────

    [Fact]
    public void AddApplication_ShouldRegisterLibraryHandlers()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IRequestHandler<GetCategoriesQuery, GetCategoriesResponse>) &&
            d.ImplementationType == typeof(GetCategoriesHandler));

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IRequestHandler<GetDrawingListQuery, GetDrawingListResponse>) &&
            d.ImplementationType == typeof(GetDrawingListHandler));

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IRequestHandler<GetDrawingDetailQuery, GetDrawingDetailResponse>) &&
            d.ImplementationType == typeof(GetDrawingDetailHandler));

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IRequestHandler<GetDrawingPreviewQuery, GetDrawingPreviewResponse>) &&
            d.ImplementationType == typeof(GetDrawingPreviewHandler));
    }

    [Fact]
    public void AddApplication_ShouldRegisterLibraryValidators()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IRequestValidator<GetCategoriesQuery>) &&
            d.ImplementationType == typeof(GetCategoriesQueryValidator));

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IRequestValidator<GetDrawingListQuery>) &&
            d.ImplementationType == typeof(GetDrawingListQueryValidator));

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IRequestValidator<GetDrawingDetailQuery>) &&
            d.ImplementationType == typeof(GetDrawingDetailQueryValidator));

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IRequestValidator<GetDrawingPreviewQuery>) &&
            d.ImplementationType == typeof(GetDrawingPreviewQueryValidator));
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterInMemoryLibraryRepository()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(ILibraryRepository) &&
            d.ImplementationType == typeof(InMemoryLibraryRepository));
    }

    // ── Response record types ─────────────────────────────────────────────────

    private sealed record CategoriesPayload(List<CategoryItem> Items);
    private sealed record CategoryItem(Guid Id, string Name, string Slug, int Order, int DrawingCount);

    private sealed record DrawingListPayload(List<DrawingListItem> Items, PaginationInfo Pagination);
    private sealed record DrawingListItem(Guid Id, string Title, string Code, string CategorySlug, string CategoryName, string Status, string? PreviewUrl);
    private sealed record PaginationInfo(int Page, int PageSize, int TotalItems, int TotalPages);

    private sealed record DrawingDetailPayload(
        Guid Id, string Title, string Code, string? Description,
        string CategorySlug, string CategoryName, string Status,
        FileInfoDetail FileInfo);
    private sealed record FileInfoDetail(string FileName, string MimeType, long SizeBytes,
        string Checksum, string PreviewAvailability);

    private sealed record PreviewPayload(
        Guid DrawingId, string PreviewAvailability,
        string? PreviewType, string? PreviewUrl, string? Message);

    private sealed record ErrorPayload(string Error, string Message, string? CorrelationId);
}
