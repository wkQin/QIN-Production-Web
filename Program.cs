using QIN_Production_Web.Components;
using QIN_Production_Web.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthenticationCore();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
    });
builder.Services.AddAuthorization();

// FIX: Persist encryption keys so that F5/Server Restarts don't invalidate LocalStorage!
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")));

builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<ProtectedSessionStorage>();

// Use Singleton for our fast token exchange cache
builder.Services.AddSingleton<LoginTokenCache>();

builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AlertService>();
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

app.MapGet("/api/auth/process", async (HttpContext context, LoginTokenCache cache) => 
{
    var tokenStr = context.Request.Query["token"];
    if (cache.TryGetToken(tokenStr, out var session, out var rememberMe))
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, session.Name ?? ""),
            new Claim(ClaimTypes.Role, session.Rechte ?? ""),
            new Claim("UserId", session.Personalnummer ?? "")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        
        var authProperties = new AuthenticationProperties 
        { 
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
        };

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        return Results.Redirect(session.Rechte == "Admin" || session.Rechte == "Verwaltung" ? "/verwaltung/aktivitaeten" : "/");
    }
    return Results.Redirect("/login?error=InvalidToken");
});

app.MapGet("/api/auth/logout", async (HttpContext context) => 
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
