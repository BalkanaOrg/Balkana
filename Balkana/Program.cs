using Balkana;
using Balkana.Data;
using Balkana.Data.Infrastructure;
using Balkana.Services.Players;
using Balkana.Services.Teams;
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

    //services.AddTransient<>
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
