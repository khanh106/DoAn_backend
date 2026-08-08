using DoAnV2.Infrastructure.Persistence;
using DoAnV2.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<BlockchainOptions>(builder.Configuration.GetSection(BlockchainOptions.SectionName));
builder.Services.Configure<IpfsOptions>(builder.Configuration.GetSection(IpfsOptions.SectionName));
builder.Services.Configure<WalletOptions>(builder.Configuration.GetSection(WalletOptions.SectionName));
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddSwaggerGen();


var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
{
    opts.UseSqlServer(connectionString, sql =>
        sql.MigrationsAssembly("DoAnV2.Infrastructure"));
    opts.UseSnakeCaseNamingConvention();
    // Tắt cảnh báo PendingModelChangesWarning của EF Core 9
    // (do shadow FK dư thừa từ convention - không ảnh hưởng logic)
    opts.ConfigureWarnings(w => w.Ignore(
        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();      // <-- serve file JSON
    app.UseSwaggerUI();   // <-- giao diện Swagger UI
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
