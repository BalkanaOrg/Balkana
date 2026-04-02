// Run while connected to the target database, e.g.:
//   mongosh "mongodb://127.0.0.1:27017/balkana_coursework" scripts/init-indexes.mongosh.js
// Uses the default `db` from the connection URI.

db.teams.createIndex({ gameId: 1 });
db.players.createIndex({ nationalityId: 1 });
db.playerTeamTransfers.createIndex({ playerId: 1 });
db.playerTeamTransfers.createIndex({ teamId: 1 });
db.tournaments.createIndex({ gameId: 1 });
db.tournaments.createIndex({ organizerId: 1 });
db.tournamentTeams.createIndex({ tournamentId: 1, teamId: 1 }, { unique: true });
db.tournamentTeams.createIndex({ teamId: 1 });

print("Indexes ensured on: " + db.getName());
