SELECT Id, FullName, ShortName, IconURL
FROM dbo.Games
ORDER BY Id
FOR JSON PATH, ROOT('games');
