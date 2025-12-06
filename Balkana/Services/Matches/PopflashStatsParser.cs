using Balkana.Models.Match;
using System.Text.RegularExpressions;

namespace Balkana.Services.Matches
{
    public class PopflashStatsParser
    {
        public class ParsedStats
        {
            public List<PlayerStatsViewModel> TeamAStats { get; set; } = new();
            public List<PlayerStatsViewModel> TeamBStats { get; set; } = new();
            public int? TeamARounds { get; set; }
            public int? TeamBRounds { get; set; }
        }

        public ParsedStats Parse(string pastedText)
        {
            var result = new ParsedStats();
            var lines = pastedText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            // Find all category headers - store all occurrences
            var categoryHeaders = new Dictionary<string, List<int>>();
            var categories = new[] { "BASIC STATS", "FLASH STATS", "KILLS", "TRADE INFO", "SHOTS FIRED", "EXTRA STATS" };
            
            for (int i = 0; i < lines.Count; i++)
            {
                foreach (var category in categories)
                {
                    if (lines[i].StartsWith(category))
                    {
                        if (!categoryHeaders.ContainsKey(category))
                        {
                            categoryHeaders[category] = new List<int>();
                        }
                        categoryHeaders[category].Add(i);
                    }
                }
            }

            // Parse each category
            ParseBasicStats(lines, categoryHeaders, result);
            ParseFlashStats(lines, categoryHeaders, result);
            ParseKills(lines, categoryHeaders, result);
            ParseTradeInfo(lines, categoryHeaders, result);
            ParseShotsFired(lines, categoryHeaders, result);
            ParseExtraStats(lines, categoryHeaders, result);

            return result;
        }

        private void ParseBasicStats(List<string> lines, Dictionary<string, List<int>> headers, ParsedStats result)
        {
            if (!headers.ContainsKey("BASIC STATS") || headers["BASIC STATS"].Count == 0) return;

            var basicStatsHeaders = headers["BASIC STATS"];
            var headerIndex = basicStatsHeaders[0]; // First occurrence = Team A
            
            // Find next category after first BASIC STATS
            var nextCategoryIndex = FindNextCategoryIndex(lines, headerIndex, headers);

            // Parse Team A (first occurrence)
            var teamAEnd = FindCategoryEnd(lines, headerIndex, nextCategoryIndex);
            var teamAStats = ParseBasicStatsForTeam(lines, headerIndex + 1, teamAEnd);
            
            // Parse Team B (second occurrence)
            if (basicStatsHeaders.Count > 1)
            {
                var teamBStart = basicStatsHeaders[1]; // Second occurrence = Team B
                var teamBEnd = FindCategoryEnd(lines, teamBStart, lines.Count);
                var teamBStats = ParseBasicStatsForTeam(lines, teamBStart + 1, teamBEnd);
                
                // Initialize Team B stats if needed
                while (result.TeamBStats.Count < teamBStats.Count)
                {
                    result.TeamBStats.Add(new PlayerStatsViewModel());
                }
                
                // Merge Team B stats into result
                for (int i = 0; i < teamBStats.Count && i < result.TeamBStats.Count; i++)
                {
                    result.TeamBStats[i].Kills = teamBStats[i].Kills;
                    result.TeamBStats[i].Assists = teamBStats[i].Assists;
                    result.TeamBStats[i].Deaths = teamBStats[i].Deaths;
                    result.TeamBStats[i].Damage = teamBStats[i].Damage;
                    result.TeamBStats[i].HLTV1 = teamBStats[i].HLTV1;
                    result.TeamBStats[i].HSkills = teamBStats[i].HSkills;
                    result.TeamBStats[i].UD = teamBStats[i].UD;
                    result.TeamBStats[i].RoundsPlayed = teamBStats[i].RoundsPlayed;
                }
            }

            // Initialize Team A stats
            for (int i = 0; i < teamAStats.Count; i++)
            {
                if (i >= result.TeamAStats.Count)
                {
                    result.TeamAStats.Add(new PlayerStatsViewModel());
                }
                result.TeamAStats[i].Kills = teamAStats[i].Kills;
                result.TeamAStats[i].Assists = teamAStats[i].Assists;
                result.TeamAStats[i].Deaths = teamAStats[i].Deaths;
                result.TeamAStats[i].Damage = teamAStats[i].Damage;
                result.TeamAStats[i].HLTV1 = teamAStats[i].HLTV1;
                result.TeamAStats[i].HSkills = teamAStats[i].HSkills;
                result.TeamAStats[i].UD = teamAStats[i].UD;
                result.TeamAStats[i].RoundsPlayed = teamAStats[i].RoundsPlayed;
            }

            // Initialize Team B stats if not already done
            while (result.TeamBStats.Count < 5)
            {
                result.TeamBStats.Add(new PlayerStatsViewModel());
            }
        }

