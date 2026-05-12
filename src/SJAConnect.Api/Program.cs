using Serilog;
using SJAConnect.Infrastructure;
using SJAConnect.Modules.Auth;
using SJAConnect.Shared.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddSJAConnectAuth(builder.Configuration);

var modules = new IModule[]
{
    new SJAConnect.Modules.Auth.AuthModule(),
};
foreach (var m in modules)
{
    m.RegisterServices(builder.Services, builder.Configuration);
}

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("Primary")!, name: "sql")
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis");

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.MapGet("/", () => "SJAConnect API");
foreach (var m in modules)
{
    m.MapEndpoints(app);
}

app.Run();

public partial class Program { }
