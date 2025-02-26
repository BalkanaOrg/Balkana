using Balkana.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using Balkana.Data;

namespace Balkana.Data.Infrastructure
{
    //using static WebConstants

    public static class ApplicationBuilderExtension
    {
        public static IApplicationBuilder PrepareDatabase (
            this IApplicationBuilder app)
        {
            using var serviceScope = app.ApplicationServices.CreateScope();
            var services = serviceScope.ServiceProvider;

            MigrateDatabase(services);
            AddNationalities(services);
            AddOriginalMaps(services);
            AddDefaultPlayer(services);
            AddDefaultPlayerPfp(services);
            AddInitialGames(services);

            return app;
        }

        private static void MigrateDatabase(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();

            data.Database.Migrate();
        }

        private static void AddOriginalMaps (IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.csMaps.Any())
            {
                return;
            }

            data.csMaps.AddRange(new[]
            {
                new csMap
                {
                    Name = "Mirage",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/f/f1/CS2_de_mirage.png/534px-CS2_de_mirage.png",
                    isActiveDuty = true
                },
                new csMap
                {
                    Name = "Inferno",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/0/08/CS2_de_inferno.png/534px-CS2_de_inferno.png",
                    isActiveDuty = true
                },
                new csMap
                {
                    Name = "Anubis",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/2/28/CS2_de_anubis.png/534px-CS2_de_anubis.png",
                    isActiveDuty = true
                },
                new csMap
                {
                    Name = "Ancient",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/f/fc/CS2_de_ancient.png/534px-CS2_de_ancient.png",
                    isActiveDuty = true
                },
                new csMap
                {
                    Name = "Train",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/4/44/CS2_de_train.png/534px-CS2_de_train.png",
                    isActiveDuty = true
                },
                new csMap
                {
                    Name = "Dust 2",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/d/d7/CS2_Dust_2_A_Site.jpg/534px-CS2_Dust_2_A_Site.jpg",
                    isActiveDuty = true
                },
                new csMap
                {
                    Name = "Nuke",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/a/ad/CS2_de_nuke.png/534px-CS2_de_nuke.png",
                    isActiveDuty = true
                }
            });
            data.SaveChanges();

        }
        private static void AddDefaultPlayerPfp(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.Pictures.Any())
            {
                return;
            }

            data.Pictures.AddRange(new[]
            {
                new PlayerPicture
                {
                    PictureURL = "https://i.imgur.com/ZizgQGH.png",
                    PlayerId = 1,
                    dateChanged = DateTime.Now
                }
                //new PlayerPicture
                //{
                //    PictureURL = "https://i.imgur.com/ZizgQGH.png",
                //    PlayerId = 1,
                //    dateChanged = DateTime.Now
                //}
            });
            data.SaveChanges();
        }

        private static void AddDefaultPlayer(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.Players.Any())
            {
                return;
            }

            data.Players.AddRange(new[]
            {
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "Random Guy"
                }
            });
            data.SaveChanges();
        }

        private static void AddNationalities(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.Nationalities.Any())
            {
                return;
            }

            data.Nationalities.AddRange(new[]
            {
                new Nationality { Name="Bulgaria", FlagURL="https://flagicons.lipis.dev/flags/4x3/bg.svg"},
                new Nationality { Name="Germany", FlagURL="https://flagicons.lipis.dev/flags/4x3/de.svg"}
                //new Nationality { Name="", FlagURL=""}
            });
            data.SaveChanges();
        }
        private static void AddInitialGames(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.Games.Any())
            {
                return;
            }

            data.Games.AddRange(new[]
            {
                new Game
                {
                    FullName = "Counter-Strike",
                    ShortName = "CS2",
                    IconURL = "https://static.wikia.nocookie.net/logopedia/images/4/49/Counter-Strike_2_%28Icon%29.png/revision/latest?cb=20230330015359"
                },
                new Game
                {
                    FullName = "Rainbow Six Siege",
                    ShortName = "R6",
                    IconURL = "https://sw6.elbenwald.de/media/96/e2/44/1633451371/E1063325_3.jpg"
                },
                new Game
                {
                    FullName = "Fortnite",
                    ShortName = "FN",
                    IconURL = "https://preview.redd.it/d1qbfa9zqwn61.png?width=640&crop=smart&auto=webp&s=386aa51786fc010ab2b34b3e6fed1c3fce8fa68b"
                },
                new Game
                {
                    FullName = "League of Legends",
                    ShortName = "LoL",
                    IconURL = "https://brand.riotgames.com/static/a91000434ed683358004b85c95d43ce0/8a20a/lol-logo.png"
                }
            });
            data.SaveChanges();
        }
    }
}
