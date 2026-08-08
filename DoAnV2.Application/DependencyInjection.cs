using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DoAnV2.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký MediatR và scan tất cả Handlers trong Application layer.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        return services;
    }
}
