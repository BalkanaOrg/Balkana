using Balkana.Data;
using Balkana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

static string? Env(params string[] keys)
{
    foreach (var key in keys)
    {
        var v = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(v))
            return v;
    }
    return null;
}

const string LookupFallback = "Unknown";

var basePath = AppContext.BaseDirectory;
var config = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var sqlConnection = config.GetConnectionString("DefaultConnection")
    ?? Env("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException(
        "Set ConnectionStrings:DefaultConnection in appsettings.json or environment ConnectionStrings__DefaultConnection.");

var mongoConnection = config["MongoCoursework:ConnectionString"]
    ?? Env("MongoCoursework__ConnectionString", "MONGO_COURSEWORK_CONNECTION_STRING")
    ?? throw new InvalidOperationException(
        "Set MongoCoursework:ConnectionString or MONGO_COURSEWORK_CONNECTION_STRING.");

var databaseName = config["MongoCoursework:DatabaseName"]
    ?? Env("MongoCoursework__DatabaseName")
    ?? "balkana";

var skipClear = args.Contains("--no-clear");

var efOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(sqlConnection)
    .Options;

await using var db = new ApplicationDbContext(efOptions);

var nationalities = await db.Nationalities.AsNoTracking().ToDictionaryAsync(n => n.Id, n => n.Name);
var games = await db.Games.AsNoTracking().ToDictionaryAsync(g => g.Id, g => g.FullName);
var organizers = await db.Organizers.AsNoTracking().ToDictionaryAsync(o => o.Id, o => o.FullName);
var positions = await db.Positions.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Name);

var teams = await db.Teams.AsNoTracking().ToListAsync();
var players = await db.Players.AsNoTracking().ToListAsync();
var transfers = await db.PlayerTeamTransfers.AsNoTracking().ToListAsync();
var tournaments = await db.Tournaments.AsNoTracking().ToListAsync();
var tournamentTeams = await db.TournamentTeams.AsNoTracking().ToListAsync();

var participantsByTournament = tournamentTeams
    .GroupBy(tt => tt.TournamentId)
    .ToDictionary(g => g.Key, g => g.OrderBy(tt => tt.Seed).ToList());

var mongoClient = new MongoClient(mongoConnection);
var mongoDb = mongoClient.GetDatabase(databaseName);

var teamsColl = mongoDb.GetCollection<BsonDocument>("teams");
var playersColl = mongoDb.GetCollection<BsonDocument>("players");
var transfersColl = mongoDb.GetCollection<BsonDocument>("playerTeamTransfers");
var tournamentsColl = mongoDb.GetCollection<BsonDocument>("tournaments");

if (!skipClear)
{
    await teamsColl.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    await playersColl.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    await transfersColl.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    await tournamentsColl.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    // Remove legacy collection from older ETL runs
    await mongoDb.GetCollection<BsonDocument>("tournamentTeams").DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
}

if (teams.Count > 0)
    await teamsColl.InsertManyAsync(teams.Select(t => ToTeamDoc(t, games)));

if (players.Count > 0)
    await playersColl.InsertManyAsync(players.Select(p => ToPlayerDoc(p, nationalities)));

if (transfers.Count > 0)
    await transfersColl.InsertManyAsync(transfers.Select(tr => ToTransferDoc(tr, positions)));

if (tournaments.Count > 0)
{
    await tournamentsColl.InsertManyAsync(tournaments.Select(t =>
        ToTournamentDoc(
            t,
            participantsByTournament.GetValueOrDefault(t.Id, []),
            games,
            organizers)));
}

await EnsureIndexesAsync(teamsColl, playersColl, transfersColl, tournamentsColl);

var participantRows = tournamentTeams.Count;
Console.WriteLine("Mongo sync complete (denormalized, 4 collections).");
Console.WriteLine($"  teams:               {await teamsColl.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)} (SQL {teams.Count})");
Console.WriteLine($"  players:             {await playersColl.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)} (SQL {players.Count})");
Console.WriteLine($"  playerTeamTransfers: {await transfersColl.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)} (SQL {transfers.Count})");
Console.WriteLine($"  tournaments:       {await tournamentsColl.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)} (SQL {tournaments.Count})");
Console.WriteLine($"  participant rows embedded: {participantRows} (SQL TournamentTeams)");
Console.WriteLine($"Database: {databaseName}");
return;

