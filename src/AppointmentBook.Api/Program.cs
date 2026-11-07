using System.Text.Json.Serialization;
using AppointmentBook.Api.Data;
using AppointmentBook.Api.Filters;
using AppointmentBook.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppointmentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AppointmentBook")));

builder.Services.Configure<BookingOptions>(builder.Configuration.GetSection(BookingOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<SlotService>();
builder.Services.AddScoped<AdminKeyFilter>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// The React dev server (Vite) runs on a different origin during development.
const string DevelopmentCorsPolicy = "DevelopmentFrontend";
builder.Services.AddCors(options => options.AddPolicy(
    DevelopmentCorsPolicy,
    policy => policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevelopmentCorsPolicy);

    // Create/upgrade the database automatically in development only.
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppointmentDbContext>().Database.MigrateAsync();
}

app.MapControllers();
app.Run();