        private List<PlayerStatsViewModel> ParseBasicStatsForTeam(List<string> lines, int startIndex, int endIndex)
        {
            var stats = new List<PlayerStatsViewModel>();
            int currentIndex = startIndex;
            int playersFound = 0;

            while (currentIndex < endIndex && playersFound < 5)
            {
                // Skip ping (first line after header or after previous player) - it's a number, typically < 200
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int potentialPing) && potentialPing < 200)
                {
                    currentIndex++;
                }

                // Skip player name (non-numeric, non-category header line, doesn't contain % or .)
                if (currentIndex < endIndex && !IsNumeric(lines[currentIndex]) && !IsCategoryHeader(lines[currentIndex]) && 
                    !lines[currentIndex].Contains("%") && !lines[currentIndex].Contains("."))
                {
                    currentIndex++;
                }

                // Now we should be at the stats (11 values: K, A, D, ADR, HLTV, CK, HS, UD, TAR, RP, next ping)
                if (currentIndex >= endIndex) break;

                var playerStat = new PlayerStatsViewModel();
                
                // K (Kills) - line 0
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int kills))
                {
                    playerStat.Kills = kills;
                    currentIndex++;
                }

                // A (Assists) - line 1
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int assists))
                {
                    playerStat.Assists = assists;
                    currentIndex++;
                }

                // D (Deaths) - line 2
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int deaths))
                {
                    playerStat.Deaths = deaths;
                    currentIndex++;
                }

                // ADR (Damage) - line 3
                if (currentIndex < endIndex && double.TryParse(lines[currentIndex], out double adr))
                {
                    playerStat.Damage = (int)Math.Round(adr); // Store ADR as Damage (rounded to int)
                    currentIndex++;
                }

                // HLTV - line 4
                if (currentIndex < endIndex && double.TryParse(lines[currentIndex], out double hltv))
                {
                    playerStat.HLTV1 = hltv;
                    currentIndex++;
                }

                // CK (CollateralKills) - line 5 (we don't have this in PlayerStatsViewModel, skip)
                if (currentIndex < endIndex)
                {
                    currentIndex++;
                }

                // HS (HSkills) - line 6 (decimal like .186, convert to percentage)
                if (currentIndex < endIndex)
                {
                    var hsValue = lines[currentIndex];
                    if (hsValue.StartsWith("."))
                    {
                        if (double.TryParse("0" + hsValue, out double hsDecimal))
                        {
                            playerStat.HSkills = (int)(hsDecimal * 100); // Convert .186 to 18
                        }
                    }
                    else if (int.TryParse(hsValue, out int hsInt))
                    {
                        playerStat.HSkills = hsInt;
                    }
                    currentIndex++;
                }

                // UD (UtilityDamage) - line 7
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int ud))
                {
                    playerStat.UD = ud;
                    currentIndex++;
                }

                // TAR (ignore) - line 8
                if (currentIndex < endIndex)
                {
                    currentIndex++;
                }

                // RP (RoundsPlayed) - line 9
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int rp))
                {
                    playerStat.RoundsPlayed = rp;
                    currentIndex++;
                }

                // Next player's ping (ignore) - line 10 (this is the ping for the next player)
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int nextPing) && nextPing < 200)
                {
                    currentIndex++;
                }

                stats.Add(playerStat);
                playersFound++;
            }

            return stats;
        }

        private void ParseFlashStats(List<string> lines, Dictionary<string, List<int>> headers, ParsedStats result)
        {
            // Flash stats don't seem to be used in PlayerStatsViewModel based on the model
            // But we can parse Flashes if needed - it appears in EXTRA STATS or might be in FLASH STATS
            // For now, skip this category as it's not in the model
        }

        private void ParseKills(List<string> lines, Dictionary<string, List<int>> headers, ParsedStats result)
        {
            if (!headers.ContainsKey("KILLS") || headers["KILLS"].Count == 0) return;

            var killsHeaders = headers["KILLS"];
            var headerIndex = killsHeaders[0]; // First occurrence = Team A
            
            // Find next category after first KILLS
            var nextCategoryIndex = FindNextCategoryIndex(lines, headerIndex, headers);

            // Parse Team A
            var teamAEnd = FindCategoryEnd(lines, headerIndex, nextCategoryIndex);
            var teamAKills = ParseKillsForTeam(lines, headerIndex + 1, teamAEnd);

            // Parse Team B (second occurrence)
            List<Dictionary<string, int>> teamBKills = new();
            if (killsHeaders.Count > 1)
            {
                var teamBStart = killsHeaders[1]; // Second occurrence = Team B
                var teamBEnd = FindCategoryEnd(lines, teamBStart, lines.Count);
                teamBKills = ParseKillsForTeam(lines, teamBStart + 1, teamBEnd);
            }

            // Apply to Team A
            for (int i = 0; i < teamAKills.Count && i < result.TeamAStats.Count; i++)
            {
                result.TeamAStats[i].SniperKills = teamAKills[i].GetValueOrDefault("AWP", 0);
                result.TeamAStats[i]._1k = teamAKills[i].GetValueOrDefault("1K", 0);
                result.TeamAStats[i]._2k = teamAKills[i].GetValueOrDefault("2K", 0);
                result.TeamAStats[i]._3k = teamAKills[i].GetValueOrDefault("3K", 0);
                result.TeamAStats[i]._4k = teamAKills[i].GetValueOrDefault("4K", 0);
                result.TeamAStats[i]._5k = teamAKills[i].GetValueOrDefault("5K", 0);
                result.TeamAStats[i]._1v1 = teamAKills[i].GetValueOrDefault("1v1", 0);
                result.TeamAStats[i]._1v2 = teamAKills[i].GetValueOrDefault("v2", 0);
                result.TeamAStats[i]._1v3 = teamAKills[i].GetValueOrDefault("v3", 0);
                result.TeamAStats[i]._1v4 = teamAKills[i].GetValueOrDefault("v4", 0);
                result.TeamAStats[i]._1v5 = teamAKills[i].GetValueOrDefault("v5", 0);
            }

            // Apply to Team B
            for (int i = 0; i < teamBKills.Count && i < result.TeamBStats.Count; i++)
            {
                result.TeamBStats[i].SniperKills = teamBKills[i].GetValueOrDefault("AWP", 0);
                result.TeamBStats[i]._1k = teamBKills[i].GetValueOrDefault("1K", 0);
                result.TeamBStats[i]._2k = teamBKills[i].GetValueOrDefault("2K", 0);
                result.TeamBStats[i]._3k = teamBKills[i].GetValueOrDefault("3K", 0);
                result.TeamBStats[i]._4k = teamBKills[i].GetValueOrDefault("4K", 0);
                result.TeamBStats[i]._5k = teamBKills[i].GetValueOrDefault("5K", 0);
                result.TeamBStats[i]._1v1 = teamBKills[i].GetValueOrDefault("1v1", 0);
                result.TeamBStats[i]._1v2 = teamBKills[i].GetValueOrDefault("v2", 0);
                result.TeamBStats[i]._1v3 = teamBKills[i].GetValueOrDefault("v3", 0);
                result.TeamBStats[i]._1v4 = teamBKills[i].GetValueOrDefault("v4", 0);
                result.TeamBStats[i]._1v5 = teamBKills[i].GetValueOrDefault("v5", 0);
            }
        }

        private List<Dictionary<string, int>> ParseKillsForTeam(List<string> lines, int startIndex, int endIndex)
        {
            var stats = new List<Dictionary<string, int>>();
            int currentIndex = startIndex;
            int playersFound = 0;

            while (currentIndex < endIndex && playersFound < 5)
            {
                // Skip ping (first line after header or after previous player)
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int potentialPing) && potentialPing < 200)
                {
                    currentIndex++;
                }

                // Skip player name (non-numeric, non-category header line, doesn't contain % or .)
                if (currentIndex < endIndex && !IsNumeric(lines[currentIndex]) && !IsCategoryHeader(lines[currentIndex]) && 
                    !lines[currentIndex].Contains("%") && !lines[currentIndex].Contains("."))
                {
                    currentIndex++;
                }

                if (currentIndex >= endIndex) break;

                var playerKills = new Dictionary<string, int>();

                // AWP - line 0
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int awp))
                {
                    playerKills["AWP"] = awp;
                    currentIndex++;
                }

                // 1K - line 1
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int k1))
                {
                    playerKills["1K"] = k1;
                    currentIndex++;
                }

                // 2K - line 2
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int k2))
                {
                    playerKills["2K"] = k2;
                    currentIndex++;
                }

                // 3K - line 3
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int k3))
                {
                    playerKills["3K"] = k3;
                    currentIndex++;
                }

                // 4K - line 4
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int k4))
                {
                    playerKills["4K"] = k4;
                    currentIndex++;
                }

                // 5K - line 5
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int k5))
                {
                    playerKills["5K"] = k5;
                    currentIndex++;
                }

                // 1v1 - line 6
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int v1))
                {
                    playerKills["1v1"] = v1;
                    currentIndex++;
                }

                // v2 - line 7
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int v2))
                {
                    playerKills["v2"] = v2;
                    currentIndex++;
                }

                // v3 - line 8
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int v3))
                {
                    playerKills["v3"] = v3;
                    currentIndex++;
                }

                // v4 - line 9
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int v4))
                {
                    playerKills["v4"] = v4;
                    currentIndex++;
                }

                // v5 - line 10
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int v5))
                {
                    playerKills["v5"] = v5;
                    currentIndex++;
                }

                // Next player's ping (ignore) - this is the ping for the next player
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int nextPing) && nextPing < 200)
                {
                    currentIndex++;
                }

                stats.Add(playerKills);
                playersFound++;
            }

            return stats;
        }

        private void ParseTradeInfo(List<string> lines, Dictionary<string, List<int>> headers, ParsedStats result)
        {
            if (!headers.ContainsKey("TRADE INFO") || headers["TRADE INFO"].Count == 0) return;

            var tradeInfoHeaders = headers["TRADE INFO"];
            var headerIndex = tradeInfoHeaders[0]; // First occurrence = Team A
            
            // Find next category after first TRADE INFO
            var nextCategoryIndex = FindNextCategoryIndex(lines, headerIndex, headers);

            // Parse Team A
            var teamAEnd = FindCategoryEnd(lines, headerIndex, nextCategoryIndex);
            var teamATrade = ParseTradeInfoForTeam(lines, headerIndex + 1, teamAEnd);

            // Parse Team B (second occurrence)
            List<Dictionary<string, object>> teamBTrade = new();
            if (tradeInfoHeaders.Count > 1)
            {
                var teamBStart = tradeInfoHeaders[1]; // Second occurrence = Team B
                var teamBEnd = FindCategoryEnd(lines, teamBStart, lines.Count);
                teamBTrade = ParseTradeInfoForTeam(lines, teamBStart + 1, teamBEnd);
            }

            // Apply to Team A
            for (int i = 0; i < teamATrade.Count && i < result.TeamAStats.Count; i++)
            {
                if (teamATrade[i].ContainsKey("KAST"))
                {
                    var kastStr = teamATrade[i]["KAST"].ToString();
                    if (kastStr != null && kastStr.EndsWith("%"))
                    {
                        if (int.TryParse(kastStr.TrimEnd('%'), out int kast))
                        {
                            result.TeamAStats[i].KAST = kast;
                        }
                    }
                }
            }

            // Apply to Team B
            for (int i = 0; i < teamBTrade.Count && i < result.TeamBStats.Count; i++)
            {
                if (teamBTrade[i].ContainsKey("KAST"))
                {
                    var kastStr = teamBTrade[i]["KAST"].ToString();
                    if (kastStr != null && kastStr.EndsWith("%"))
                    {
                        if (int.TryParse(kastStr.TrimEnd('%'), out int kast))
                        {
                            result.TeamBStats[i].KAST = kast;
                        }
                    }
                }
            }
        }

        private List<Dictionary<string, object>> ParseTradeInfoForTeam(List<string> lines, int startIndex, int endIndex)
        {
            var stats = new List<Dictionary<string, object>>();
            int currentIndex = startIndex;
            int playersFound = 0;

            while (currentIndex < endIndex && playersFound < 5)
            {
                // Skip ping (first line after header or after previous player)
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int potentialPing) && potentialPing < 200)
                {
                    currentIndex++;
                }

                // Skip player name (non-numeric, non-category header line, doesn't contain % or .)
                if (currentIndex < endIndex && !IsNumeric(lines[currentIndex]) && !IsCategoryHeader(lines[currentIndex]) && 
                    !lines[currentIndex].Contains("%") && !lines[currentIndex].Contains("."))
                {
                    currentIndex++;
                }

                if (currentIndex >= endIndex) break;

                var playerTrade = new Dictionary<string, object>();

                // KAST - line 0 (percentage like "56%")
                if (currentIndex < endIndex)
                {
                    playerTrade["KAST"] = lines[currentIndex];
                    currentIndex++;
                }

                // TR (ignore) - line 1
                if (currentIndex < endIndex)
                {
                    currentIndex++;
                }

                // TRK (ignore) - line 2
                if (currentIndex < endIndex)
                {
                    currentIndex++;
                }

                // TRD (ignore) - line 3
                if (currentIndex < endIndex)
                {
                    currentIndex++;
                }

                // DT% (ignore) - line 4
                if (currentIndex < endIndex)
                {
                    currentIndex++;
                }

                // KT% (ignore) - line 5
                if (currentIndex < endIndex)
                {
                    currentIndex++;
                }

                // TK% (ignore) - line 6
                if (currentIndex < endIndex)
                {
                    currentIndex++;
                }

                // Next player's ping (ignore) - this is the ping for the next player
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int nextPing) && nextPing < 200)
                {
                    currentIndex++;
                }

                stats.Add(playerTrade);
                playersFound++;
            }

            return stats;
        }

        private void ParseShotsFired(List<string> lines, Dictionary<string, List<int>> headers, ParsedStats result)
        {
            // Shots fired stats don't seem to be in PlayerStatsViewModel
            // Skip this category
        }

        private void ParseExtraStats(List<string> lines, Dictionary<string, List<int>> headers, ParsedStats result)
        {
            if (!headers.ContainsKey("EXTRA STATS") || headers["EXTRA STATS"].Count == 0) return;

            var extraStatsHeaders = headers["EXTRA STATS"];
            var headerIndex = extraStatsHeaders[0]; // First occurrence = Team A
            
            // Find next category after first EXTRA STATS
            var nextCategoryIndex = FindNextCategoryIndex(lines, headerIndex, headers);

            // Parse Team A
            var teamAEnd = FindCategoryEnd(lines, headerIndex, nextCategoryIndex);
            var teamAExtra = ParseExtraStatsForTeam(lines, headerIndex + 1, teamAEnd);

            // Parse Team B (second occurrence)
            List<Dictionary<string, int>> teamBExtra = new();
            if (extraStatsHeaders.Count > 1)
            {
                var teamBStart = extraStatsHeaders[1]; // Second occurrence = Team B
                var teamBEnd = FindCategoryEnd(lines, teamBStart, lines.Count);
                teamBExtra = ParseExtraStatsForTeam(lines, teamBStart + 1, teamBEnd);
            }

            // Apply to Team A
            for (int i = 0; i < teamAExtra.Count && i < result.TeamAStats.Count; i++)
            {
                result.TeamAStats[i].FK = teamAExtra[i].GetValueOrDefault("FK", 0);
                result.TeamAStats[i].FD = teamAExtra[i].GetValueOrDefault("FD", 0);
                result.TeamAStats[i].SniperKills = teamAExtra[i].GetValueOrDefault("SK", 0); // Might override from KILLS
                result.TeamAStats[i].PistolKills = teamAExtra[i].GetValueOrDefault("PK", 0);
                result.TeamAStats[i].KnifeKills = teamAExtra[i].GetValueOrDefault("NK", 0);
            }

            // Apply to Team B
            for (int i = 0; i < teamBExtra.Count && i < result.TeamBStats.Count; i++)
            {
                result.TeamBStats[i].FK = teamBExtra[i].GetValueOrDefault("FK", 0);
                result.TeamBStats[i].FD = teamBExtra[i].GetValueOrDefault("FD", 0);
                result.TeamBStats[i].SniperKills = teamBExtra[i].GetValueOrDefault("SK", 0); // Might override from KILLS
                result.TeamBStats[i].PistolKills = teamBExtra[i].GetValueOrDefault("PK", 0);
                result.TeamBStats[i].KnifeKills = teamBExtra[i].GetValueOrDefault("NK", 0);
            }
        }

        private List<Dictionary<string, int>> ParseExtraStatsForTeam(List<string> lines, int startIndex, int endIndex)
        {
            var stats = new List<Dictionary<string, int>>();
            int currentIndex = startIndex;
            int playersFound = 0;

            while (currentIndex < endIndex && playersFound < 5)
            {
                // Skip ping (first line after header or after previous player)
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int potentialPing) && potentialPing < 200)
                {
                    currentIndex++;
                }

                // Skip player name (non-numeric, non-category header line, doesn't contain % or .)
                if (currentIndex < endIndex && !IsNumeric(lines[currentIndex]) && !IsCategoryHeader(lines[currentIndex]) && 
                    !lines[currentIndex].Contains("%") && !lines[currentIndex].Contains("."))
                {
                    currentIndex++;
                }

                if (currentIndex >= endIndex) break;

                var playerExtra = new Dictionary<string, int>();

                // FK - line 0
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int fk))
                {
                    playerExtra["FK"] = fk;
                    currentIndex++;
                }

                // FD - line 1
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int fd))
                {
                    playerExtra["FD"] = fd;
                    currentIndex++;
                }

                // SK (SniperKills) - line 2
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int sk))
                {
                    playerExtra["SK"] = sk;
                    currentIndex++;
                }

                // SD (ignore) - line 3
                if (currentIndex < endIndex)
                {
                    currentIndex++;
                }

                // PK (PistolKills) - line 4
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int pk))
                {
                    playerExtra["PK"] = pk;
                    currentIndex++;
                }

                // PD (ignore) - line 5
                if (currentIndex < endIndex)
                {
                    currentIndex++;
                }

                // NK (KnifeKills) - line 6
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int nk))
                {
                    playerExtra["NK"] = nk;
                    currentIndex++;
                }

                // ND (ignore) - line 7
                if (currentIndex < endIndex)
                {
                    currentIndex++;
                }

                // KARE (ignore) - line 8
                if (currentIndex < endIndex)
                {
                    currentIndex++;
                }

                // DARE (ignore) - line 9
                if (currentIndex < endIndex)
                {
                    currentIndex++;
                }

                // SR (ignore) - line 10
                if (currentIndex < endIndex)
                {
                    currentIndex++;
                }

                // Next player's ping (ignore) - this is the ping for the next player
                if (currentIndex < endIndex && int.TryParse(lines[currentIndex], out int nextPing) && nextPing < 200)
                {
                    currentIndex++;
                }

                stats.Add(playerExtra);
                playersFound++;
            }

            return stats;
        }

        private int FindNextCategoryIndex(List<string> lines, int currentIndex, Dictionary<string, List<int>> headers)
        {
            int minNext = int.MaxValue;
            foreach (var headerList in headers.Values)
            {
                foreach (var header in headerList)
                {
                    if (header > currentIndex && header < minNext)
                    {
                        minNext = header;
                    }
                }
            }
            return minNext == int.MaxValue ? lines.Count : minNext;
        }

        private int FindCategoryEnd(List<string> lines, int startIndex, int maxIndex)
        {
            // Find where the next category starts or where we run out of players
            for (int i = startIndex + 1; i < Math.Min(maxIndex, lines.Count); i++)
            {
                if (IsCategoryHeader(lines[i]))
                {
                    return i;
                }
            }
            return Math.Min(maxIndex, lines.Count);
        }

        private bool IsCategoryHeader(string line)
        {
            var categories = new[] { "BASIC STATS", "FLASH STATS", "KILLS", "TRADE INFO", "SHOTS FIRED", "EXTRA STATS" };
            return categories.Any(c => line.StartsWith(c));
        }

        private bool IsNumeric(string value)
        {
            return int.TryParse(value, out _) || double.TryParse(value, out _);
        }
    }
}

