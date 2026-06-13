using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Domain.Repositories;
using STRIDE.Modules.Webhooks.Infrastructure.Dispatch;
using STRIDE.Modules.Webhooks.Infrastructure.Persistence;
using STRIDE.Modules.Webhooks.Infrastructure.ReadModels;
using STRIDE.Modules.Webhooks.Infrastructure.Security;

namespace STRIDE.Modules.Webhooks.Infrastructure;

public static class WebhooksInfrastructureExtensions
{
    public static IServiceCollection AddWebhooksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<WebhooksDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(WebhooksDbContext).Assembly.FullName)));

        services.AddSingleton<IWebhookSecretProtector, AesWebhookSecretProtector>();
        services.AddScoped<IWebhookSubscriptionRepository, WebhookSubscriptionRepository>();
        services.AddScoped<IWebhookReadService, WebhookReadService>();
        services.AddScoped<IWebhookTester, HttpWebhookTester>();

        // Short, bounded timeout so a slow/unreachable endpoint never hangs the test request.
        services.AddHttpClient(HttpWebhookTester.HttpClientName, client =>
            client.Timeout = TimeSpan.FromSeconds(10));

        return services;
    }
}
