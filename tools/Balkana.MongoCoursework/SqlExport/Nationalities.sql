SELECT Id, Name, FlagURL
FROM dbo.Nationalities
ORDER BY Id
FOR JSON PATH, ROOT('nationalities');
