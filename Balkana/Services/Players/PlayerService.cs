

namespace Balkana.Services.Players
{
    using AutoMapper;
    using AutoMapper.QueryableExtensions;
    using Balkana.Data;
    using Balkana.Data.Models;
    using Balkana.Models.Players;
    using Balkana.Services.Players.Models;
    using Balkana.Services.Teams;
    using Balkana.Services.Transfers.Models;
    using Microsoft.EntityFrameworkCore;

    public class PlayerService : IPlayerService
    {
        private readonly ApplicationDbContext data;
        private readonly IConfigurationProvider mapper;

        public PlayerService(ApplicationDbContext data, IMapper mapper)
        {
            this.mapper = mapper.ConfigurationProvider;
            this.data = data;
        }

        public PlayerQueryServiceModel All(
            string searchTerm = null,
            int currentPage = 1,
            int playersPerPage = int.MaxValue
        )
        {
            var playersQuery = this.data.Players.AsQueryable();

            if(!string.IsNullOrWhiteSpace(searchTerm))
            {
                playersQuery = playersQuery.Where(c=>(c.Nickname).ToLower().Contains(searchTerm.ToLower()));
            }

            var totalPlayers = playersQuery.Count();

            var players = GetPlayers(playersQuery.Skip((currentPage - 1) * playersPerPage).Take(playersPerPage));

            return new PlayerQueryServiceModel
            {
                TotalPlayers = totalPlayers,
                CurrentPage = currentPage,
                PlayersPerPage = playersPerPage,
                Players = players
            };
        }

        public PlayerDetailsServiceModel Profile(int id, string? gameFilter = null)
        {
            var defaultPic = "https://media.istockphoto.com/id/1618846975/photo/smile-black-woman-and-hand-pointing-in-studio-for-news-deal-or-coming-soon-announcement-on.jpg?s=612x612&w=0&k=20&c=LUvvJu4sGaIry5WLXmfQV7RStbGG5hEQNo8hEFxZSGY=";

            var player = this.data.Players
        .Where(p => p.Id == id)
        .Select(p => new PlayerDetailsServiceModel
        {
            Id = p.Id,
            Nickname = p.Nickname,
            FirstName = p.FirstName,
            LastName = p.LastName,
            NationalityId = p.NationalityId,
            NationalityName = p.Nationality.Name,
            FlagUrl = p.Nationality.FlagURL,

            PictureUrl = p.PlayerPictures
                .OrderByDescending(pic => pic.dateChanged)
                .Select(pic => pic.PictureURL)
                .FirstOrDefault() ?? defaultPic,

            PlayerPictures = p.PlayerPictures
                .OrderByDescending(pic => pic.dateChanged)
                .Select(pic => new PlayerPictureServiceModel
                {
                    Id = pic.Id,
                    PictureUrl = pic.PictureURL,
                    DateChanged = pic.dateChanged
                }),

            GameProfiles = p.GameProfiles
                .Select(gp => new PlayerGameProfileServiceModel
                {
                    Provider = gp.Provider,
                    GameName = gp.Player.Nationality.Name, // This will be updated properly
                    UUID = gp.UUID
                }).ToList(),

            Trophies = p.PlayerTrophies
                .Where(pt => pt.Trophy.AwardType != "MVP" && pt.Trophy.AwardType != "EVP")
                .Select(pt => new PlayerTrophyServiceModel
                {
                    Id = pt.Trophy.Id,
                    Description = pt.Trophy.Description,
                    IconURL = pt.Trophy.IconURL,
                    AwardDate = pt.Trophy.AwardDate
                }).ToList(),

            MVPs = p.PlayerTrophies
                .Where(pt => pt.Trophy.AwardType == "MVP")
                .Select(pt => new PlayerTrophyServiceModel
                {
                    Id = pt.Trophy.Id,
                    Description = pt.Trophy.Description,
                    IconURL = pt.Trophy.IconURL,
                    AwardDate = pt.Trophy.AwardDate
                }).ToList(),

            EVPs = p.PlayerTrophies
                .Where(pt => pt.Trophy.AwardType == "EVP")
                .Select(pt => new PlayerTrophyServiceModel
                {
                    Id = pt.Trophy.Id,
                    Description = pt.Trophy.Description,
                    IconURL = pt.Trophy.IconURL,
                    AwardDate = pt.Trophy.AwardDate
                }).ToList()
        })
        .FirstOrDefault();

            if (player == null) return null;

            // Fetch game profiles with proper game names
            var gameProfiles = this.data.GameProfiles
                .Include(gp => gp.Player)
                .Where(gp => gp.PlayerId == id)
                .ToList();

            player.GameProfiles = gameProfiles
                .Select(gp => new PlayerGameProfileServiceModel
                {
                    Provider = gp.Provider,
                    GameName = GetGameNameFromProvider(gp.Provider),
                    UUID = gp.UUID
                }).ToList();

            // Fetch transfers grouped by game
            var transfers = this.data.PlayerTeamTransfers
                .Where(t => t.PlayerId == id)
                .Include(t => t.Team)
                .Include(t => t.TeamPosition)
                    .ThenInclude(tp => tp.Game)
                .OrderByDescending(t => t.StartDate)
                .Select(t => new TransferDetailsServiceModel
                {
                    Id = t.Id,
                    TeamId = t.TeamId,
                    TeamFullName = t.Team.FullName,
                    LogoURL = t.Team.LogoURL,
                    GameId = t.TeamPosition.GameId,
                    GameName = t.TeamPosition.Game.FullName,
                    PositionId = (int)t.PositionId,
                    Position = t.TeamPosition.Name,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    Status = t.Status
                })
                .ToList();

            player.TransfersByGame = transfers
                .GroupBy(t => t.GameName)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Calculate average stats for each game
            foreach (var gameProfile in player.GameProfiles)
            {
                player.AverageStatsByGame[gameProfile.GameName] = CalculateAverageStats(id, gameProfile.Provider);
                player.TournamentParticipationByGame[gameProfile.GameName] = GetTournamentParticipation(id, gameProfile.Provider);
                player.MatchHistoryByGame[gameProfile.GameName] = GetMatchHistory(id, gameProfile.Provider);
            }

            return player;
        }

