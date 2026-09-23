using FluentValidation;
using MediatR;
using WalletSystem.Application.Features.Users.Register;
using WalletSystem.Infrastructure.EventBus.Behaviors;


namespace WalletSystem.Api.Settings.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(RegisterUserHandler).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(MetricsPipelineBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(OpenTelemetryBehavior<,>));
        });


        // Register FluentValidation
        services.AddValidatorsFromAssemblyContaining<RegisterUserCommandValidator>();

        return services;
    }
}

