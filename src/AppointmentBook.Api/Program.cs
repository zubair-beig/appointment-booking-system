using System.Text.Json.Serialization;
using AppointmentBook.Api.Data;
using AppointmentBook.Api.Filters;
using AppointmentBook.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

// Configuration keys. Every value can be overridden per environment through
// appsettings.{Environment}.json or environment variables, using "__" in place of ":"
// (for example ConnectionStrings__AppointmentBook or Cors__AllowedOrigins__0).
const string CorsAllowedOriginsKey = "Cors:AllowedOrigins";
const string SwaggerIsEnabledKey = "Swagger:IsEnabled";
const string ApplyMigrationsOnStartupKey = "Database:ApplyMigrationsOnStartup";
const string FrontendCorsPolicyName = "Frontend";
const string AdminKeySecuritySchemeId = "AdminKey";

var builder = WebApplication.CreateBuilder(args);

// Fail fast with an actionable message instead of a confusing SQL error on the first request.
// Accepts ConnectionStrings:AppointmentBook, or the Supabase-provided POSTGRES_URL_NON_POOLING / POSTGRES_URL.
var appointmentBookConnectionString = builder.Configuration.GetAppointmentBookConnectionString();
if (appointmentBookConnectionString is null)
{
    const string ConnectionStringName = AppointmentBookConfigurationExtensions.ConnectionStringName;
    throw new InvalidOperationException(
        $"No PostgreSQL connection is configured. Set 'ConnectionStrings:{ConnectionStringName}' in " +
        $"appsettings.{builder.Environment.EnvironmentName}.json, the environment variable 'ConnectionStrings__{ConnectionStringName}', " +
        "or POSTGRES_URL_NON_POOLING / POSTGRES_URL (set automatically by the Vercel Supabase integration).");
}

builder.Services.AddDbContext<AppointmentDbContext>(options => options.UseNpgsql(appointmentBookConnectionString));

builder.Services.Configure<BookingOptions>(builder.Configuration.GetSection(BookingOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<SlotService>();
builder.Services.AddScoped<AdminKeyFilter>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Browser origins (the React frontend) allowed to call this API. Empty list = no cross-origin access.
var corsAllowedOrigins = builder.Configuration.GetSection(CorsAllowedOriginsKey).Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(
    FrontendCorsPolicyName,
    policy => policy.WithOrigins(corsAllowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var isSwaggerEnabled = builder.Configuration.GetValue<bool>(SwaggerIsEnabledKey);
if (isSwaggerEnabled)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "AppointmentBook API", Version = "v1" });

        // Lets Swagger UI send the X-Admin-Key header via its "Authorize" button for the admin endpoints.
        var adminKeySecurityScheme = new OpenApiSecurityScheme
        {
            Name = AdminKeyFilter.HeaderName,
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Description = $"Admin API key sent in the {AdminKeyFilter.HeaderName} header (configured as Admin:ApiKey).",
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = AdminKeySecuritySchemeId },
        };
        options.AddSecurityDefinition(AdminKeySecuritySchemeId, adminKeySecurityScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement { [adminKeySecurityScheme] = [] });
    });
}

var app = builder.Build();

app.UseCors(FrontendCorsPolicyName);

if (isSwaggerEnabled)
{
    // JSON at /swagger/v1/swagger.json, UI at /swagger.
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Configuration.GetValue<bool>(ApplyMigrationsOnStartupKey))
{
    // Create/upgrade the database schema at startup (enabled in Development by default).
    using var migrationScope = app.Services.CreateScope();
    await migrationScope.ServiceProvider.GetRequiredService<AppointmentDbContext>().Database.MigrateAsync();
}

app.MapControllers();
app.Run();
