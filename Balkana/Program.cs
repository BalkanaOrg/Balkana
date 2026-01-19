using Balkana;
using Balkana.Data;
using Balkana.Data.Infrastructure;
using Balkana.Data.Models;
using Balkana.Data.Repositories;
using Balkana.Data.Seed;
using Balkana.Services;
using Balkana.Services.Admin;
using Balkana.Services.Brandings;
using Balkana.Services.Bracket;
using Balkana.Services.Discord;
using Balkana.Services.Matches;
using Balkana.Services.Matches.Models;
using Balkana.Services.Organizers;
using Balkana.Services.Players;
using Balkana.Services.Series;
using Balkana.Services.Stats;
using Balkana.Services.Teams;
using Balkana.Services.Tournaments;
using Balkana.Services.Transfers;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;

Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");


// At top of Program.cs (before builder.Build())
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    try
    {
        Console.WriteLine("UNHANDLED EXCEPTION (AppDomain): " + (e.ExceptionObject ?? "(null)"));
        if (e.ExceptionObject is Exception ex) Console.WriteLine(ex.ToString());
    }
    catch { }
};

TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    try
    {
        Console.WriteLine("UNOBSERVED TASK EXCEPTION: " + e.Exception?.ToString());
    }
    catch { }
};

Console.WriteLine("Runtime: " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
Console.WriteLine("OS: " + System.Runtime.InteropServices.RuntimeInformation.OSDescription);


var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80); // HTTP
    options.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps("/etc/letsencrypt/live/balkana.org/balkana.pfx", "SilnaParola123");
    });
});


// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Connection String: {connectionString ?? "(null)"}");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    // Suppress the warning about pending model changes to allow app startup
    // Migrations should still be created manually, but the app won't crash
    options.ConfigureWarnings(warnings => 
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
//    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

// Configure Data Protection for OAuth state cookies
// In production, you should persist keys to a shared location
var dataProtectionKeysPath = "/tmp/DataProtection-Keys";
if (!Directory.Exists(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
    Console.WriteLine($"Created Data Protection keys directory: {dataProtectionKeysPath}");
}
else
{
    Console.WriteLine($"Using existing Data Protection keys directory: {dataProtectionKeysPath}");
    var keyFiles = Directory.GetFiles(dataProtectionKeysPath);
    Console.WriteLine($"Found {keyFiles.Length} key file(s) in Data Protection directory");
}

builder.Services.AddDataProtection()
    .SetApplicationName("Balkana")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

ConfigureServices(builder.Services);
builder.Services.AddScoped<SeriesService>();

    // Add session support for guest shopping cart
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromHours(2);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });


builder.Services.AddHttpClient<RiotMatchImporter>(client =>
{
    client.BaseAddress = new Uri("https://europe.api.riotgames.com/"); // adjust for your region
    client.DefaultRequestHeaders.Add("X-Riot-Token", builder.Configuration["Riot:ApiKey"]);
});

builder.Services.AddHttpClient<FaceitMatchImporter>(client =>
{
    client.BaseAddress = new Uri("https://open.faceit.com/data/v4/");
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + builder.Configuration["Faceit:ApiKey"]);
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 104_857_600; // 100 MB
    serverOptions.Limits.MaxRequestBufferSize = 104_857_600;
    serverOptions.Limits.MaxRequestLineSize = 16_384;
    serverOptions.Limits.MaxRequestHeadersTotalSize = 65_536;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104_857_600; // 100 MB
    options.BufferBody = true;
});

builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

//builder.Services.AddScoped<ISeriesRepository, SeriesRepository>();
//builder.Services.AddScoped<ISeriesService, SeriesService>();
//builder.WebHost.UseUrls("https://localhost:7241");
AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    Console.WriteLine("Unhandled exception: " + args.ExceptionObject.ToString());
};
TaskScheduler.UnobservedTaskException += (sender, args) =>
{
    Console.WriteLine("Unobserved task exception: " + args.Exception.ToString());
    args.SetObserved();
};

var app = builder.Build();

app.Use(async (context, next) =>
{
    Console.WriteLine($"Incoming request: {context.Request.Method} {context.Request.Path}");
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Middleware caught exception: " + ex);
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal server error: " + ex.Message);
    }
});

try
{
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await RoleSeeder.SeedRoles(roleManager);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Failed to seed roles. This might be a database connection issue.");
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
    // Don't throw - allow app to continue if database isn't available yet
    // This is useful during initial setup
}

