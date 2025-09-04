using Balkana.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using Balkana.Data;
using Balkana.Data.Infrastructure;

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
            AddInitialGames(services);
            AddNationalities(services);
            AddDefaultPlayer(services);
            AddDefaultPlayerPfp(services);
            AddTeamPositions(services);
            AddDefaultOrganizer(services);
            AddDefaultTournament(services);
            AddTestTeams(services);
            AddTestSeries(services);
            TestTransfers(services);
            AddOriginalMaps(services);
            AddTrophies(services);

            return app;
        }

        private static void MigrateDatabase(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();

            data.Database.Migrate();
        }

        private static void AddOriginalMaps(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.GameMaps.Any())
            {
                return;
            }

            data.GameMaps.AddRange(new[]
            {
                new GameMap
                {
                    DisplayName = "Mirage",
                    Name = "de_mirage",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/f/f1/CS2_de_mirage.png/534px-CS2_de_mirage.png",
                    isActiveDuty = true,
                    GameId=1
                },
                new GameMap
                {
                    DisplayName = "Inferno",
                    Name = "de_inferno",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/0/08/CS2_de_inferno.png/534px-CS2_de_inferno.png",
                    isActiveDuty = true,
                    GameId=1
                },
                new GameMap
                {
                    DisplayName = "Anubis",
                    Name = "de_anubis",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/2/28/CS2_de_anubis.png/534px-CS2_de_anubis.png",
                    isActiveDuty = false,
                    GameId=1
                },
                new GameMap
                {
                    DisplayName = "Ancient",
                    Name = "de_ancient",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/f/fc/CS2_de_ancient.png/534px-CS2_de_ancient.png",
                    isActiveDuty = true,
                    GameId=1
                },
                new GameMap
                {
                    DisplayName = "Train",
                    Name = "de_train",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/4/44/CS2_de_train.png/534px-CS2_de_train.png",
                    isActiveDuty = true,
                    GameId=1
                },
                new GameMap
                {
                    DisplayName = "Dust 2",
                    Name = "de_dust2",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/d/d7/CS2_Dust_2_A_Site.jpg/534px-CS2_Dust_2_A_Site.jpg",
                    isActiveDuty = true,
                    GameId=1
                },
                new GameMap
                {
                    DisplayName = "Nuke",
                    Name = "de_nuke",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/a/ad/CS2_de_nuke.png/534px-CS2_de_nuke.png",
                    isActiveDuty = true,
                    GameId=1
                },
                new GameMap
                {
                    DisplayName = "Overpass",
                    Name = "de_overpass",
                    PictureURL = "https://liquipedia.net/commons/images/thumb/e/ed/CS2OverpassConnector.jpg/800px-CS2OverpassConnector.jpg",
                    isActiveDuty = true,
                    GameId=1
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

        private static void AddDefaultOrganizer(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.Organizers.Any())
            {
                return;
            }

            data.Organizers.AddRange(new[]
            {
                new Organizer
                {
                    FullName = "Balkana",
                    Tag = "Balkana",
                    Description = "#1 burner of money.",
                    LogoURL = "https://i.imgur.com/sfnTpvc.png"
                },
                new Organizer
                {
                    FullName = "Electronic Sports League",
                    Tag = "ESL",
                    Description = "SAUDI MONEY BABYYYY",
                    LogoURL = "https://cdn.freebiesupply.com/logos/large/2x/esl-1-logo-svg-vector.svg"
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

        private static void AddDefaultTournament(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.Tournaments.Any())
            {
                return;
            }

            data.Tournaments.AddRange(new[]
            {
                new Tournament
                {
                    FullName = "Balkana Invitational Season III",
                    ShortName = "BI3",
                    Description = "The First Balkana 5v5 Tournament",
                    OrganizerId = 1,
                    GameId = 1,
                    StartDate = new DateTime(2024,9,8,18,0,0),
                    EndDate = new DateTime(2024,9,8,23,45,0)
                },
                new Tournament
                {
                    FullName = "Balkana Invitational Season IV",
                    ShortName = "BI4",
                    Description = "The First Balkana 5v5 Tournament",
                    OrganizerId = 1,
                    GameId = 1,
                    StartDate = new DateTime(2024,9,29,18,0,0),
                    EndDate = new DateTime(2024,9,29,23,59,0)
                },
                new Tournament
                {
                    FullName = "Balkana Invitational Season V",
                    ShortName = "BI5",
                    Description = "The Last Balkana Counter-Strike Tournament for Counter-Strike",
                    OrganizerId = 1,
                    GameId = 1,
                    StartDate = new DateTime(2024,11,30,18,0,0),
                    EndDate = new DateTime(2024,11,30,23,59,0)
                },
                new Tournament
                {
                    FullName = "Balkana Invitational Season VI",
                    ShortName = "BI6",
                    Description = "The First Balkana Tournament for 2025",
                    OrganizerId = 1,
                    GameId = 1,
                    StartDate = new DateTime(2025,2,1,17,0,0),
                    EndDate = new DateTime(2025,2,1,23,45,0)
                },
                new Tournament
                {
                    FullName = "Balkana Invitational Season VII",
                    ShortName = "BI7",
                    Description = "The Prodigal son returns",
                    OrganizerId = 1,
                    GameId = 1,
                    StartDate = new DateTime(2025,3,15,17,0,0),
                    EndDate = new DateTime(2025,3,15,23,59,0)
                },
                new Tournament
                {
                    FullName = "Balkana Invitational Season VIII",
                    ShortName = "BI8",
                    Description = "The Prodigal son returns",
                    OrganizerId = 1,
                    GameId = 1,
                    StartDate = new DateTime(2025,7,27,17,0,0),
                    EndDate = new DateTime(2025,7,27,23,45,0)
                },
                new Tournament
                {
                    FullName = "Balkana Invitational Season IX",
                    ShortName = "BI9",
                    Description = "The First Balkana 5v5 Tournament",
                    OrganizerId = 1,
                    GameId = 1,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now
                },
                new Tournament
                {
                    FullName = "Balkana FACE OFF",
                    ShortName = "BF",
                    Description = "The First Balkana FACE OFF",
                    OrganizerId = 1,
                    GameId = 1,
                    StartDate = new DateTime(2025,2,16,17,0,0),
                    EndDate = new DateTime(2025,2,16,23,45,0)
                },
                new Tournament
                {
                    FullName = "Balkana Showdown 2024",
                    ShortName = "BS",
                    Description = "The First Balkana League of Legends Tournament",
                    OrganizerId = 1,
                    GameId = 2,
                    StartDate = new DateTime(2024,12,15,17,0,0),
                    EndDate = new DateTime(2024,12,15,23,45,0)
                },
                new Tournament
                {
                    FullName = "Balkana Showdown II",
                    ShortName = "BS2",
                    Description = "The First Balkana League of Legends Tournament of 2025",
                    OrganizerId = 1,
                    GameId = 2,
                    StartDate = new DateTime(2025,8,31,17,0,0),
                    EndDate = new DateTime(2025,8,31,23,30,0)
                }
            });
            data.SaveChanges();
        }

        public static void AddTeamPositions(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.Positions.Any())
            {
                return;
            }

            data.Positions.AddRange(new[]
            {
                new TeamPosition { Name="Rifler", Icon="", GameId=1},
                new TeamPosition { Name="AWPer", Icon="", GameId=1},
                new TeamPosition { Name="Anchor", Icon="", GameId=1},
                new TeamPosition { Name="Support", Icon="", GameId=1},
                new TeamPosition { Name="IGL", Icon="", GameId=1},
                new TeamPosition { Name="Lurker", Icon="", GameId=1},
                new TeamPosition { Name="Head Coach", Icon="", GameId=1},
                new TeamPosition { Name="Coach", Icon="", GameId=1},
                new TeamPosition { Name="Analyst", Icon="", GameId=1},
                new TeamPosition { Name="Top laner", Icon="", GameId=4},
                new TeamPosition { Name="Jungler", Icon="", GameId=4},
                new TeamPosition { Name="Mid laner", Icon="", GameId=4},
                new TeamPosition { Name="ADC", Icon="", GameId=4},
                new TeamPosition { Name="Support", Icon="", GameId=4},
                new TeamPosition { Name="Head Coach", Icon="", GameId=4},
                new TeamPosition { Name="Coach", Icon="", GameId=4},
                new TeamPosition { Name="Analyst", Icon="", GameId=4},
                new TeamPosition { Name="Positional Coach", Icon="", GameId=4},
            });
            data.SaveChanges();
        }

        public static void AddTestSeries(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.Series.Any())
            {
                return;
            }

            data.Series.AddRange(new[]
            {
                new Series { Name="Test Series CS", TournamentId=1, TeamAId=1, TeamBId=2, DatePlayed=DateTime.Now.AddDays(-7), isFinished=false }
            });
            data.SaveChanges();
        }

        public static void AddNationalities(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.Nationalities.Any())
            {
                return;
            }

            data.Nationalities.AddRange(new[]
            {
                new Nationality { Name="Bulgaria", FlagURL="https://flagicons.lipis.dev/flags/4x3/bg.svg"},
                new Nationality { Name="Germany", FlagURL="https://flagicons.lipis.dev/flags/4x3/de.svg"},
                new Nationality { Name="Europe", FlagURL="https://flagicons.lipis.dev/flags/4x3/eu.svg"},
                new Nationality { Name="Greece", FlagURL="https://flagicons.lipis.dev/flags/4x3/gr.svg"},
                new Nationality { Name="Albania", FlagURL="https://flagicons.lipis.dev/flags/4x3/al.svg"},
                new Nationality { Name="Armenia", FlagURL="https://flagicons.lipis.dev/flags/4x3/am.svg"},
                new Nationality { Name="Austria", FlagURL="https://flagicons.lipis.dev/flags/4x3/at.svg"},
                new Nationality { Name="Belarus", FlagURL="https://flagicons.lipis.dev/flags/4x3/by.svg"},
                new Nationality { Name="Belgium", FlagURL="https://flagicons.lipis.dev/flags/4x3/be.svg"},
                new Nationality { Name="Bosnia & Herzegovina", FlagURL="https://flagicons.lipis.dev/flags/4x3/ba.svg"},
                new Nationality { Name="Djibouti", FlagURL="https://flagicons.lipis.dev/flags/4x3/dj.svg"},
                new Nationality { Name="Czech Republic", FlagURL="https://flagicons.lipis.dev/flags/4x3/cz.svg"},
                new Nationality { Name="China", FlagURL="https://flagicons.lipis.dev/flags/4x3/cn.svg"},
                new Nationality { Name="Brazil", FlagURL="https://flagicons.lipis.dev/flags/4x3/br.svg"},
                new Nationality { Name="Canada", FlagURL="https://flagicons.lipis.dev/flags/4x3/ca.svg"},
                new Nationality { Name="Croatia", FlagURL="https://flagicons.lipis.dev/flags/4x3/hr.svg"},
                new Nationality { Name="Cyprus", FlagURL="https://flagicons.lipis.dev/flags/4x3/cy.svg"},
                new Nationality { Name="Denmark", FlagURL="https://flagicons.lipis.dev/flags/4x3/dk.svg"},
                new Nationality { Name="Egypt", FlagURL="https://flagicons.lipis.dev/flags/4x3/eg.svg"},
                new Nationality { Name="Estonia", FlagURL="https://flagicons.lipis.dev/flags/4x3/ee.svg"},
                new Nationality { Name="Finland", FlagURL="https://flagicons.lipis.dev/flags/4x3/fi.svg"},
                new Nationality { Name="France", FlagURL="https://flagicons.lipis.dev/flags/4x3/fr.svg"},
                new Nationality { Name="Hungary", FlagURL="https://flagicons.lipis.dev/flags/4x3/hu.svg"},
                new Nationality { Name="Ireland", FlagURL="https://flagicons.lipis.dev/flags/4x3/ie.svg"},
                new Nationality { Name="Israel", FlagURL="https://flagicons.lipis.dev/flags/4x3/il.svg"},
                new Nationality { Name="Italy", FlagURL="https://flagicons.lipis.dev/flags/4x3/it.svg"},
                new Nationality { Name="Iran", FlagURL="https://flagicons.lipis.dev/flags/4x3/ir.svg"},
                new Nationality { Name="Iraq", FlagURL="https://flagicons.lipis.dev/flags/4x3/iq.svg"},
                new Nationality { Name="Jordan", FlagURL="https://flagicons.lipis.dev/flags/4x3/jo.svg"},
                new Nationality { Name="Kazakhstan", FlagURL="https://flagicons.lipis.dev/flags/4x3/kz.svg"},
                new Nationality { Name="Kuwait", FlagURL="https://flagicons.lipis.dev/flags/4x3/kw.svg"},
                new Nationality { Name="Kyrgyzstan", FlagURL="https://flagicons.lipis.dev/flags/4x3/kg.svg"},
                new Nationality { Name="Latvia", FlagURL="https://flagicons.lipis.dev/flags/4x3/lv.svg"},
                new Nationality { Name="Lebanon", FlagURL="https://flagicons.lipis.dev/flags/4x3/lb.svg"},
                new Nationality { Name="Libya", FlagURL="https://flagicons.lipis.dev/flags/4x3/ly.svg"},
                new Nationality { Name="Lichtenstein", FlagURL="https://flagicons.lipis.dev/flags/4x3/li.svg"},
                new Nationality { Name="Lithuania", FlagURL="https://flagicons.lipis.dev/flags/4x3/lt.svg"},
                new Nationality { Name="Luxembourg", FlagURL="https://flagicons.lipis.dev/flags/4x3/lu.svg"},
                new Nationality { Name="Malta", FlagURL="https://flagicons.lipis.dev/flags/4x3/mt.svg"},
                new Nationality { Name="Moldova", FlagURL="https://flagicons.lipis.dev/flags/4x3/md.svg"},
                new Nationality { Name="Monaco", FlagURL="https://flagicons.lipis.dev/flags/4x3/mc.svg"},
                new Nationality { Name="Montenegro", FlagURL="https://flagicons.lipis.dev/flags/4x3/me.svg"},
                new Nationality { Name="Morocco", FlagURL="https://flagicons.lipis.dev/flags/4x3/ma.svg"},
                new Nationality { Name="Netherlands", FlagURL="https://flagicons.lipis.dev/flags/4x3/nl.svg"},
                new Nationality { Name="North Macedonia", FlagURL="https://flagicons.lipis.dev/flags/4x3/mk.svg"},
                new Nationality { Name="Norway", FlagURL="https://flagicons.lipis.dev/flags/4x3/no.svg"},
                new Nationality { Name="Oman", FlagURL="https://flagicons.lipis.dev/flags/4x3/om.svg"},
                new Nationality { Name="Poland", FlagURL="https://flagicons.lipis.dev/flags/4x3/pl.svg"},
                new Nationality { Name="Portugal", FlagURL="https://flagicons.lipis.dev/flags/4x3/pt.svg"},
                new Nationality { Name="Qatar", FlagURL="https://flagicons.lipis.dev/flags/4x3/qa.svg"},
                new Nationality { Name="Romania", FlagURL="https://flagicons.lipis.dev/flags/4x3/ro.svg"},
                new Nationality { Name="Russia", FlagURL="https://flagicons.lipis.dev/flags/4x3/ru.svg"},
                new Nationality { Name="San Marino", FlagURL="https://flagicons.lipis.dev/flags/4x3/sm.svg"},
                new Nationality { Name="Saudi Arabia", FlagURL="https://flagicons.lipis.dev/flags/4x3/sa.svg"},
                new Nationality { Name="Serbia", FlagURL="https://flagicons.lipis.dev/flags/4x3/rs.svg"},
                new Nationality { Name="Slovakia", FlagURL="https://flagicons.lipis.dev/flags/4x3/sk.svg"},
                new Nationality { Name="Slovenia", FlagURL="https://flagicons.lipis.dev/flags/4x3/si.svg"},
                new Nationality { Name="Spain", FlagURL="https://flagicons.lipis.dev/flags/4x3/es.svg"},
                new Nationality { Name="Palestine", FlagURL="https://flagicons.lipis.dev/flags/4x3/ps.svg"},
                new Nationality { Name="Sweden", FlagURL="https://flagicons.lipis.dev/flags/4x3/se.svg"},
                new Nationality { Name="Switzerland", FlagURL="https://flagicons.lipis.dev/flags/4x3/ch.svg"},
                new Nationality { Name="Syria", FlagURL="https://flagicons.lipis.dev/flags/4x3/sy.svg"},
                new Nationality { Name="Tajikistan", FlagURL="https://flagicons.lipis.dev/flags/4x3/tj.svg"},
                new Nationality { Name="Tunisia", FlagURL="https://flagicons.lipis.dev/flags/4x3/tn.svg"},
                new Nationality { Name="Turkmenistan", FlagURL="https://flagicons.lipis.dev/flags/4x3/tm.svg"},
                new Nationality { Name="Turkey", FlagURL="https://flagicons.lipis.dev/flags/4x3/tr.svg"},
                new Nationality { Name="Ukraine", FlagURL="https://flagicons.lipis.dev/flags/4x3/ua.svg"},
                new Nationality { Name="United Arab Emirates", FlagURL="https://flagicons.lipis.dev/flags/4x3/ae.svg"},
                new Nationality { Name="United Kingdom", FlagURL="https://flagicons.lipis.dev/flags/4x3/gb.svg"},
                new Nationality { Name="United States of America", FlagURL="https://flagicons.lipis.dev/flags/4x3/us.svg"},
                new Nationality { Name="Uzbekistan", FlagURL="https://flagicons.lipis.dev/flags/4x3/uz.svg"},
                new Nationality { Name="Yemen", FlagURL="https://flagicons.lipis.dev/flags/4x3/ye.svg"}
                //new Nationality { Name="", FlagURL=""}
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
                    FirstName = "Kristian",
                    LastName = "Alexander",
                    NationalityId = 1,
                    Nickname = "meally1g"
                },
                new Player
                {
                    FirstName = "Rayan",
                    LastName = "Yordanov",
                    NationalityId = 1,
                    Nickname = "Arsoyy"
                },
                new Player
                {
                    FirstName = "Yordan",
                    LastName = "Paradaisov",
                    NationalityId = 1,
                    Nickname = "Paradise"
                },
                new Player
                {
                    FirstName = "Valentin",
                    LastName = "Cvetanov",
                    NationalityId = 1,
                    Nickname = "s_koleloto_bace"
                },
                new Player
                {
                    FirstName = "Daniel",
                    LastName = "Kirev",
                    NationalityId = 1,
                    Nickname = "Danchouu_"
                },
                new Player
                {
                    FirstName = "Pavel",
                    LastName = "Gurbov",
                    NationalityId = 1,
                    Nickname = "KNZO"
                },
                new Player
                {
                    FirstName = "Vladimir",
                    LastName = "Manchev",
                    NationalityId = 1,
                    Nickname = "VallM"
                },
                new Player
                {
                    FirstName = "Teodor",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "[VPX]"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "Atina"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "Seka4a"
                },
                new Player
                {
                    FirstName = "Georgi",
                    LastName = "Gospodinov",
                    NationalityId = 1,
                    Nickname = "GeGo"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "btk"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "GURGO"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "Steezy"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "extinctlxrd"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "Yoni"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "radiatorbg"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "Kannax"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "korrkapio"
                },
                new Player
                {
                    FirstName = "Bobo",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "wait1p"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "renn_fps"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "shems187"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "BaLkanZ"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "Bavarin"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "x1no"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "TONI"
                },
                new Player
                {
                    FirstName = "Pavel",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "HeyZe"
                },
                new Player
                {
                    FirstName = "Danail",
                    LastName = "Gigov",
                    NationalityId = 1,
                    Nickname = "Frosssty"
                },
                new Player
                {
                    FirstName = "Petur",
                    LastName = "Petrov",
                    NationalityId = 1,
                    Nickname = "ogpepi"
                },
                new Player
                {
                    FirstName = "Random",
                    LastName = "Randomov",
                    NationalityId = 1,
                    Nickname = "CurrentlyNull"
                }
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
                    IconURL = "https://static.wikia.nocookie.net/logopedia/images/4/49/Counter-Strike_2_%28Icon%29.png"
                },
                new Game
                {
                    FullName = "League of Legends",
                    ShortName = "LoL",
                    IconURL = "https://brand.riotgames.com/static/a91000434ed683358004b85c95d43ce0/8a20a/lol-logo.png"
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
                    IconURL = "https://preview.redd.it/d1qbfa9zqwn61.png"
                }
            });
            data.SaveChanges();
        }
        private static void AddTestTeams(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.Teams.Any())
            {
                return;
            }

            data.Teams.AddRange(new[]
            {
                new Team
                {
                    FullName = "TDI",
                    Tag = "TDI",
                    GameId = 1,
                    LogoURL = "https://i.imgur.com/IEl6MYC.png",
                    yearFounded = 2024
                },
                new Team
                {
                    FullName = "Divi Qzovci",
                    Tag = "DIV",
                    GameId = 1,
                    LogoURL = "https://i.imgur.com/gc0dGxD.png",
                    yearFounded = 2024
                },
                new Team
                {
                    FullName = "Demarsiqnska Uiovina",
                    Tag = "DMU",
                    GameId = 2,
                    LogoURL = "https://i.imgur.com/gc0dGxD.png",
                    yearFounded = 2025
                },
                new Team
                {
                    FullName = "Ludite Kravi",
                    Tag = "LK",
                    GameId = 2,
                    LogoURL = "https://i.imgur.com/gc0dGxD.png",
                    yearFounded = 2025
                },
                new Team
                {
                    FullName = "SOP Clan",
                    Tag = "SOP",
                    GameId = 1,
                    LogoURL = "https://i.imgur.com/gc0dGxD.png",
                    yearFounded = 2024
                },
                new Team
                {
                    FullName = "TROHARITE",
                    Tag = "TRH",
                    GameId = 1,
                    LogoURL = "https://i.imgur.com/gc0dGxD.png",
                    yearFounded = 2024
                }
            });
            data.SaveChanges();
        }
        private static void TestTransfers(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.PlayerTeamTransfers.Any())
            {
                return;
            }

            data.PlayerTeamTransfers.AddRange(new[]
            {
                new PlayerTeamTransfer
                {
                    PlayerId = 1,
                    TeamId = 1,
                    PositionId = 1,
                    TransferDate = DateTime.Now,
                },
                new PlayerTeamTransfer
                {
                    PlayerId = 2,
                    TeamId = 1,
                    PositionId = 1,
                    TransferDate = DateTime.Now,
                },
                new PlayerTeamTransfer
                {
                    PlayerId = 3,
                    TeamId = 1,
                    PositionId = 1,
                    TransferDate = DateTime.Now,
                },
                new PlayerTeamTransfer
                {
                    PlayerId = 4,
                    TeamId = 1,
                    PositionId = 1,
                    TransferDate = DateTime.Now,
                },
                new PlayerTeamTransfer
                {
                    PlayerId = 5,
                    TeamId = 1,
                    PositionId = 1,
                    TransferDate = DateTime.Now,
                },
                new PlayerTeamTransfer
                {
                    PlayerId = 6,
                    TeamId = 2,
                    PositionId = 1,
                    TransferDate = DateTime.Now,
                },
                new PlayerTeamTransfer
                {
                    PlayerId = 7,
                    TeamId = 2,
                    PositionId = 1,
                    TransferDate = DateTime.Now,
                },
                new PlayerTeamTransfer
                {
                    PlayerId = 8,
                    TeamId = 2,
                    PositionId = 1,
                    TransferDate = DateTime.Now,
                },
                new PlayerTeamTransfer
                {
                    PlayerId = 9,
                    TeamId = 2,
                    PositionId = 1,
                    TransferDate = DateTime.Now,
                },
                new PlayerTeamTransfer
                {
                    PlayerId = 10,
                    TeamId = 2,
                    PositionId = 1,
                    TransferDate = DateTime.Now,
                },
            });
            data.SaveChanges();
        }
        private static void AddTrophies(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.Trophies.Any())
            {
                return;
            }

            data.Trophies.AddRange(new[]
            {
                new TrophyTournament
                {
                    AwardType = "Trophy",
                    Description = "Winner of Balkana Invitational Season III",
                    IconURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSH2wuwu0bRlPLic52NbVE71BDSQdPMcqBpHw&s",
                    TournamentId = 1,
                    AwardDate = new DateTime(2024,8,9),
                },
                new TrophyTournament
                {
                    AwardType = "Trophy",
                    Description = "Winner of Balkana Invitational Season IV",
                    IconURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSH2wuwu0bRlPLic52NbVE71BDSQdPMcqBpHw&s",
                    TournamentId = 1,
                    AwardDate = new DateTime(2024,9,29),
                },
                new TrophyTournament
                {
                    AwardType = "Trophy",
                    Description = "Winner of Balkana Invitational Season V",
                    IconURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSH2wuwu0bRlPLic52NbVE71BDSQdPMcqBpHw&s",
                    TournamentId = 1,
                    AwardDate = new DateTime(2024,11,30),
                },
                new TrophyTournament
                {
                    AwardType = "Trophy",
                    Description = "Winner of Balkana Invitational Season VI",
                    IconURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSH2wuwu0bRlPLic52NbVE71BDSQdPMcqBpHw&s",
                    TournamentId = 1,
                    AwardDate = new DateTime(2025,2,2),
                },
                new TrophyTournament
                {
                    AwardType = "Trophy",
                    Description = "Winner of Balkana Invitational Season VII",
                    IconURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSH2wuwu0bRlPLic52NbVE71BDSQdPMcqBpHw&s",
                    TournamentId = 1,
                    AwardDate = new DateTime(2025,3,15),
                },
                new TrophyTournament
                {
                    AwardType = "Trophy",
                    Description = "Winner of Balkana Invitational Season VIII",
                    IconURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSH2wuwu0bRlPLic52NbVE71BDSQdPMcqBpHw&s",
                    TournamentId = 1,
                    AwardDate = new DateTime(2025,7,27),
                },
                new TrophyTournament
                {
                    AwardType = "MVP",
                    Description = "MVP of Balkana Invitational Season III",
                    IconURL = "https://i.imgur.com/VEvcsmv.png",
                    TournamentId = 1,
                    AwardDate = new DateTime(2024,8,9),
                },
                new TrophyTournament
                {
                    AwardType = "EVP",
                    Description = "EVP of Balkana Invitational Season VI",
                    IconURL = "https://i.imgur.com/EmdO0lj.png",
                    TournamentId = 1,
                    AwardDate = new DateTime(2024,8,9),
                },
            });
            data.SaveChanges();
        }
    }
}