        public PlayerStatsServiceModel Stats(int id, string? gameFilter = null)
        {
            var player = this.data.Players
                .Include(p => p.GameProfiles)
                .FirstOrDefault(p => p.Id == id);

            if (player == null) return null;

            var playerUUIDs = player.GameProfiles.Select(g => g.UUID).ToList();

            // Find all stats across games
            var statsQuery = this.data.Set<PlayerStatistic>()
                .Include(s => s.Match).ThenInclude(m => m.Series)
                .Where(s => playerUUIDs.Contains(s.PlayerUUID));

            if (!string.IsNullOrEmpty(gameFilter))
                statsQuery = statsQuery.Where(s => s.Source == gameFilter);

            var stats = statsQuery.ToList();

            // Detect games played
            var gamesPlayed = stats.Select(s => s.Source).Distinct().ToList();

            // Default filter = game with most matches
            if (string.IsNullOrEmpty(gameFilter))
            {
                gameFilter = stats
                    .GroupBy(s => s.Source)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key;
            }

            // Filter again with final choice
            var filtered = this.data.PlayerStats
                // join with GameProfile to resolve PlayerId from PlayerUUID
                .Join(
                    this.data.GameProfiles,
                    stats => stats.PlayerUUID,
                    profile => profile.UUID,
                    (stats, profile) => new { Stats = stats, PlayerId = profile.PlayerId }
                )
                // filter by the actual player Id
                .Where(x => x.PlayerId == id)
                .Select(x => x.Stats)
                // include related navigation properties
                .Include(s => s.Match)
                    .ThenInclude(m => m.Series)
                        .ThenInclude(se => se.Tournament)
                .ToList();

            var result = new PlayerStatsServiceModel
            {
                PlayerId = player.Id,
                Nickname = player.Nickname,
                GamesPlayed = gamesPlayed,
                SelectedGame = gameFilter,
                SeriesStats = filtered
                    .GroupBy(s => s.Match.SeriesId)
                    .Select(g => new PlayerSeriesStatsServiceModel
                    {
                        SeriesId = g.Key,
                        TournamentName = g.First().Match.Series.Tournament.FullName,
                        StartedAt = g.First().Match.Series.DatePlayed,
                        Matches = g.Select(s => new PlayerMatchStatsServiceModel
                        {
                            MatchId = s.MatchId,
                            PlayedAt = s.Match.PlayedAt,
                            //Opponent = (s.Match.TeamAId == s.Match.TeamBId ? "Unknown" :
                            //            s.Match.TeamA.Players.Any(p => p.Id == id) ?
                            //                s.Match.TeamB.FullName : s.Match.TeamA.FullName),
                            IsWinner = s.IsWinner,
                            CS2Stats = s as PlayerStatistic_CS2,
                            LoLStats = s as PlayerStatistic_LoL
                        }).ToList()
                    }).ToList()
            };

            return result;
        }

