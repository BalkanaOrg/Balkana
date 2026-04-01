-- Export for mongoimport / manual JSON: run in SSMS, save result as UTF-8.
-- Or use: sqlcmd with -o teams.json after wrapping output appropriately.
SELECT
    Id,
    Tag,
    FullName,
    yearFounded,
    LogoURL,
    GameId,
    BrandId
FROM dbo.Teams
ORDER BY Id
FOR JSON PATH, ROOT('teams');