void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<ApplicationDbContext>(options => options
    .UseSqlServer(connectionString));

    services.AddDatabaseDeveloperPageExceptionFilter();

    services.AddAutoMapper(typeof(Startup));

    services.AddControllersWithViews(options =>
    {
        options.Filters.Add<AutoValidateAntiforgeryTokenAttribute>();
    });
    builder.Services.AddRazorPages();

    services.AddTransient<ITeamService, TeamService>();
    services.AddTransient<IPlayerService, PlayerService>();
    services.AddTransient<ITransferService, TransferService>();
    services.AddTransient<IOrganizerService, OrganizerService>();
    services.AddTransient<IMatchService, MatchService>();
    services.AddTransient<IStatsService, StatsService>();
    services.AddTransient<IBrandingService, BrandingService>();

    //USER IDENTITY
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // Google OAuth Configuration - Add to existing authentication
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = builder.Configuration["Google:ClientId"] ?? "";
            options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";
            // Use the standard OAuth callback path - Google will redirect here
            // The middleware will process it and then redirect to our controller action
            options.CallbackPath = "/signin-google";
            options.SaveTokens = true;
            
            // Configure cookie settings for OAuth state preservation
            // The correlation cookie stores the OAuth state between the initial request and callback
            // This MUST work across the redirect from Google, so we need careful configuration
            // Note: Don't override the cookie name - let the framework use its default
            // The framework generates a unique cookie name per OAuth provider
            options.CorrelationCookie.HttpOnly = true;
            options.CorrelationCookie.SameSite = SameSiteMode.Lax; // Lax works for GET redirects from external sites
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Match the request scheme
            options.CorrelationCookie.Path = "/";
            options.CorrelationCookie.IsEssential = true; // Ensure cookie is always set even if user hasn't consented
            options.CorrelationCookie.MaxAge = TimeSpan.FromMinutes(10); // State should be valid for 10 minutes
            
            // Add event handlers to debug OAuth flow
            options.Events.OnRemoteFailure = context =>
            {
                Console.WriteLine($"OAuth RemoteFailure: {context.Failure?.Message}");
                Console.WriteLine($"OAuth RemoteFailure Exception: {context.Failure}");
                Console.WriteLine($"OAuth RemoteFailure RedirectUri: {context.Properties?.RedirectUri}");
                // Don't let the failure propagate - handle it in the controller
                context.HandleResponse();
                context.Response.Redirect("/Account/Login?error=oauth_failed");
                return Task.CompletedTask;
            };
            
            options.Events.OnAccessDenied = context =>
            {
                Console.WriteLine($"OAuth AccessDenied: {context.Properties?.RedirectUri}");
                context.HandleResponse();
                context.Response.Redirect("/Account/Login?error=access_denied");
                return Task.CompletedTask;
            };
            
            options.Events.OnTicketReceived = context =>
            {
                Console.WriteLine($"OAuth TicketReceived - Principal: {context.Principal?.Identity?.Name}");
                Console.WriteLine($"OAuth TicketReceived - RedirectUri: {context.Properties?.RedirectUri}");
                
                // Don't handle the response here - let the middleware complete and redirect naturally
                // The middleware will redirect to the RedirectUri (or default to callback path)
                // Since we're not setting a RedirectUri in GoogleLogin, it should stay on the callback path
                // and the controller action will be called
                return Task.CompletedTask;
            };
            
            options.Events.OnCreatingTicket = context =>
            {
                Console.WriteLine($"OAuth CreatingTicket - Principal: {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            };
            
            // Note: OnRemoteSuccess doesn't exist in OAuthEvents
            // The OAuth middleware will automatically redirect after processing
            // We need to handle this in the controller action instead
            
        })
        .AddDiscord(options =>
        {
            options.ClientId = builder.Configuration["Discord:ClientId"] ?? "";
            options.ClientSecret = builder.Configuration["Discord:ClientSecret"] ?? "";
            options.CallbackPath = "/signin-discord";
            options.SaveTokens = true;
            
            options.CorrelationCookie.HttpOnly = true;
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.CorrelationCookie.Path = "/";
            options.CorrelationCookie.IsEssential = true;
            options.CorrelationCookie.MaxAge = TimeSpan.FromMinutes(10);
            
            options.Scope.Add("identify");
            
            options.Events.OnRemoteFailure = context =>
            {
                Console.WriteLine($"Discord OAuth RemoteFailure: {context.Failure?.Message}");
                context.HandleResponse();
                context.Response.Redirect("/Account/Profile?error=discord_oauth_failed");
                return Task.CompletedTask;
            };
            
            options.Events.OnAccessDenied = context =>
            {
                Console.WriteLine($"Discord OAuth AccessDenied");
                context.HandleResponse();
                context.Response.Redirect("/Account/Profile?error=discord_access_denied");
                return Task.CompletedTask;
            };
        });

    // Add HttpClient for our importers
    builder.Services.AddHttpClient<RiotMatchImporter>();
    builder.Services.AddHttpClient<FaceitMatchImporter>();
    builder.Services.AddHttpClient<IExternalApiService, ExternalApiService>();

    // Also register the service dictionary
    builder.Services.AddScoped(provider => new Dictionary<string, IMatchImporter>
    {
        ["RIOT"] = provider.GetRequiredService<RiotMatchImporter>(),
        ["FACEIT"] = provider.GetRequiredService<FaceitMatchImporter>()
    });

    builder.Services.AddScoped<MatchHistoryService>();
    builder.Services.AddScoped<DoubleEliminationBracketService>();
    
    // Riot Tournament Service
    builder.Services.AddHttpClient<IRiotTournamentService, RiotTournamentService>();
    
    // Store Services
    builder.Services.AddScoped<Balkana.Services.Store.IStoreService, Balkana.Services.Store.StoreService>();
    builder.Services.AddScoped<Balkana.Services.Store.IAdminStoreService, Balkana.Services.Store.AdminStoreService>();
    builder.Services.AddScoped<Balkana.Services.Store.IPaymentService, Balkana.Services.Store.PaymentService>();
    builder.Services.AddHttpClient<Balkana.Services.Store.IDeliveryService, Balkana.Services.Store.DeliveryService>();
    
    // Gambling Services
    builder.Services.AddScoped<Balkana.Services.Gambling.IGamblingService, Balkana.Services.Gambling.GamblingService>();
    
    //services.AddTransient<ISeriesS, MatchService>();

    //services.AddTransient<>

    builder.Services.AddHttpClient("faceit", c =>
    {
        c.BaseAddress = new Uri("https://open.faceit.com/data/v4/");
        c.DefaultRequestHeaders.Add("Authorization", "Bearer " + "moq kluch");
    });

    // Discord Bot Services
    builder.Services.Configure<Balkana.Models.Discord.DiscordConfig>(
        builder.Configuration.GetSection("Discord"));
    builder.Services.AddHttpClient<IDiscordBotService, DiscordBotService>();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    Console.WriteLine($"Incoming request: {context.Request.Method} {context.Request.Path} - ContentLength: {context.Request.ContentLength} - ContentType: {context.Request.ContentType}");
    Console.WriteLine($"Request Scheme: {context.Request.Scheme}, Host: {context.Request.Host}");
    
    // Debug OAuth correlation cookies
    if (context.Request.Path.StartsWithSegments("/Account/GoogleCallback") || 
        context.Request.Path.StartsWithSegments("/Account/GoogleLogin"))
    {
        Console.WriteLine("OAuth-related request - checking cookies:");
        foreach (var cookie in context.Request.Cookies)
        {
            if (cookie.Key.Contains("Correlation") || cookie.Key.Contains("Google"))
            {
                Console.WriteLine($"  Cookie: {cookie.Key} = {cookie.Value?.Substring(0, Math.Min(50, cookie.Value?.Length ?? 0))}...");
            }
        }
    }
    
    try 
    { 
        await next(); 
        Console.WriteLine($"Request completed: {context.Request.Method} {context.Request.Path} - Status: {context.Response.StatusCode}");
        
        // Debug cookies being set
        if (context.Request.Path.StartsWithSegments("/Account/GoogleLogin"))
        {
            Console.WriteLine("Response cookies being set:");
            foreach (var cookie in context.Response.Headers.SetCookie)
            {
                if (cookie.Contains("Correlation") || cookie.Contains("Google"))
                {
                    Console.WriteLine($"  Set-Cookie: {cookie}");
                }
            }
        }
        
        // Debug redirects on OAuth callback
        if (context.Request.Path.StartsWithSegments("/Account/GoogleCallback"))
        {
            if (context.Response.StatusCode == 302 || context.Response.StatusCode == 301)
            {
                var location = context.Response.Headers.Location.ToString();
                Console.WriteLine($"OAuth Callback redirecting to: {location}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Middleware caught exception: " + ex);
        Console.WriteLine("Exception stack trace: " + ex.StackTrace);
        throw;
    }
});

app.PrepareDatabase();

// Configure HTTPS redirection but exclude OAuth callbacks to avoid breaking the flow
app.UseHttpsRedirection();

app.UseRouting();

app.UseSession(); // Must be before UseAuthentication - Session is critical for OAuth state

// Authentication middleware - handles OAuth callbacks
app.UseAuthentication();

// Authorization middleware
app.UseAuthorization();

// Map controllers - the SitemapController has [Route("/sitemap.xml")] attribute
app.MapControllers();

// Explicit route for sitemap.xml to ensure it's registered
app.MapControllerRoute(
    name: "sitemap",
    pattern: "sitemap.xml",
    defaults: new { controller = "Sitemap", action = "Index" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Static files should come AFTER routing so routes are matched first
// Disable caching for static files if in development OR if DisableStaticFileCache is true
var disableStaticFileCache = app.Environment.IsDevelopment() || 
                              builder.Configuration.GetValue<bool>("DisableStaticFileCache", false);

if (disableStaticFileCache)
{
    // Disable caching for static files in development
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
    });
}
else
{
    app.UseStaticFiles();
}

app.Run();