        public PlayerDetailsServiceModel GetPlayerProfile(int playerId)
        {
            var player = this.data.Players
            .Where(p => p.Id == playerId)
            .Include(p => p.PlayerTrophies)                 // ✅ Include PlayerTrophies
                .ThenInclude(pt => pt.Trophy)              // ✅ Include the Trophy itself
            .Select(p => new PlayerDetailsServiceModel
            {
                Id = p.Id,
                Nickname = p.Nickname,
                FirstName = p.FirstName,
                LastName = p.LastName,
                PictureUrl = p.PlayerPictures
                    .OrderByDescending(pic => pic.dateChanged)
                    .Select(pic => pic.PictureURL)
                    .FirstOrDefault(),

                Trophies = p.PlayerTrophies
                    .Where(pt => pt.Trophy.AwardType != "MVP" && pt.Trophy.AwardType != "EVP")
                    .Select(pt => new PlayerTrophyServiceModel
                    {
                        Id = pt.Trophy.Id,
                        Description = pt.Trophy.Description,
                        IconURL = pt.Trophy.IconURL,
                        AwardDate = pt.Trophy.AwardDate
                    }).ToList(),

                MVPs = p.PlayerTrophies
                    .Where(pt => pt.Trophy.AwardType == "MVP")
                    .Select(pt => new PlayerTrophyServiceModel
                    {
                        Id = pt.Trophy.Id,
                        Description = pt.Trophy.Description,
                        IconURL = pt.Trophy.IconURL,
                        AwardDate = pt.Trophy.AwardDate
                    }).ToList(),

                EVPs = p.PlayerTrophies
                    .Where(pt => pt.Trophy.AwardType == "EVP")
                    .Select(pt => new PlayerTrophyServiceModel
                    {
                        Id = pt.Trophy.Id,
                        Description = pt.Trophy.Description,
                        IconURL = pt.Trophy.IconURL,
                        AwardDate = pt.Trophy.AwardDate
                    }).ToList()
            })
            .FirstOrDefault();

            var test = this.data.PlayerTrophies
    .Include(pt => pt.Trophy)
    .Where(pt => pt.PlayerId == playerId)
    .ToList();

            Console.WriteLine($"Found {test.Count} trophies for player {playerId}");

            return player;
        }

        public int Create(string nickname, string firstname, string lastname, int nationalityid)
        {
            var playerData = new Player
            {
                Nickname = nickname,
                FirstName = firstname,
                LastName = lastname,
                NationalityId = nationalityid
            };
            this.data.Players.Add(playerData);
            this.data.SaveChanges();

            return playerData.Id;
        }

        public bool Edit(
            int id,
            string nickname,
            string firstname,
            string lastname,
            int nationalityId)
        {
            var playerData = this.data.Players.Find(id);
            if(playerData== null)
            {
                return false;
            }

            playerData.Nickname = nickname;
            playerData.FirstName = firstname;
            playerData.LastName = lastname;
            playerData.NationalityId = nationalityId;

            this.data.SaveChanges();

            return true;
        }

        public bool PictureExists(int id)
            => this.data
                .Pictures
                .Any(c=>c.Id == id);

        public IEnumerable<PlayerPictureServiceModel> AllPlayerPictures(int playerId)
            => this.data
                .Pictures
                .Where(c => c.PlayerId == playerId)
                .ProjectTo<PlayerPictureServiceModel>(this.mapper)
                .ToList();

