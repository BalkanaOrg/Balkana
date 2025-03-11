using Balkana.Data;
using Balkana.Data.Models;
using Balkana.Models.Match;
using Balkana.Services.Matches.Models;
using Balkana.Services.Stats.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;

namespace Balkana.Controllers
{
    [Route("match")]
    public class MatchController : Controller
    {
        private readonly ApplicationDbContext data;

        public MatchController(ApplicationDbContext data)
        {
            this.data = data;
        }

        [HttpGet("details/{id?}")]
        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                var seriesList = data.Series.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                }).ToList();
                var mapsList = data.csMaps.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                }).ToList();

                var createViewModel = new MatchCreateViewModel { SeriesList = seriesList, MapsList=mapsList };
                return View("Create", createViewModel);
            }

            var match = data.Matches
                .Include(s => s.Series)
                .Include(c => c.Stats_CS2)
                .ThenInclude(s => s.Player)
                .FirstOrDefault(c => c.Id == id);

            if (match == null) return NotFound();


            var playersList = data.Players
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Nickname
            })
            .ToList();

            var matchDetailsViewModel = new MatchDetailsViewModel
            {
                MatchId = match.Id,
                SeriesName = match.Series.Name,
                PlayerStats = match.Stats_CS2.Select(stat => new PlayerStatViewModel
                {
                    Nickname = stat.Player.Nickname,
                    Damage = stat.Damage,
                    Kills = stat.Kills,
                    Assists = stat.Assists,
                    Deaths = stat.Deaths,
                    UD = stat.UD,
                    FK = stat.FK,
                    FD = stat.FD,
                    HLTV1 = stat.HLTV1,
                    HLTV2 = stat.HLTV2,
                    HSkills = stat.HSkills,
                    KAST = stat.KAST,
                    NoScopeKills = stat.NoScopeKills,
                    CTsideRoundsWon = stat.CTsideRoundsWon,
                    TsideRoundsWon = stat.TsideRoundsWon,
                    CollateralKills = stat.CollateralKills,
                    WallbangKills = stat.WallbangKills,
                    TD = stat.TD,
                    TK = stat.TK,
                    _1k = stat._1k,
                    _2k = stat._2k,
                    _3k = stat._3k,
                    _4k = stat._4k,
                    _5k = stat._5k,
                    _1v1 = stat._1v1,
                    _1v2 = stat._1v2,
                    _1v3 = stat._1v3,
                    _1v4 = stat._1v4,
                    _1v5 = stat._1v5,
                }).ToList() ?? new List<PlayerStatViewModel>(),

                PlayersList = playersList ?? new List<SelectListItem>()
            };

            return View("Details", matchDetailsViewModel);

        }

        [HttpPost("create")]
        public IActionResult Create(MatchCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.SeriesList = data.Series.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                }).ToList();
                model.MapsList = data.csMaps.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                }).ToList();
                return View("Create", model);
            }

            var match = new Match
            {
                SeriesId = model.SeriesId,
                MapId = model.MapId,
                VOD = model.VOD
            };

            data.Matches.Add(match);
            data.SaveChanges();

            return RedirectToAction("Details", new { matchId = match.Id });
        }

        [HttpPost]
        public IActionResult SaveStats(MatchStatisticViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Details", model);

            var match = data.Matches.Include(m => m.Stats_CS2).FirstOrDefault(m => m.Id == model.MatchId);

            if (match == null) return NotFound();

            var statLines = model.BulkStats?.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (statLines == null || statLines.Length != 10) // Ensure exactly 10 players
            {
                ModelState.AddModelError("BulkStats", "You must provide exactly 10 rows of stats.");
                return View("Details", model);
            }

            for (int i = 0; i < 10; i++)
            {
                var playerId = model.SelectedPlayerIds[i];
                if (playerId == 0)
                {
                    ModelState.AddModelError($"SelectedPlayerIds[{i}]", "Select a valid player.");
                    return View("Details", model);
                }
                var statParts = statLines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (statParts.Length != 26 || !statParts.All(s => int.TryParse(s, out _)))
                {
                    ModelState.AddModelError("BulkStats", $"Invalid format on line {i + 1}.");
                    return View("Details", model);
                }

                var existingStat = data.PlayerStatistics_CS2.FirstOrDefault(s => s.MatchId == model.MatchId && s.PlayerId == playerId);

                if (existingStat != null)
                {
                    existingStat.Kills = int.Parse(statParts[0]);
                    existingStat.Assists = int.Parse(statParts[1]);
                    existingStat.Deaths = int.Parse(statParts[2]);
                    existingStat.Damage = int.Parse(statParts[3]);
                    existingStat.KAST = int.Parse(statParts[4]);
                    existingStat.HSkills = int.Parse(statParts[5]);
                    existingStat.HLTV2 = int.Parse(statParts[6]);
                    existingStat.HLTV1 = int.Parse(statParts[7]);
                    existingStat.UD = int.Parse(statParts[8]);
                    existingStat.FK = int.Parse(statParts[9]);
                    existingStat.FD = int.Parse(statParts[10]);
                    existingStat.TK = int.Parse(statParts[11]);
                    existingStat.TD = int.Parse(statParts[12]);
                    existingStat.WallbangKills = int.Parse(statParts[13]);
                    existingStat.CollateralKills = int.Parse(statParts[14]);
                    existingStat.NoScopeKills = int.Parse(statParts[15]);
                    existingStat._5k = int.Parse(statParts[16]);
                    existingStat._4k = int.Parse(statParts[17]);
                    existingStat._3k = int.Parse(statParts[18]);
                    existingStat._2k = int.Parse(statParts[19]);
                    existingStat._1v5 = int.Parse(statParts[20]);
                    existingStat._1v4 = int.Parse(statParts[21]);
                    existingStat._1v3 = int.Parse(statParts[22]);
                    existingStat._1v2 = int.Parse(statParts[23]);
                    existingStat._1v1 = int.Parse(statParts[24]);
                    // Assign all other properties...
                }
                else
                {
                    data.PlayerStatistics_CS2.Add(new PlayerStatistic_CS2
                    {
                        MatchId = model.MatchId,
                        PlayerId = playerId,
                        Kills = int.Parse(statParts[0]),
                        Assists = int.Parse(statParts[1]),
                        Deaths = int.Parse(statParts[2]),
                        Damage = int.Parse(statParts[3]),
                        KAST = int.Parse(statParts[4]),
                        HSkills = int.Parse(statParts[5]),
                        HLTV2 = int.Parse(statParts[6]),
                        HLTV1 = int.Parse(statParts[7]),
                        UD = int.Parse(statParts[8]),
                        FK = int.Parse(statParts[9]),
                        FD = int.Parse(statParts[10]),
                        TK = int.Parse(statParts[11]),
                        TD = int.Parse(statParts[12]),
                        WallbangKills = int.Parse(statParts[13]),
                        CollateralKills = int.Parse(statParts[14]),
                        NoScopeKills = int.Parse(statParts[15]),
                        _5k = int.Parse(statParts[16]),
                        _4k = int.Parse(statParts[17]),
                        _3k = int.Parse(statParts[18]),
                        _2k = int.Parse(statParts[19]),
                        _1v5 = int.Parse(statParts[20]),
                        _1v4 = int.Parse(statParts[21]),
                        _1v3 = int.Parse(statParts[22]),
                        _1v2 = int.Parse(statParts[23]),
                        _1v1 = int.Parse(statParts[24])
                    // Assign all other properties...
                });
                }
            }

            data.SaveChanges();
            return RedirectToAction("Details", new { matchId = model.MatchId });
        }

        //[HttpPost]
        //public IActionResult SaveStats(MatchStatisticViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //        return View("Details", model);

        //    var match = data.Matches.Include(m => m.Stats_CS2).FirstOrDefault(m => m.Id == model.MatchId);

        //    var playerStats = data.Players.Where(c=>c.Transfers).Select(s => new SelectListItem
        //    {
        //        Value = s.Id.ToString(),
        //        Text = s.Nickname
        //    }).ToList();

        //    if (match == null) return NotFound();

        //    var statLines = model.BulkStats?.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        //    if (statLines == null || statLines.Length == 10)
        //    {
        //        ModelState.AddModelError("BulkStats", "You must provide exactly 10 rows of stats.");
        //        return View("Details", model);
        //    }

        //    for (int i = 0; i < 10; i++)
        //    {
        //        var playerId = model.SelectedPlayerIds[i];
        //        if (playerId == 0)
        //        {
        //            ModelState.AddModelError($"SelectedPlayerIds[{i}]", "Select a valid player.");
        //            return View("Details", model);
        //        }
        //        var statParts = statLines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        //        if (statParts.Length != 26 ||
        //            !int.TryParse(statParts[0], out int kills) ||
        //            !int.TryParse(statParts[1], out int deaths) ||
        //            !int.TryParse(statParts[2], out int assists) ||
        //            !int.TryParse(statParts[3], out int Damage) ||
        //            !int.TryParse(statParts[4], out int KAST) ||
        //            !int.TryParse(statParts[5], out int HSkills) ||
        //            !int.TryParse(statParts[6], out int HLTV2) ||
        //            !int.TryParse(statParts[7], out int HLTV1) ||
        //            !int.TryParse(statParts[8], out int UD) ||
        //            !int.TryParse(statParts[9], out int FK) ||
        //            !int.TryParse(statParts[10], out int FD) ||
        //            !int.TryParse(statParts[11], out int TK) ||
        //            !int.TryParse(statParts[12], out int TD) ||
        //            !int.TryParse(statParts[13], out int WallbangKills) ||
        //            !int.TryParse(statParts[14], out int CollateralKills) ||
        //            !int.TryParse(statParts[15], out int NoScopeKills) ||
        //            !int.TryParse(statParts[16], out int _1k) ||
        //            !int.TryParse(statParts[17], out int _2k) ||
        //            !int.TryParse(statParts[18], out int _3k) ||
        //            !int.TryParse(statParts[19], out int _4k) ||
        //            !int.TryParse(statParts[20], out int _5k) ||
        //            !int.TryParse(statParts[21], out int _1v1) ||
        //            !int.TryParse(statParts[22], out int _1v2) ||
        //            !int.TryParse(statParts[23], out int _1v3) ||
        //            !int.TryParse(statParts[24], out int _1v4) ||
        //            !int.TryParse(statParts[25], out int _1v5))
        //        {
        //            ModelState.AddModelError("BulkStats", $"Invalid format on line {i + 1}.");
        //            return View("Details", model);
        //        }
        //        var existingStat = data.PlayerStatistics_CS2
        //            .FirstOrDefault(s => s.MatchId == model.MatchId && s.PlayerId == playerId);


        //        if (existingStat != null)
        //        {
        //            Kills = kills;
        //            Deaths = deaths;
        //            Assists = assists;
        //            UD = UD;
        //            FK = FK;
        //            FD = FD;
        //            HLTV1 = HLTV1;
        //            HLTV2 = HLTV2;
        //            HSkills = HSkills;
        //            KAST = KAST;
        //            NoScopeKills = NoScopeKills;
        //            CollateralKills = CollateralKills;
        //            WallbangKills = WallbangKills;
        //            TD = TD;
        //            TK = TK;
        //            _1k = _1k;
        //            _2k = _2k;
        //            _3k = _3k;
        //            _4k = _4k;
        //            _5k = _5k;
        //            _1v1 = _1v1;
        //            _1v2 = _1v2;
        //            _1v3 = _1v3;
        //            _1v4 = _1v4;
        //            _1v5 = _1v5;
        //        }
        //        else
        //        {
        //            data.PlayerStatistics_CS2.Add(new PlayerStatistic_CS2
        //            {
        //                MatchId = model.MatchId,
        //                PlayerId = playerId,
        //                Kills = kills,
        //                Deaths = deaths,
        //                Assists = assists,
        //                UD = UD,
        //                FK = FK,
        //                FD = FD,
        //                HLTV1 = HLTV1,
        //                HLTV2 = HLTV2,
        //                HSkills = HSkills,
        //                KAST = KAST,
        //                NoScopeKills = NoScopeKills,
        //                CollateralKills = CollateralKills,
        //                WallbangKills = WallbangKills,
        //                TD = TD,
        //                TK = TK,
        //                _1k = _1k,
        //                _2k = _2k,
        //                _3k = _3k,
        //                _4k = _4k,
        //                _5k = _5k,
        //                _1v1 = _1v1,
        //                _1v2 = _1v2,
        //                _1v3 = _1v3,
        //                _1v4 = _1v4,
        //                _1v5 = _1v5,
        //            });
        //        }
        //    }
        //    data.SaveChanges();
        //    return RedirectToAction("Details", new { matchId = model.MatchId });
        //}
    }
}
