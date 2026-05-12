using MudBlazor.Services;
using Serilog;
using SJAConnect.Host;
using SJAConnect.Host.Components;
using SJAConnect.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddInfrastructure(builder.Configuration);

var modules = ModuleRegistry.Discover(new[]
{
    typeof(SJAConnect.Modules.Sample.SampleModule).Assembly,
    typeof(SJAConnect.Modules.Auth.AuthModule).Assembly,
});
foreach (var m in modules)
{
    m.RegisterServices(builder.Services, builder.Configuration);
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
foreach (var m in modules)
{
    m.MapEndpoints(app);
}

app.Run();
