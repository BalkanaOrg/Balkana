SELECT
    Id,
    Nickname,
    FirstName,
    LastName,
    NationalityId,
    BirthDate,
    PrizePoolWon
FROM dbo.Players
ORDER BY Id
FOR JSON PATH, ROOT('players');