static BsonDocument ToTeamDoc(Team t, IReadOnlyDictionary<int, string> gameNames)
{
    var d = new BsonDocument
    {
        ["_id"] = t.Id,
        ["tag"] = t.Tag,
        ["fullName"] = t.FullName,
        ["yearFounded"] = t.yearFounded,
        ["logoURL"] = t.LogoURL,
        ["game"] = gameNames.GetValueOrDefault(t.GameId, LookupFallback),
    };
    if (t.BrandId is { } brandId)
        d["brandId"] = brandId;
    return d;
}

static BsonDocument ToPlayerDoc(Player p, IReadOnlyDictionary<int, string> nationalityNames)
{
    var d = new BsonDocument
    {
        ["_id"] = p.Id,
        ["nickname"] = p.Nickname,
        ["nationality"] = nationalityNames.GetValueOrDefault(p.NationalityId, LookupFallback),
        ["prizePoolWon"] = ToDecimal128(p.PrizePoolWon),
    };
    if (p.FirstName is { } fn)
        d["firstName"] = fn;
    if (p.LastName is { } ln)
        d["lastName"] = ln;
    if (p.BirthDate is { } bd)
        d["birthDate"] = bd.ToUniversalTime();
    return d;
}

static BsonDocument ToTransferDoc(PlayerTeamTransfer tr, IReadOnlyDictionary<int, string> positionNames)
{
    var d = new BsonDocument
    {
        ["_id"] = tr.Id,
        ["playerId"] = tr.PlayerId,
        ["startDate"] = tr.StartDate.ToUniversalTime(),
        ["status"] = tr.Status.ToString(),
    };
    if (tr.TeamId is { } teamId)
        d["teamId"] = teamId;
    if (tr.EndDate is { } end)
        d["endDate"] = end.ToUniversalTime();
    if (tr.PositionId is { } posId)
        d["position"] = positionNames.GetValueOrDefault(posId, LookupFallback);
    return d;
}

static BsonDocument ToTournamentDoc(
    Tournament t,
    IReadOnlyList<TournamentTeam> participants,
    IReadOnlyDictionary<int, string> gameNames,
    IReadOnlyDictionary<int, string> organizerNames)
{
    var arr = new BsonArray();
    foreach (var tt in participants)
    {
        arr.Add(new BsonDocument
        {
            ["seed"] = tt.Seed,
            ["teamId"] = tt.TeamId,
        });
    }

    return new BsonDocument
    {
        ["_id"] = t.Id,
        ["fullName"] = t.FullName,
        ["shortName"] = t.ShortName,
        ["organizer"] = organizerNames.GetValueOrDefault(t.OrganizerId, LookupFallback),
        ["description"] = t.Description,
        ["startDate"] = t.StartDate.ToUniversalTime(),
        ["endDate"] = t.EndDate.ToUniversalTime(),
        ["prizePool"] = ToDecimal128(t.PrizePool),
        ["pointsConfiguration"] = t.PointsConfiguration ?? "",
        ["prizeConfiguration"] = t.PrizeConfiguration ?? "",
        ["bannerUrl"] = t.BannerUrl ?? "",
        ["elimination"] = t.Elimination.ToString(),
        ["game"] = gameNames.GetValueOrDefault(t.GameId, LookupFallback),
        ["isPublic"] = t.IsPublic,
        ["participants"] = arr,
    };
}

static Decimal128 ToDecimal128(decimal value) =>
    Decimal128.Parse(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

static async Task EnsureIndexesAsync(
    IMongoCollection<BsonDocument> teamsColl,
    IMongoCollection<BsonDocument> playersColl,
    IMongoCollection<BsonDocument> transfersColl,
    IMongoCollection<BsonDocument> tournamentsColl)
{
    await teamsColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("game")));

    await playersColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("nationality")));

    await transfersColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("playerId")));
    await transfersColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("teamId")));

    await tournamentsColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("game")));
    await tournamentsColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("organizer")));
    await tournamentsColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("participants.teamId")));
}
