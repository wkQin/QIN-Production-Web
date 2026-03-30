using QIN_Production_Web.Components;
using QIN_Production_Web.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();
builder.Services.AddAuthenticationCore();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
    })
    .AddNegotiate();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<SsoTokenCache>();
builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<EndkontrolleService>();
builder.Services.AddScoped<FehleranalyseService>();
builder.Services.AddScoped<ChargenanalyseService>();
builder.Services.AddScoped<ProduktionslayoutService>();
builder.Services.AddScoped<FertigungsauftraegeService>();
builder.Services.AddScoped<VerwaltungWareneingangService>();
builder.Services.AddScoped<NachrichtenService>();
builder.Services.AddScoped<ActivityLogService>();
// Zeiterfassung Services
builder.Services.AddSingleton(new QIN_Production_Web.Data.Zeiterfassung.ZeiterfassungPolicy());
builder.Services.AddScoped<QIN_Production_Web.Data.Zeiterfassung.ZeiterfassungMath>();
builder.Services.AddScoped<QIN_Production_Web.Data.Zeiterfassung.ZeiterfassungService>();
builder.Services.AddScoped<QIN_Production_Web.Data.Zeiterfassung.ZeiterfassungExcelExportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseStatusCodePagesWithReExecute("/not-found");
});

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapGet("/api/auth/windows", [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.Negotiate.NegotiateDefaults.AuthenticationScheme)] async (HttpContext context, LoginService loginService, SsoTokenCache tokenCache) => 
{
    var windowsUsername = context.User.Identity?.Name;
    if (string.IsNullOrWhiteSpace(windowsUsername))
    {
        return Results.Redirect("/login?ssoError=NoWindowsIdentity");
    }

    var session = await loginService.ADLoginAsync(windowsUsername);
    
    // Fallback: If "DOMAIN\User" fails, try just "User"
    if (session == null && windowsUsername.Contains('\\'))
    {
        var justName = windowsUsername.Split('\\').Last();
        session = await loginService.ADLoginAsync(justName);
    }

    if (session != null)
    {
        var token = tokenCache.StoreSession(session);
        return Results.Redirect($"/login?ssoToken={token}");
    }
    else
    {
        return Results.Redirect($"/login?ssoError=UserNotFound&adUser={Uri.EscapeDataString(windowsUsername)}");
    }
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
