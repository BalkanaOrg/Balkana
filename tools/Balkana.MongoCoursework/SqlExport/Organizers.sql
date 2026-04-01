SELECT Id, FullName, Tag, Description, LogoURL
FROM dbo.Organizers
ORDER BY Id
FOR JSON PATH, ROOT('organizers');
