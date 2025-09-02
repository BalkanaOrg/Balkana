using Balkana;
using Balkana.Data;
using Balkana.Data.Infrastructure;
using Balkana.Data.Repositories;
using Balkana.Services;
using Balkana.Services.Matches;
using Balkana.Services.Matches.Models;
using Balkana.Services.Organizers;
using Balkana.Services.Players;
using Balkana.Services.Series;
using Balkana.Services.Teams;
using Balkana.Services.Transfers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
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

//builder.Services.AddScoped<ISeriesRepository, SeriesRepository>();
//builder.Services.AddScoped<ISeriesService, SeriesService>();


var app = builder.Build();

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

    services.AddTransient<ITeamService, TeamService>();
    services.AddTransient<IPlayerService, PlayerService>();
    services.AddTransient<ITransferService, TransferService>();
    services.AddTransient<IOrganizerService, OrganizerService>();
    services.AddTransient<IMatchService, MatchService>();

    // Add HttpClient for our importers
    builder.Services.AddHttpClient<RiotMatchImporter>();
    builder.Services.AddHttpClient<FaceitMatchImporter>();

    // Also register the service dictionary
    builder.Services.AddScoped(provider => new Dictionary<string, IMatchImporter>
    {
        ["RIOT"] = provider.GetRequiredService<RiotMatchImporter>(),
        ["FACEIT"] = provider.GetRequiredService<FaceitMatchImporter>()
    });

    builder.Services.AddScoped<MatchHistoryService>();
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
