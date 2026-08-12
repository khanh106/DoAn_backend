using System.Text;
using DoAnV2.Application;
using DoAnV2.Application.Common.Exceptions;
using DoAnV2.Application.Common.Interfaces;
using DoAnV2.Application.Common.Options;
using DoAnV2.Infrastructure;
using DoAnV2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ========== Options binding ==========
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<BlockchainOptions>(builder.Configuration.GetSection(BlockchainOptions.SectionName));
builder.Services.Configure<IpfsOptions>(builder.Configuration.GetSection(IpfsOptions.SectionName));
builder.Services.Configure<WalletOptions>(builder.Configuration.GetSection(WalletOptions.SectionName));
builder.Services.Configure<WalletFundingOptions>(builder.Configuration.GetSection(WalletFundingOptions.SectionName));
builder.Services.Configure<TraceOptions>(builder.Configuration.GetSection(TraceOptions.SectionName));

// ========== DbContext ==========
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
{
    opts.UseSqlServer(connectionString, sql =>
        sql.MigrationsAssembly("DoAnV2.Infrastructure"));
    opts.UseSnakeCaseNamingConvention();
    opts.ConfigureWarnings(w => w.Ignore(
        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// ========== Application + Infrastructure ==========
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// ========== JWT Authentication ==========
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwt = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false; // dev; bật true khi deploy HTTPS
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        };
    });

// ========== Authorization Policies (RBAC) ==========
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("RequireAdmin", p => p.RequireRole("ADMIN"));
    opts.AddPolicy("RequireFarmer", p => p.RequireRole("FARMER"));
    opts.AddPolicy("RequireProcessor", p => p.RequireRole("PROCESSOR"));
    opts.AddPolicy("RequireRetailer", p => p.RequireRole("RETAILER"));
});

// ========== MVC + Swagger ==========
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(opts =>
    {
        // Trả li validation đúng format JSON
        opts.InvalidModelStateResponseFactory = context =>
        {

            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            return new BadRequestObjectResult(new
            {
                status = 400,
                message = "Validation failed",
                errors
            });
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DoAnV2 API",
        Version = "v1"
    });

    // Tránh xung đột schemaId khi 2 DTO trùng tên ở 2 namespace khác nhau
    // (ví dụ: Public.Dtos.FarmAreaDto vs MasterData.Dtos.FarmAreaDto)
    c.CustomSchemaIds(t => t.FullName);

    // Bearer auth cho Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập: Bearer {access_token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ========== Pipeline ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Tự động kiểm tra và thêm cột/bảng vào SQL Server nếu chưa có
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (
                SELECT 1 FROM sys.columns 
                WHERE object_id = OBJECT_ID(N'users') 
                AND name = N'cooperative_profile_info'
            )
            BEGIN
                ALTER TABLE users ADD cooperative_profile_info NVARCHAR(MAX) NULL;
            END

            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'distributors')
            BEGIN
                CREATE TABLE distributors (
                    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    processor_id UNIQUEIDENTIFIER NOT NULL,
                    retailer_id UNIQUEIDENTIFIER NULL,
                    code NVARCHAR(100) NOT NULL,
                    name NVARCHAR(255) NOT NULL,
                    phone NVARCHAR(50) NOT NULL,
                    email NVARCHAR(255) NULL,
                    address NVARCHAR(500) NOT NULL,
                    tax_code NVARCHAR(100) NULL,
                    status NVARCHAR(50) NOT NULL DEFAULT 'ACTIVE',
                    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    updated_at DATETIME2 NULL,
                    is_deleted BIT NOT NULL DEFAULT 0,
                    CONSTRAINT FK_distributors_users_processor_id FOREIGN KEY (processor_id) REFERENCES users(id) ON DELETE NO ACTION,
                    CONSTRAINT FK_distributors_users_retailer_id FOREIGN KEY (retailer_id) REFERENCES users(id) ON DELETE NO ACTION
                );
            END
            ELSE IF NOT EXISTS (
                SELECT 1 FROM sys.columns 
                WHERE object_id = OBJECT_ID(N'distributors') 
                AND name = N'retailer_id'
            )
            BEGIN
                ALTER TABLE distributors ADD retailer_id UNIQUEIDENTIFIER NULL;
            END
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB Auto-Migration Note]: {ex.Message}");
    }
}

// Global Exception Handler
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        var feature = ctx.Features.Get<IExceptionHandlerFeature>();
        var ex = feature?.Error;
        if (ex != null)
        {
            Console.WriteLine($"[GLOBAL ERROR] {ex.GetType().Name}: {ex.Message}");
        }

        var (status, msg) = ex switch
        {
            DomainException de => (de.StatusCode, de.Message),
            _ => (500, app.Environment.IsDevelopment() && ex != null ? ex.Message : "Lỗi hệ thống. Vui lòng thử lại sau.")
        };

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new
        {
            status,
            message = msg,
            error = ex?.GetType().Name
        });
    });
});
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

