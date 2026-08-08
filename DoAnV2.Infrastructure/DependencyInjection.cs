using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Infrastructure.Persistence;
using DoAnV2.Infrastructure.Persistence.Repositories;
using DoAnV2.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DoAnV2.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký các service của Infrastructure layer.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // UoW + Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBlockchainTransactionRepository, BlockchainTransactionRepository>();

        // Auth / Wallet / JWT
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IWalletService, WalletService>();

        // Blockchain abstraction - TASK 03 sẽ thay bằng Nethereum impl
        services.AddScoped<IBlockchainService, NoOpBlockchainService>();
        services.AddScoped<IRoleOnChainAssigner, NoOpRoleOnChainAssigner>();

        // Current user from HttpContext (JWT claims)
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}