        public IEnumerable<PlayerNationalityServiceModel> GetNationalities()
            => this.data
            .Nationalities
            .Select(c => new PlayerNationalityServiceModel
            {
                Id = c.Id,
                Name = c.Name,
                PictureUrl = c.FlagURL
            })
            .ToList();

        private IEnumerable<PlayerServiceModel> GetPlayers(IQueryable<Player> playerQuery)
            => playerQuery
            .ProjectTo<PlayerServiceModel>(this.mapper)
            .ToList();

        private string GetGameNameFromProvider(string provider)
        {
            return provider switch
            {
                "FACEIT" => "Counter-Strike",
                "RIOT" => "League of Legends",
                _ => provider
            };
        }

        private PlayerAverageStatsServiceModel CalculateAverageStats(int playerId, string source)
        {
            var playerUUIDs = this.data.GameProfiles
                .Where(gp => gp.PlayerId == playerId && gp.Provider == source)
                .Select(gp => gp.UUID)
                .ToList();

            if (!playerUUIDs.Any()) return new PlayerAverageStatsServiceModel { Source = source };

            var stats = this.data.Set<PlayerStatistic>()
                .Where(s => playerUUIDs.Contains(s.PlayerUUID) && s.Source == source)
                .ToList();

            if (!stats.Any()) return new PlayerAverageStatsServiceModel { Source = source };

            var gameName = GetGameNameFromProvider(source);
            var totalMatches = stats.Count;
            
            // Get all matches for proper win/loss calculation
            var matchIds = stats.Select(s => s.MatchId).ToList();
            var matches = this.data.Matches
                .Where(m => matchIds.Contains(m.Id))
                .ToList();
            
            var wins = stats.Count(s => {
                var match = matches.First(m => m.Id == s.MatchId);
                return IsPlayerWinner(match, s.PlayerUUID);
            });
            var losses = totalMatches - wins;

            var result = new PlayerAverageStatsServiceModel
            {
                GameName = gameName,
                Source = source,
                TotalMatches = totalMatches,
                Wins = wins,
                Losses = losses
            };

            if (source == "FACEIT")
            {
                var cs2Stats = stats.OfType<PlayerStatistic_CS2>().ToList();
                if (cs2Stats.Any())
                {
                    result.AverageKills = cs2Stats.Average(s => s.Kills);
                    result.AverageDeaths = cs2Stats.Average(s => s.Deaths);
                    result.AverageAssists = cs2Stats.Average(s => s.Assists);
                    result.AverageDamage = cs2Stats.Average(s => s.Damage);
                    result.AverageHLTVRating = cs2Stats.Average(s => s.HLTV1);
                    result.AverageHeadshotPercentage = cs2Stats.Average(s => s.Kills > 0 ? (double)s.HSkills / s.Kills * 100 : 0);
                    result.AverageADR = cs2Stats.Average(s => s.RoundsPlayed > 0 ? (double)s.Damage / s.RoundsPlayed : 0);
                    result.AverageKPR = cs2Stats.Average(s => s.RoundsPlayed > 0 ? (double)s.Kills / s.RoundsPlayed : 0);
                    result.AverageDPR = cs2Stats.Average(s => s.RoundsPlayed > 0 ? (double)s.Deaths / s.RoundsPlayed : 0);

                    // Calculate map statistics
                    result.MapStats = CalculateMapStats(cs2Stats, totalMatches);
                }
            }
            else if (source == "RIOT")
            {
                var lolStats = stats.OfType<PlayerStatistic_LoL>().ToList();
                if (lolStats.Any())
                {
                    result.AverageLoLKills = lolStats.Average(s => s.Kills ?? 0);
                    result.AverageLoLDeaths = lolStats.Average(s => s.Deaths ?? 0);
                    result.AverageLoLAssists = lolStats.Average(s => s.Assists ?? 0);
                    result.AverageCreepScore = lolStats.Average(s => s.CreepScore);
                    result.AverageVisionScore = lolStats.Average(s => s.VisionScore);
                    result.AverageGoldEarned = lolStats.Average(s => s.GoldEarned);
                }
            }

            return result;
        }

