using ImapClient.Web.Components;
using ImapClient.Web.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add controllers for culture endpoint
builder.Services.AddControllers();

builder.Services.AddLocalization();
builder.Services.Configure<MailApiOptions>(builder.Configuration.GetSection("MailApi"));
builder.Services.AddHttpClient<MailApiClient>();

var app = builder.Build();


// Configure supported cultures
var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("de") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("de"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
    RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers(); // Add this for culture controller
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
