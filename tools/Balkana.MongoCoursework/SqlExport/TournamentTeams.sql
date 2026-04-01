SELECT
    TournamentId,
    TeamId,
    Seed
FROM dbo.TournamentTeams
ORDER BY TournamentId, TeamId
FOR JSON PATH, ROOT('tournamentTeams');