        private List<PlayerTournamentParticipationServiceModel> GetTournamentParticipation(int playerId, string source)
        {
            var gameName = GetGameNameFromProvider(source);

            // Get player's teams during tournament periods
            var playerTransfers = this.data.PlayerTeamTransfers
                .Include(t => t.Team)
                    .ThenInclude(team => team.Game)
                .Include(t => t.TeamPosition)
                .Where(t => t.PlayerId == playerId && t.TeamPosition.Game.FullName == gameName)
                .ToList();

            var participations = new List<PlayerTournamentParticipationServiceModel>();

            foreach (var transfer in playerTransfers)
            {
                var tournamentTeams = this.data.TournamentTeams
                    .Include(tt => tt.Tournament)
                        .ThenInclude(t => t.Game)
                    .Include(tt => tt.Tournament)
                        .ThenInclude(t => t.Organizer)
                    .Include(tt => tt.Team)
                    .Where(tt => tt.TeamId == transfer.TeamId && tt.Tournament.Game.FullName == gameName)
                    .Where(tt => tt.Tournament.StartDate <= transfer.EndDate && tt.Tournament.EndDate >= transfer.StartDate)
                    .ToList();

                foreach (var tt in tournamentTeams)
                {
                    var tournament = tt.Tournament;
                    var placement = this.data.TournamentPlacements
                        .Include(tp => tp.Tournament)
                        .Include(tp => tp.Team)
                        .FirstOrDefault(tp => tp.TournamentId == tournament.Id && tp.TeamId == transfer.TeamId);

                    // Calculate series and match stats
                    var seriesInTournament = this.data.Series
                        .Include(s => s.Matches)
                            .ThenInclude(m => m.PlayerStats)
                        .Where(s => s.TournamentId == tournament.Id && (s.TeamAId == transfer.TeamId || s.TeamBId == transfer.TeamId))
                        .ToList();

                    var totalSeries = seriesInTournament.Count;
                    var seriesWins = seriesInTournament.Count(s => s.WinnerTeamId == transfer.TeamId);
                    var seriesLosses = totalSeries - seriesWins;

                    var totalMatches = seriesInTournament.Sum(s => s.Matches.Count);
                    var matchWins = seriesInTournament.Sum(s => s.Matches.Count(m => m.WinnerTeamId == transfer.TeamId));
                    var matchLosses = totalMatches - matchWins;

                    participations.Add(new PlayerTournamentParticipationServiceModel
                    {
                        TournamentId = tournament.Id,
                        TournamentName = tournament.FullName,
                        TournamentShortName = tournament.ShortName,
                        GameName = gameName,
                        OrganizerName = tournament.Organizer.FullName,
                        StartDate = tournament.StartDate,
                        EndDate = tournament.EndDate,
                        PrizePool = tournament.PrizePool,
                        BannerUrl = tournament.BannerUrl,
                        TeamId = transfer.TeamId ?? 0,
                        TeamName = transfer.Team.FullName,
                        TeamTag = transfer.Team.Tag,
                        TeamLogoUrl = transfer.Team.LogoURL,
                        Placement = placement?.Placement,
                        PointsAwarded = placement?.PointsAwarded,
                        TotalSeries = totalSeries,
                        SeriesWins = seriesWins,
                        SeriesLosses = seriesLosses,
                        TotalMatches = totalMatches,
                        MatchWins = matchWins,
                        MatchLosses = matchLosses
                    });
                }
            }

            return participations.OrderByDescending(p => p.StartDate).ToList();
        }

