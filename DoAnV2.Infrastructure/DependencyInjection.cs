using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Infrastructure.Persistence;
using DoAnV2.Infrastructure.Persistence.Repositories;
using DoAnV2.Infrastructure.Services;
using DoAnV2.Infrastructure.Services.Blockchain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DoAnV2.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký các service của Infrastructure layer.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // ============ UoW + Repositories ============
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBlockchainTransactionRepository, BlockchainTransactionRepository>();
        services.AddScoped<IFruitTypeRepository, FruitTypeRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IFarmAreaRepository, FarmAreaRepository>();
        services.AddScoped<IMaterialItemRepository, MaterialItemRepository>();
        services.AddScoped<IInventoryLogRepository, InventoryLogRepository>();
        services.AddScoped<IBatchRepository, BatchRepository>();
        services.AddScoped<IBatchWorkerRepository, BatchWorkerRepository>();
        services.AddScoped<ICultivationLogRepository, CultivationLogRepository>();
        services.AddScoped<IHarvestRepository, HarvestRepository>();

        // ============ Auth / Wallet / JWT ============
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IWalletService, WalletService>();

        // ============ IPFS Storage (TASK 03 - Filebase S3-compatible) ============
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<IpfsOptions>>().Value;

            if (string.IsNullOrWhiteSpace(opts.AccessKeyId)
                || string.IsNullOrWhiteSpace(opts.SecretAccessKey))
                throw new InvalidOperationException(
                    "Ipfs:AccessKeyId / Ipfs:SecretAccessKey chưa được cấu hình (Filebase S3 credential).");

            var creds = new BasicAWSCredentials(opts.AccessKeyId, opts.SecretAccessKey);
            var region = RegionEndpoint.GetBySystemName(
                string.IsNullOrWhiteSpace(opts.Region) ? "auto" : opts.Region);

            var config = new AmazonS3Config
            {
                ServiceURL = opts.Endpoint, // https://s3.filebase.io
                AuthenticationRegion = opts.Region,
                ForcePathStyle = false, // Filebase dùng virtual-hosted style
                SignatureVersion = string.Equals(opts.SignatureVersion, "v4", StringComparison.OrdinalIgnoreCase)
                    ? "4"
                    : "2",
                UseHttp = opts.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
            };

            return new AmazonS3Client(creds, config);
        });
        services.AddScoped<IIpfsService, IpfsService>();

        // ============ Blockchain (TASK 03 - Nethereum) ============
        services.AddSingleton<AbiLoader>();
        services.AddScoped<IRecordBlockchainTransactionService, BlockchainTransactionRecorder>();
        services.AddScoped<IBlockchainService, BlockchainService>();
        services.AddScoped<IRoleOnChainAssigner, BlockchainRoleAssigner>();

        // ============ Current user from HttpContext (JWT claims) ============
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}
