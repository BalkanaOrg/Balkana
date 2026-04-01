SELECT
    Id,
    PlayerId,
    TeamId,
    StartDate,
    EndDate,
    Status,
    PositionId
FROM dbo.PlayerTeamTransfers
ORDER BY Id
FOR JSON PATH, ROOT('playerTeamTransfers');