        private PlayerMatchHistoryServiceModel GetMatchHistory(int playerId, string source)
        {
            var gameName = GetGameNameFromProvider(source);
            var playerUUIDs = this.data.GameProfiles
                .Where(gp => gp.PlayerId == playerId && gp.Provider == source)
                .Select(gp => gp.UUID)
                .ToList();

            if (!playerUUIDs.Any())
            {
                return new PlayerMatchHistoryServiceModel { GameName = gameName, Source = source };
            }

            var matches = this.data.Set<PlayerStatistic>()
                .Include(s => s.Match)
                    .ThenInclude(m => m.Series)
                        .ThenInclude(se => se.Tournament)
                            .ThenInclude(t => t.Organizer)
                .Include(s => s.Match)
                    .ThenInclude(m => m.TeamA)
                .Include(s => s.Match)
                    .ThenInclude(m => m.TeamB)
                .Include(s => s.Match)
                    .ThenInclude(m => (m as MatchCS).Map)
                .Where(s => playerUUIDs.Contains(s.PlayerUUID) && s.Source == source)
                .OrderByDescending(s => s.Match.PlayedAt)
                .ToList();

            var tournamentGroups = matches
                .GroupBy(s => s.Match.Series.TournamentId)
                .Select(tg => new PlayerTournamentGroupServiceModel
                {
                    TournamentId = tg.Key,
                    TournamentName = tg.First().Match.Series.Tournament.FullName,
                    TournamentShortName = tg.First().Match.Series.Tournament.ShortName,
                    OrganizerName = tg.First().Match.Series.Tournament.Organizer.FullName,
                    StartDate = tg.First().Match.Series.Tournament.StartDate,
                    EndDate = tg.First().Match.Series.Tournament.EndDate,
                    BannerUrl = tg.First().Match.Series.Tournament.BannerUrl,
                    SeriesGroups = tg
                        .GroupBy(s => s.Match.SeriesId)
                        .Select(sg => new PlayerSeriesGroupServiceModel
                        {
                            SeriesId = sg.Key,
                            SeriesName = GetSeriesName(sg.First().Match.Series, playerId),
                            DatePlayed = sg.First().Match.Series.DatePlayed,
                            IsSeriesWinner = sg.First().Match.Series.WinnerTeamId != null && 
                                           sg.Any(s => IsPlayerWinner(s.Match, s.PlayerUUID)),
                            OpponentTeamName = GetOpponentTeamName(sg.First().Match, playerId),
                            Matches = sg.Select(s => new PlayerMatchStatsServiceModel
                            {
                                MatchId = s.MatchId,
                                PlayedAt = s.Match.PlayedAt,
                                Opponent = GetOpponentTeamName(s.Match, playerId),
                                MapName = GetMapName(s.Match),
                                IsWinner = IsPlayerWinner(s.Match, s.PlayerUUID),
                                CS2Stats = s as PlayerStatistic_CS2,
                                LoLStats = s as PlayerStatistic_LoL
                            }).OrderByDescending(m => m.PlayedAt).ToList(),
                            MatchWins = sg.Count(s => IsPlayerWinner(s.Match, s.PlayerUUID)),
                            MatchLosses = sg.Count(s => !IsPlayerWinner(s.Match, s.PlayerUUID))
                        }).OrderByDescending(s => s.DatePlayed).ToList()
                }).OrderByDescending(t => t.StartDate).ToList();

            return new PlayerMatchHistoryServiceModel
            {
                GameName = gameName,
                Source = source,
                TournamentGroups = tournamentGroups
            };
        }

        private string GetSeriesName(Series series, int playerId)
        {
            var tournamentShortName = series.Tournament?.ShortName ?? "Unknown Tournament";
            
            // Get player's team during this series
            var playerTeam = GetPlayerTeamDuringSeries(series, playerId);
            var opponentTeam = GetOpponentTeamInSeries(series, playerTeam);
            
            if (playerTeam != null && opponentTeam != null)
            {
                return $"{tournamentShortName} - {playerTeam.Tag} vs {opponentTeam.Tag}";
            }
            
            return $"{tournamentShortName} - {series.Name}";
        }

        private string GetOpponentTeamName(Match match, int playerId)
        {
            if (match.TeamA != null && match.TeamB != null)
            {
                // Find which team the player was on during this match
                var playerTeamId = GetPlayerTeamInMatch(match, playerId);
                if (playerTeamId == match.TeamAId)
                {
                    return match.TeamB?.Tag ?? "Unknown";
                }
                else if (playerTeamId == match.TeamBId)
                {
                    return match.TeamA?.Tag ?? "Unknown";
                }
            }
            return "Unknown";
        }

