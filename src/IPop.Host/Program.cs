using MudBlazor.Services;
using Serilog;
using IPop.Host;
using IPop.Host.Components;
using IPop.Infrastructure;
using IPop.Infrastructure.Authentication;
using IPop.Infrastructure.Persistence;
using IPop.Modules.Auth;
using IPop.Modules.Auth.Application.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp =>
{
    var handler = new HttpClientHandler { UseCookies = false };
    var client = new HttpClient(handler);
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    var ctx = accessor.HttpContext;
    if (ctx is not null)
    {
        var req = ctx.Request;
        client.BaseAddress = new Uri($"{req.Scheme}://{req.Host}");
        if (req.Headers.TryGetValue("Cookie", out var cookie))
        {
            client.DefaultRequestHeaders.Add("Cookie", cookie.ToString());
        }
        client.DefaultRequestHeaders.Add("Origin", $"{req.Scheme}://{req.Host}");
    }
    return client;
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIPopAuth(builder.Configuration, builder.Environment);

var modules = ModuleRegistry.Discover(new[]
{
    typeof(IPop.Modules.Sample.SampleModule).Assembly,
    typeof(IPop.Modules.Auth.AuthModule).Assembly,
    typeof(IPop.Modules.Chat.ChatModule).Assembly,
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

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    try
    {
        await AuthSeeder.SeedAdminAsync(db, passwordHasher, app.Configuration, CancellationToken.None);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Auth seeding skipped — database unavailable: " + ex.Message);
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(IPop.Modules.Auth.AuthModule).Assembly,
        typeof(IPop.Modules.Chat.ChatModule).Assembly);
foreach (var m in modules)
{
    m.MapEndpoints(app);
}

app.Run();
