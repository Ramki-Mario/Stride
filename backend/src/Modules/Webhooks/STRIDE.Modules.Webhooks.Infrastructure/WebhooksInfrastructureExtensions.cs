using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Domain.Repositories;
using STRIDE.Modules.Webhooks.Infrastructure.BackgroundServices;
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
        services.AddScoped<IWebhookDeliveryRepository, WebhookDeliveryRepository>();

        services.AddScoped<IWebhookReadService, WebhookReadService>();
        services.AddScoped<IWebhookDeliveryReadService, WebhookDeliveryReadService>();
        services.AddScoped<IWebhookTester, HttpWebhookTester>();

        // Register the concrete type so WebhookRetryService can resolve it directly
        // (AttemptDeliveryAsync is internal to the Infrastructure assembly).
        services.AddScoped<HttpWebhookDispatcher>();
        services.AddScoped<IWebhookDispatcher>(sp => sp.GetRequiredService<HttpWebhookDispatcher>());

        // Short, bounded timeout so a slow/unreachable endpoint never hangs dispatch or test requests.
        services.AddHttpClient(HttpWebhookTester.HttpClientName, client =>
            client.Timeout = TimeSpan.FromSeconds(10));

        services.AddHostedService<WebhookRetryService>();

        return services;
    }
}
