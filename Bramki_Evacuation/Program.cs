using Bramki_Evacuation.Components;
using Bramki_Evacuation.Dashboard;
using Bramki_Evacuation.Services;
using Bramki_Evacuation.Wcf;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOptions<ApiOptions>()
    .Bind(builder.Configuration.GetSection("Api"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "Api:BaseUrl required")
    .ValidateOnStart();

builder.Services.AddOptions<DashboardOptions>()
    .Bind(builder.Configuration.GetSection("Dashboard"))
    .Validate(o => o.OnsiteZoneId > 0 && o.MusterZoneId > 0, "Dashboard zone IDs required")
    .ValidateOnStart();

builder.Services.AddSingleton<ApiClientFactory>();

builder.Services.AddSingleton<ApiSessionManager>();
builder.Services.AddSingleton<IApiSessionProvider>(sp => sp.GetRequiredService<ApiSessionManager>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ApiSessionManager>());

builder.Services.AddSingleton<IntegrationFacade>();

builder.Services.AddSingleton<EvacStore>();
builder.Services.AddHostedService<EvacPoller>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();