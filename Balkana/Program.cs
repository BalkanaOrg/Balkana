using Balkana;
using Balkana.Data;
using Balkana.Data.Infrastructure;
using Balkana.Data.Models;
using Balkana.Data.Repositories;
using Balkana.Data.Seed;
using Balkana.Services;
using Balkana.Services.Admin;
using Balkana.Services.Bracket;
using Balkana.Services.Matches;
using Balkana.Services.Matches.Models;
using Balkana.Services.Organizers;
using Balkana.Services.Players;
using Balkana.Services.Series;
using Balkana.Services.Teams;
using Balkana.Services.Transfers;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
//    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();
ConfigureServices(builder.Services);
builder.Services.AddScoped<SeriesService>();


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
builder.WebHost.UseUrls("https://localhost:7241");
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

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await RoleSeeder.SeedRoles(roleManager);
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
    //services.AddTransient<ISeriesS, MatchService>();

    //services.AddTransient<>

    builder.Services.AddHttpClient("faceit", c =>
    {
        c.BaseAddress = new Uri("https://open.faceit.com/data/v4/");
        c.DefaultRequestHeaders.Add("Authorization", "Bearer " + "moq kluch");
    });
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
    try { await next(); }
    catch (Exception ex)
    {
        Console.WriteLine("Middleware caught exception: " + ex);
        throw;
    }
});

app.MapControllers();
app.PrepareDatabase();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

//namespace Balkana
//{
//    public class Program
//    {
//        public static void Main(string[] args)
//            => CreateHostBuilder(args).Build().Run();

//        public static IHostBuilder CreateHostBuilder(string[] args)
//            => Host
//            .CreateDefaultBuilder(args)
//            .ConfigureWebHostDefaults(webBuilder => webBuilder
//            .UseStartup<Startup>());
//    }
//}
