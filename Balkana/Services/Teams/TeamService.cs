namespace Balkana.Services.Teams
{
    using AutoMapper;
    using Balkana.Data;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AutoMapper.QueryableExtensions;
    using Balkana.Services.Teams.Models;
    using Balkana.Models.Teams;
    using Balkana.Data.Models;
    using Balkana.Services.Players.Models;
    using Microsoft.EntityFrameworkCore;

    public class TeamService : ITeamService
    {
        private readonly ApplicationDbContext data;
        private readonly IConfigurationProvider mapper;

        public TeamService(ApplicationDbContext data, IMapper mapper)
        {
            this.data = data;
            this.mapper = mapper.ConfigurationProvider;
        }

        public TeamQueryServiceModel All(
            string game = "Counter-Strike",
            string searchTerm = null,
            int? year = null,
            int currentPage = 1,
            int teamsPerPage = int.MaxValue
        )
        {
            var teamsQuery = this.data.Teams
                .Include(t => t.TournamentTeams)
                    .ThenInclude(tt => tt.Tournament)
                .AsQueryable();

            if(!string.IsNullOrWhiteSpace(game))
            {
                teamsQuery = teamsQuery.Where(c=>c.Game.FullName==game);
            }
            if(!string.IsNullOrWhiteSpace(searchTerm))
            {
                teamsQuery = teamsQuery.Where(c => (c.FullName + " " + c.Tag).ToLower().Contains(searchTerm.ToLower()));
            }
            
            // Filter by year based on tournament participation
            if (year.HasValue)
            {
                teamsQuery = teamsQuery.Where(t => t.TournamentTeams.Any(tt => tt.Tournament.StartDate.Year == year.Value));
            }
            
            var totalTeams = teamsQuery.Count();

            var teams = GetTeams(teamsQuery.Skip((currentPage - 1) * teamsPerPage).Take(teamsPerPage));

            return new TeamQueryServiceModel
            {
                TotalTeams = totalTeams,
                CurrentPage = currentPage,
                TeamsPerPage = teamsPerPage,
                Teams = teams
            };
        }
        public TeamDetailsServiceModel Details(int id)
        {
            var team = this.data.Teams
                .Where(t => t.Id == id)
                .Include(t => t.Game)
                .Include(t => t.Transfers)
                    .ThenInclude(tr => tr.Player)
                .Include(t => t.Transfers)
                    .ThenInclude(tr => tr.TeamPosition)
                .Include(t => t.Placements)
                    .ThenInclude(p => p.Tournament)
                .Include(t => t.TournamentTeams)
                    .ThenInclude(tt => tt.Tournament)
                        .ThenInclude(tournament => tournament.Organizer)
                .Include(t => t.TeamTrophies)
                    .ThenInclude(tt => tt.Trophy)
                .Include(t => t.SeriesAsTeam1)
                .Include(t => t.SeriesAsTeam2)
                .FirstOrDefault();

            if (team == null) return null;

            var now = DateTime.UtcNow;
            var defaultPic = "/uploads/PlayerProfiles/_defaultPfp.png";

            // Build comprehensive team details
            var teamDetails = new TeamDetailsServiceModel
            {
                Id = team.Id,
                FullName = team.FullName,
                Tag = team.Tag,
                LogoURL = team.LogoURL,
                GameId = team.GameId,
                yearFounded = team.yearFounded,
                GameName = team.Game?.FullName ?? "",
                GameShortName = team.Game?.ShortName ?? "",
                CurrentRoster = GetCurrentRoster(team, now, defaultPic),
                HistoricalRosters = GetHistoricalRosters(team, now, defaultPic),
                Tournaments = GetTournamentParticipation(team),
                Trophies = GetTeamTrophies(team),
                RecentMatches = GetRecentMatches(team),
                CurrentRosterStats = GetCurrentRosterStats(team, now),
                AllTimeStats = GetAllTimeStats(team),
                Players = GetLegacyPlayers(team, now, defaultPic)
            };

            return teamDetails;
        }

        public int Create(string fullname, string tag, string logoURL, int yearFounded, int gameId)
        {
            var teamData = new Team
            {
                FullName = fullname,
                Tag = tag,
                LogoURL = logoURL,
                yearFounded = yearFounded,
                GameId = gameId
            };
            this.data.Teams.Add(teamData);
            this.data.SaveChanges();

            return teamData.Id;
        }
        public bool Update(
            int id,
            string fullname,
            string tag,
            string logo,
            int yearFounded,
            int gameId)
        {
            var teamData = this.data.Teams.Find(id);

            if(teamData == null)
            {
                return false;
            }
            teamData.FullName = fullname;
            teamData.Tag = tag;
            teamData.LogoURL = logo;
            teamData.yearFounded = yearFounded;
            teamData.GameId = gameId;

            this.data.SaveChanges();
            return true;
        }

        public IEnumerable<string> GetAllGames() 
            => this.data
                .Games
                .Select(c=>c.FullName)
                .ToList();

        public IEnumerable<int> GetAvailableYears()
        {
            return this.data.Tournaments
                .Select(t => t.StartDate.Year)
                .Distinct()
                .OrderByDescending(year => year)
                .ToList();
        }

        public IEnumerable<TeamGameServiceModel> AllGames()
            => this.data
                .Games
                .ProjectTo<TeamGameServiceModel>(this.mapper)
                .ToList();

        public bool GameExists(int gameId)
            => this.data
                .Games
                .Any(c=>c.Id == gameId);

        //THIS NEEDS TO BE REWORKED ASAP (Показва всички играчи, независимо дали са всео ще в отбора или не)
        //public IEnumerable<TeamStaffServiceModel> AllPlayers(int teamId)
        //{
        //    var latestTransfers = this.data.PlayerTeamTransfers
        //        .Where(ptt => ptt.TeamId == teamId &&
        //            ptt.TransferDate == this.data.PlayerTeamTransfers
        //                .Where(inner => inner.PlayerId == ptt.PlayerId)
        //                .Max(inner => inner.TransferDate)) // only latest transfer per player
        //        .ProjectTo<TeamStaffServiceModel>(this.mapper)
        //        .ToList();

        //    return latestTransfers;
        //}

        public IEnumerable<TeamStaffServiceModel> AllPlayers(int teamId)
        {
            var defaultPic = "https://media.istockphoto.com/id/1618846975/photo/smile-black-woman-and-hand-pointing-in-studio-for-news-deal-or-coming-soon-announcement-on.jpg?s=612x612&w=0&k=20&c=LUvvJu4sGaIry5WLXmfQV7RStbGG5hEQNo8hEFxZSGY=";

            var now = DateTime.UtcNow; // or pass a snapshot date

            var latestTransfers =
            from ptt in data.PlayerTeamTransfers
            join maxDates in
                (from inner in data.PlayerTeamTransfers
                 where inner.TeamId == teamId
                       && inner.StartDate <= now
                       && (inner.EndDate == null || inner.EndDate >= now)
                       && inner.Status == PlayerTeamStatus.Active
                 group inner by inner.PlayerId into g
                 select new { PlayerId = g.Key, LatestDate = g.Max(x => x.StartDate) })
            on new { ptt.PlayerId, ptt.StartDate } equals new { maxDates.PlayerId, StartDate = maxDates.LatestDate }
            where ptt.TeamId == teamId
                  && ptt.StartDate <= now
                  && (ptt.EndDate == null || ptt.EndDate >= now)
                  && ptt.Status == PlayerTeamStatus.Active
            select new TeamStaffServiceModel
            {
                Id = ptt.Player.Id,
                Nickname = ptt.Player.Nickname,
                FirstName = ptt.Player.FirstName,
                LastName = ptt.Player.LastName,
                PositionId = ptt.PositionId ?? 0,
                PictureUrl = ptt.Player.PlayerPictures
                    .OrderByDescending(pic => pic.dateChanged)
                    .Select(pic => pic.PictureURL)
                    .FirstOrDefault() ?? defaultPic
            };

            var list = latestTransfers.ToList();

            return latestTransfers;
        }


        private IEnumerable<TeamServiceModel> GetTeams(IQueryable<Team> teamQuery)
        {
            var defaultPic = "https://media.istockphoto.com/id/1618846975/photo/smile-black-woman-and-hand-pointing-in-studio-for-news-deal-or-coming-soon-announcement-on.jpg?s=612x612&w=0&k=20&c=LUvvJu4sGaIry5WLXmfQV7RStbGG5hEQNo8hEFxZSGY=";

            // Load teams in memory first
            var teams = teamQuery
                .AsNoTracking()
                .ToList();

            // Map teams to service models
            var teamModels = teams.Select(team =>
            {
                // Calculate wins/losses from Series table
                var seriesPlayed = this.data.Series
                    .Where(s => s.isFinished && 
                                (s.TeamAId == team.Id || s.TeamBId == team.Id) &&
                                s.WinnerTeamId.HasValue)
                    .ToList();

                int wins = 0;
                int losses = 0;

                foreach (var series in seriesPlayed)
                {
                    if (series.WinnerTeamId == team.Id)
                    {
                        wins++;
                    }
                    else if ((series.TeamAId == team.Id || series.TeamBId == team.Id) && 
                             series.WinnerTeamId.HasValue)
                    {
                        losses++;
                    }
                }

                int totalSeries = wins + losses;
                double winRate = totalSeries > 0 ? (double)wins / totalSeries * 100 : 0;

                var model = new TeamServiceModel
                {
                    Id = team.Id,
                    FullName = team.FullName,
                    Tag = team.Tag,
                    LogoURL = team.LogoURL,
                    yearFounded = team.yearFounded,
                    GameId = team.GameId,
                    Wins = wins,
                    Losses = losses,
                    WinRate = Math.Round(winRate, 1),
                    Players = this.AllPlayers(team.Id)
                        .Take(5) // max 5 players for HLTV-style grid
                        .Select(p => new PlayerServiceModel
                        {
                            Id = p.Id,
                            Nickname = p.Nickname,
                            FirstName = p.FirstName,
                            LastName = p.LastName,
                            PictureUrl = p.PictureUrl ?? defaultPic
                        })
                        .ToList()
                };

                return model;
            }).ToList();

            return teamModels;
        }

        public int AbsoluteNumberOfTeams()
        {
            var count = this.data.Teams.Count();
            return count;
        }

        private IEnumerable<TeamRosterServiceModel> GetCurrentRoster(Team team, DateTime now, string defaultPic)
        {
            return team.Transfers
                .Where(tr => tr.StartDate <= now && 
                           (tr.EndDate == null || tr.EndDate >= now) && 
                           tr.Status == PlayerTeamStatus.Active)
                .GroupBy(tr => tr.PlayerId)
                .Select(g => g.OrderByDescending(tr => tr.StartDate).First())
                .Select(tr => new TeamRosterServiceModel
                {
                    PlayerId = tr.Player.Id,
                    Nickname = tr.Player.Nickname,
                    FirstName = tr.Player.FirstName,
                    LastName = tr.Player.LastName,
                    PictureUrl = tr.Player.PlayerPictures
                        .OrderByDescending(pic => pic.dateChanged)
                        .Select(pic => pic.PictureURL)
                        .FirstOrDefault() ?? defaultPic,
                    PositionId = tr.PositionId,
                    PositionName = tr.TeamPosition?.Name ?? "",
                    StartDate = tr.StartDate,
                    EndDate = tr.EndDate,
                    Status = tr.Status,
                    IsCurrentRoster = true
                })
                .OrderBy(p => p.PositionId)
                .ThenBy(p => p.Nickname)
                .ToList();
        }

        private IEnumerable<TeamRosterServiceModel> GetHistoricalRosters(Team team, DateTime now, string defaultPic)
        {
            // Get all transfers that have ended (for historical view)
            return team.Transfers
                .Where(tr => tr.EndDate != null && tr.EndDate < now)
                .GroupBy(tr => new { tr.PlayerId, tr.StartDate })
                .Select(g => g.First())
                .Select(tr => new TeamRosterServiceModel
                {
                    PlayerId = tr.Player.Id,
                    Nickname = tr.Player.Nickname,
                    FirstName = tr.Player.FirstName,
                    LastName = tr.Player.LastName,
                    PictureUrl = tr.Player.PlayerPictures
                        .OrderByDescending(pic => pic.dateChanged)
                        .Select(pic => pic.PictureURL)
                        .FirstOrDefault() ?? defaultPic,
                    PositionId = tr.PositionId,
                    PositionName = tr.TeamPosition?.Name ?? "",
                    StartDate = tr.StartDate,
                    EndDate = tr.EndDate,
                    Status = tr.Status,
                    IsCurrentRoster = false
                })
                .OrderByDescending(p => p.EndDate)
                .ThenBy(p => p.Nickname)
                .ToList();
        }

        private IEnumerable<TeamTournamentServiceModel> GetTournamentParticipation(Team team)
        {
            var tournaments = team.TournamentTeams
                .OrderByDescending(tt => tt.Tournament.StartDate)
                .Select(tt => new TeamTournamentServiceModel
                {
                    TournamentId = tt.Tournament.Id,
                    TournamentName = tt.Tournament.FullName,
                    TournamentShortName = tt.Tournament.ShortName,
                    StartDate = tt.Tournament.StartDate,
                    EndDate = tt.Tournament.EndDate,
                    PrizePool = tt.Tournament.PrizePool,
                    Seed = tt.Seed,
                    Placement = team.Placements
                        .Where(p => p.TournamentId == tt.TournamentId)
                        .Select(p => p.Placement)
                        .FirstOrDefault(),
                    PointsAwarded = team.Placements
                        .Where(p => p.TournamentId == tt.TournamentId)
                        .Select(p => p.PointsAwarded)
                        .FirstOrDefault(),
                    OrganizerName = tt.Tournament.Organizer?.FullName ?? "",
                    BannerUrl = tt.Tournament.BannerUrl,
                    IsCompleted = tt.Tournament.EndDate <= DateTime.UtcNow
                })
                .ToList();

            return tournaments;
        }

        private IEnumerable<TeamTrophyServiceModel> GetTeamTrophies(Team team)
        {
            return team.TeamTrophies
                .OrderByDescending(tt => tt.Trophy.AwardDate)
                .Select(tt => new TeamTrophyServiceModel
                {
                    Id = tt.Trophy.Id,
                    Description = tt.Trophy.Description,
                    IconURL = tt.Trophy.IconURL,
                    AwardType = tt.Trophy.AwardType,
                    AwardDate = tt.Trophy.AwardDate
                })
                .ToList();
        }

        private IEnumerable<TeamMatchServiceModel> GetRecentMatches(Team team)
        {
            // Get all matches where this team participated (either as TeamA or TeamB)
            var allMatches = this.data.Matches
                .Where(m => (m.TeamAId == team.Id || m.TeamBId == team.Id) && m.IsCompleted)
                .Include(m => m.Series)
                    .ThenInclude(s => s.Tournament)
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Include(m => m.WinnerTeam)
                .OrderByDescending(m => m.PlayedAt)
                .Take(20) // Last 20 matches
                .ToList();

            // For CS matches, we need to include the Map relationship
            var csMatches = this.data.Matches
                .OfType<MatchCS>()
                .Where(m => (m.TeamAId == team.Id || m.TeamBId == team.Id) && m.IsCompleted)
                .Include(m => m.Map)
                .Include(m => m.Series)
                    .ThenInclude(s => s.Tournament)
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Include(m => m.WinnerTeam)
                .OrderByDescending(m => m.PlayedAt)
                .Take(20)
                .ToList();

            // Combine all matches with proper map names
            var allMatchesWithMaps = allMatches.Select(match => 
            {
                string mapName = "N/A";
                if (match is MatchCS)
                {
                    var csMatch = csMatches.FirstOrDefault(cm => cm.Id == match.Id);
                    mapName = csMatch?.Map?.Name ?? "Unknown";
                }
                
                return new TeamMatchServiceModel
                {
                    MatchId = match.Id,
                    SeriesId = match.SeriesId,
                    TournamentName = match.Series?.Tournament?.FullName ?? "",
                    PlayedAt = match.PlayedAt,
                    OpponentName = match.TeamAId == team.Id ? match.TeamB?.FullName : match.TeamA?.FullName,
                    OpponentTag = match.TeamAId == team.Id ? match.TeamB?.Tag : match.TeamA?.Tag,
                    OpponentLogoUrl = match.TeamAId == team.Id ? match.TeamB?.LogoURL : match.TeamA?.LogoURL,
                    IsWin = match.WinnerTeamId == team.Id,
                    IsCompleted = match.IsCompleted,
                    MapName = mapName,
                    Source = match.Source,
                    ExternalMatchId = match.ExternalMatchId
                };
            }).ToList();

            return allMatchesWithMaps;
        }

        private TeamStatisticsServiceModel GetCurrentRosterStats(Team team, DateTime now)
        {
            // Get current roster player IDs
            var currentPlayerIds = team.Transfers
                .Where(tr => tr.StartDate <= now && 
                           (tr.EndDate == null || tr.EndDate >= now) && 
                           tr.Status == PlayerTeamStatus.Active)
                .GroupBy(tr => tr.PlayerId)
                .Select(g => g.Key)
                .ToList();

            // Get matches where current roster players participated
            // First get the GameProfiles for current roster players
            var currentPlayerUUIDs = this.data.GameProfiles
                .Where(gp => currentPlayerIds.Contains(gp.PlayerId))
                .Select(gp => gp.UUID)
                .ToList();

            var currentRosterMatches = this.data.Matches
                .Where(m => m.PlayerStats.Any(ps => currentPlayerUUIDs.Contains(ps.PlayerUUID)))
                .Include(m => m.Series)
                .ThenInclude(s => s.Tournament)
                .ToList();

            return CalculateStatistics(currentRosterMatches, team.Id);
        }

        private TeamStatisticsServiceModel GetAllTimeStats(Team team)
        {
            // Get all matches where this team played (as TeamA or TeamB)
            var allMatches = team.SeriesAsTeam1.Concat(team.SeriesAsTeam2)
                .SelectMany(s => s.Matches)
                .ToList();

            return CalculateStatistics(allMatches, team.Id);
        }

        private TeamStatisticsServiceModel CalculateStatistics(IEnumerable<Match> matches, int teamId)
        {
            var completedMatches = matches.Where(m => m.IsCompleted).ToList();
            var wins = completedMatches.Count(m => m.WinnerTeamId == teamId);
            var losses = completedMatches.Count - wins;
            var winRate = completedMatches.Any() ? (double)wins / completedMatches.Count * 100 : 0;

            // Tournament statistics
            var tournaments = matches.Select(m => m.Series?.Tournament).Where(t => t != null).Distinct().ToList();
            var tournamentWins = this.data.TournamentPlacements
                .Where(p => p.TeamId == teamId && p.Placement == 1)
                .Count();
            var top3Finishes = this.data.TournamentPlacements
                .Where(p => p.TeamId == teamId && p.Placement <= 3)
                .Count();

            // Points and prize money
            var totalPoints = this.data.TournamentPlacements
                .Where(p => p.TeamId == teamId)
                .Sum(p => p.PointsAwarded);

            var totalPrizeMoney = this.data.TournamentPlacements
                .Where(p => p.TeamId == teamId)
                .Select(p => p.Tournament)
                .Sum(t => t.PrizePool);

            // Map statistics - get CS matches with Map included
            var mapStats = new Dictionary<string, MapStatistics>();
            
            // Get match IDs from completed matches to filter CS matches
            var completedMatchIds = completedMatches.Select(m => m.Id).ToList();
            
            var csMatches = this.data.Matches
                .OfType<MatchCS>()
                .Where(m => completedMatchIds.Contains(m.Id))
                .Include(m => m.Map)
                .ToList();
            
            foreach (var match in csMatches)
            {
                var mapName = match.Map?.Name ?? "Unknown";
                var mapImageUrl = match.Map?.PictureURL ?? "";
                
                if (!mapStats.ContainsKey(mapName))
                {
                    mapStats[mapName] = new MapStatistics 
                    { 
                        MapName = mapName,
                        MapImageUrl = mapImageUrl
                    };
                }
                
                var mapStat = mapStats[mapName];
                mapStat.MatchesPlayed++;
                if (match.WinnerTeamId == teamId)
                    mapStat.Wins++;
                else
                    mapStat.Losses++;
                
                mapStat.WinRate = mapStat.MatchesPlayed > 0 ? (double)mapStat.Wins / mapStat.MatchesPlayed * 100 : 0;
            }

            return new TeamStatisticsServiceModel
            {
                TotalMatches = completedMatches.Count,
                Wins = wins,
                Losses = losses,
                WinRate = winRate,
                TotalTournaments = tournaments.Count,
                TournamentWins = tournamentWins,
                Top3Finishes = top3Finishes,
                TotalPoints = totalPoints,
                TotalPrizeMoney = totalPrizeMoney,
                MapStats = mapStats
            };
        }

        private IEnumerable<TeamStaffServiceModel> GetLegacyPlayers(Team team, DateTime now, string defaultPic)
        {
            // Keep the existing AllPlayers logic for backward compatibility
            return AllPlayers(team.Id);
        }
    }
}
