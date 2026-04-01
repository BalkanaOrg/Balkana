SELECT
    Id,
    FullName,
    ShortName,
    OrganizerId,
    Description,
    StartDate,
    EndDate,
    PrizePool,
    PointsConfiguration,
    PrizeConfiguration,
    BannerUrl,
    Elimination,
    GameId,
    IsPublic
FROM dbo.Tournaments
ORDER BY Id
FOR JSON PATH, ROOT('tournaments');