        private string GetMapName(Match match)
        {
            if (match is MatchCS csMatch && csMatch.Map != null)
            {
                return csMatch.Map.DisplayName ?? csMatch.Map.Name;
            }
            else if (match is MatchLoL lolMatch)
            {
                // For LoL, we could map MapId to map names, but for now return a generic name
                return "Summoner's Rift";
            }
            return "Unknown Map";
        }

        private int? GetPlayerTeamInMatch(Match match, int playerId)
        {
            // Find the team the player was on during this match date
            var transfer = this.data.PlayerTeamTransfers
                .Where(t => t.PlayerId == playerId && t.StartDate <= match.PlayedAt && (t.EndDate == null || t.EndDate >= match.PlayedAt))
                .OrderByDescending(t => t.StartDate)
                .FirstOrDefault();

            return transfer?.TeamId;
        }

        private Team GetPlayerTeamDuringSeries(Series series, int playerId)
        {
            // Find the team the player was on during this series date
            var transfer = this.data.PlayerTeamTransfers
                .Include(t => t.Team)
                .Where(t => t.PlayerId == playerId && t.StartDate <= series.DatePlayed && (t.EndDate == null || t.EndDate >= series.DatePlayed))
                .OrderByDescending(t => t.StartDate)
                .FirstOrDefault();

            return transfer?.Team;
        }

        private Team GetOpponentTeamInSeries(Series series, Team playerTeam)
        {
            if (playerTeam == null) return null;
            
            if (series.TeamAId == playerTeam.Id)
            {
                return series.TeamB;
            }
            else if (series.TeamBId == playerTeam.Id)
            {
                return series.TeamA;
            }
            
            return null;
        }

        private List<PlayerMapStatsServiceModel> CalculateMapStats(List<PlayerStatistic_CS2> stats, int totalMatches)
        {
            if (!stats.Any()) return new List<PlayerMapStatsServiceModel>();

            // Get match IDs from stats
            var matchIds = stats.Select(s => s.MatchId).ToList();

            // Get all matches with map information using the match IDs
            var matchesWithMaps = this.data.MatchesCS
                .Include(m => m.Map)
                .Where(m => matchIds.Contains(m.Id) && m.MapId.HasValue && m.Map != null && m.Map.isActiveDuty)
                .ToList();

            var mapStats = matchesWithMaps
                .GroupBy(m => m.MapId.Value)
                .Select(g => new PlayerMapStatsServiceModel
                {
                    MapId = g.Key,
                    MapName = g.First().Map.Name,
                    MapDisplayName = g.First().Map.DisplayName ?? g.First().Map.Name,
                    MatchesPlayed = g.Count(),
                    PictureURL = g.First().Map.PictureURL,
                    Wins = g.Count(m => IsPlayerWinner(m, stats.First(s => s.MatchId == m.Id).PlayerUUID)),
                    Losses = g.Count(m => !IsPlayerWinner(m, stats.First(s => s.MatchId == m.Id).PlayerUUID)),
                    PickRate = totalMatches > 0 ? (double)g.Count() / totalMatches * 100 : 0,
                    IsActiveDuty = g.First().Map.isActiveDuty
                })
                .OrderByDescending(ms => ms.MatchesPlayed)
                .ToList();

            return mapStats;
        }

        private bool IsPlayerWinner(Match match, string playerUUID)
        {
            if (!match.WinnerTeamId.HasValue) return false;

            // Get the player's team during this match
            var playerTeamId = GetPlayerTeamDuringMatch(match.PlayedAt, playerUUID);
            
            return playerTeamId.HasValue && playerTeamId.Value == match.WinnerTeamId.Value;
        }

        private int? GetPlayerTeamDuringMatch(DateTime matchDate, string playerUUID)
        {
            // Find the player ID from the UUID
            var playerId = this.data.GameProfiles
                .Where(gp => gp.UUID == playerUUID)
                .Select(gp => gp.PlayerId)
                .FirstOrDefault();

            if (playerId == 0) return null;

            // Find the team the player was on during this match date
            var transfer = this.data.PlayerTeamTransfers
                .Where(t => t.PlayerId == playerId && t.StartDate <= matchDate && (t.EndDate == null || t.EndDate >= matchDate))
                .OrderByDescending(t => t.StartDate)
                .FirstOrDefault();

            return transfer?.TeamId;
        }
    }
}
