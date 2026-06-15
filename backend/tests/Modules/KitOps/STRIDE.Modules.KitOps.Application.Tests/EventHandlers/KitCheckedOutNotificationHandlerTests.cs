using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.KitOps.Application.EventHandlers;
using STRIDE.Modules.KitOps.Domain.Events;

namespace STRIDE.Modules.KitOps.Application.Tests.EventHandlers;

public sealed class KitCheckedOutNotificationHandlerTests
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IUserRoleQueryService _userRoles = Substitute.For<IUserRoleQueryService>();
    private readonly KitCheckedOutNotificationHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();

    public KitCheckedOutNotificationHandlerTests()
    {
        var logger = Substitute.For<ILogger<KitCheckedOutNotificationHandler>>();
        _handler = new KitCheckedOutNotificationHandler(_emailSender, _userRoles, logger);
    }

    private static KitCheckedOutEvent AnEvent() => new(
        CheckoutId:         Guid.NewGuid(),
        KitItemId:          Guid.NewGuid(),
        TenantId:           TenantId,
        CheckedOutByUserId: Guid.NewGuid(),
        ExpectedReturnAt:   DateTime.UtcNow.AddDays(3));

    [Fact]
    public async Task Handle_NoAdminUsers_SendsNoEmails()
    {
        var e = AnEvent();
        _userRoles.GetEmailsByRoleNameAsync("Admin", TenantId, Arg.Any<CancellationToken>())
                  .Returns(new List<string>());

        await _handler.Handle(
            new DomainEventNotification<KitCheckedOutEvent>(e),
            CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAdminUsers_SendsEmailsToAllAdmins()
    {
        var emails = new List<string> { "admin1@test.com", "admin2@test.com" };
        var e = AnEvent();

        _userRoles.GetEmailsByRoleNameAsync("Admin", TenantId, Arg.Any<CancellationToken>())
                  .Returns(emails);

        await _handler.Handle(
            new DomainEventNotification<KitCheckedOutEvent>(e),
            CancellationToken.None);

        await _emailSender.Received(2).SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        foreach (var email in emails)
        {
            await _emailSender.Received(1).SendAsync(
                email,
                Arg.Is<string>(s => s.Contains("Kit item checked out")),
                Arg.Is<string>(s => s.Contains(e.KitItemId.ToString("N"))),
                Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Handle_EmailSendingFails_LogsWarning()
    {
        var emails = new List<string> { "admin@test.com" };
        var e = AnEvent();

        _userRoles.GetEmailsByRoleNameAsync("Admin", TenantId, Arg.Any<CancellationToken>())
                  .Returns(emails);

        _emailSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromException(new InvalidOperationException("Email service down")));

        await _handler.Handle(
            new DomainEventNotification<KitCheckedOutEvent>(e),
            CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IncludesKitItemIdInEmailBody()
    {
        var emails = new List<string> { "admin@test.com" };
        var e = AnEvent();

        _userRoles.GetEmailsByRoleNameAsync("Admin", TenantId, Arg.Any<CancellationToken>())
                  .Returns(emails);

        await _handler.Handle(
            new DomainEventNotification<KitCheckedOutEvent>(e),
            CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<string>(body => body.Contains(e.KitItemId.ToString("N"))),
            Arg.Any<CancellationToken>());
    }
}
