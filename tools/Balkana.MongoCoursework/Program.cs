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
    ?? "balkana_coursework";

var skipClear = args.Contains("--no-clear");

var efOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(sqlConnection)
    .Options;

await using var db = new ApplicationDbContext(efOptions);

var mongoClient = new MongoClient(mongoConnection);
var mongoDb = mongoClient.GetDatabase(databaseName);

var teamsColl = mongoDb.GetCollection<BsonDocument>("teams");
var playersColl = mongoDb.GetCollection<BsonDocument>("players");
var transfersColl = mongoDb.GetCollection<BsonDocument>("playerTeamTransfers");
var tournamentsColl = mongoDb.GetCollection<BsonDocument>("tournaments");
var tournamentTeamsColl = mongoDb.GetCollection<BsonDocument>("tournamentTeams");

if (!skipClear)
{
    await teamsColl.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    await playersColl.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    await transfersColl.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    await tournamentsColl.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    await tournamentTeamsColl.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
}

var teams = await db.Teams.AsNoTracking().ToListAsync();
var players = await db.Players.AsNoTracking().ToListAsync();
var transfers = await db.PlayerTeamTransfers.AsNoTracking().ToListAsync();
var tournaments = await db.Tournaments.AsNoTracking().ToListAsync();
var tournamentTeams = await db.TournamentTeams.AsNoTracking().ToListAsync();

if (teams.Count > 0)
    await teamsColl.InsertManyAsync(teams.Select(ToTeamDoc));

if (players.Count > 0)
    await playersColl.InsertManyAsync(players.Select(ToPlayerDoc));

if (transfers.Count > 0)
    await transfersColl.InsertManyAsync(transfers.Select(ToTransferDoc));

if (tournaments.Count > 0)
    await tournamentsColl.InsertManyAsync(tournaments.Select(ToTournamentDoc));

if (tournamentTeams.Count > 0)
    await tournamentTeamsColl.InsertManyAsync(tournamentTeams.Select(ToTournamentTeamDoc));

await EnsureIndexesAsync(teamsColl, playersColl, transfersColl, tournamentsColl, tournamentTeamsColl);

Console.WriteLine("Mongo coursework sync complete.");
Console.WriteLine($"  teams:              {await teamsColl.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)} (SQL {teams.Count})");
Console.WriteLine($"  players:            {await playersColl.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)} (SQL {players.Count})");
Console.WriteLine($"  playerTeamTransfers:{await transfersColl.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)} (SQL {transfers.Count})");
Console.WriteLine($"  tournaments:        {await tournamentsColl.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)} (SQL {tournaments.Count})");
Console.WriteLine($"  tournamentTeams:    {await tournamentTeamsColl.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)} (SQL {tournamentTeams.Count})");
Console.WriteLine($"Database: {databaseName}");
return;

static BsonDocument ToTeamDoc(Team t)
{
    var d = new BsonDocument
    {
        ["_id"] = t.Id,
        ["tag"] = t.Tag,
        ["fullName"] = t.FullName,
        ["yearFounded"] = t.yearFounded,
        ["logoURL"] = t.LogoURL,
        ["gameId"] = t.GameId,
    };
    if (t.BrandId is { } brandId)
        d["brandId"] = brandId;
    return d;
}

static BsonDocument ToPlayerDoc(Player p)
{
    var d = new BsonDocument
    {
        ["_id"] = p.Id,
        ["nickname"] = p.Nickname,
        ["nationalityId"] = p.NationalityId,
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

static BsonDocument ToTransferDoc(PlayerTeamTransfer tr)
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
        d["positionId"] = posId;
    return d;
}

static BsonDocument ToTournamentDoc(Tournament t)
{
    return new BsonDocument
    {
        ["_id"] = t.Id,
        ["fullName"] = t.FullName,
        ["shortName"] = t.ShortName,
        ["organizerId"] = t.OrganizerId,
        ["description"] = t.Description,
        ["startDate"] = t.StartDate.ToUniversalTime(),
        ["endDate"] = t.EndDate.ToUniversalTime(),
        ["prizePool"] = ToDecimal128(t.PrizePool),
        ["pointsConfiguration"] = t.PointsConfiguration ?? "",
        ["prizeConfiguration"] = t.PrizeConfiguration ?? "",
        ["bannerUrl"] = t.BannerUrl ?? "",
        ["elimination"] = t.Elimination.ToString(),
        ["gameId"] = t.GameId,
        ["isPublic"] = t.IsPublic,
    };
}

static BsonDocument ToTournamentTeamDoc(TournamentTeam tt)
{
    return new BsonDocument
    {
        ["_id"] = ObjectId.GenerateNewId(),
        ["tournamentId"] = tt.TournamentId,
        ["teamId"] = tt.TeamId,
        ["seed"] = tt.Seed,
    };
}

static Decimal128 ToDecimal128(decimal value) => Decimal128.Parse(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

static async Task EnsureIndexesAsync(
    IMongoCollection<BsonDocument> teamsColl,
    IMongoCollection<BsonDocument> playersColl,
    IMongoCollection<BsonDocument> transfersColl,
    IMongoCollection<BsonDocument> tournamentsColl,
    IMongoCollection<BsonDocument> tournamentTeamsColl)
{
    await teamsColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("gameId")));

    await playersColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("nationalityId")));

    await transfersColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("playerId")));
    await transfersColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("teamId")));

    await tournamentsColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("gameId")));
    await tournamentsColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("organizerId")));

    await tournamentTeamsColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("tournamentId").Ascending("teamId"),
        new CreateIndexOptions { Unique = true }));
    await tournamentTeamsColl.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        Builders<BsonDocument>.IndexKeys.Ascending("teamId")));
}
