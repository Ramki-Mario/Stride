using STRIDE.Modules.Reporting.Application.Queries.GetCompletionTimeAnalytics;

namespace STRIDE.Modules.Reporting.Application.Tests.Queries;

public sealed class GetCompletionTimeAnalyticsQueryValidatorTests
{
    private readonly GetCompletionTimeAnalyticsQueryValidator _sut = new();

    private static readonly Guid     TenantId = Guid.NewGuid();
    private static readonly DateTime From     = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To       = new(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ValidQuery_PassesValidation()
    {
        var result = await _sut.ValidateAsync(
            new GetCompletionTimeAnalyticsQuery(TenantId, From, To));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_FailsValidation()
    {
        var result = await _sut.ValidateAsync(
            new GetCompletionTimeAnalyticsQuery(Guid.Empty, From, To));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task ToDateBeforeFromDate_FailsValidation()
    {
        var result = await _sut.ValidateAsync(
            new GetCompletionTimeAnalyticsQuery(TenantId, To, From));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ToDate");
    }

    [Fact]
    public async Task DateRangeOver366Days_FailsValidation()
    {
        var from  = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var tooBig = from.AddDays(367);
        var result = await _sut.ValidateAsync(
            new GetCompletionTimeAnalyticsQuery(TenantId, from, tooBig));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ExactlyMaxRange_PassesValidation()
    {
        var from    = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var exactly = from.AddDays(366);
        var result  = await _sut.ValidateAsync(
            new GetCompletionTimeAnalyticsQuery(TenantId, from, exactly));
        result.IsValid.Should().BeTrue();
    }
}
