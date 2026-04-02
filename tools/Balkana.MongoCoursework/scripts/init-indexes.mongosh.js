// Run while connected to the target database, e.g.:
//   mongosh "mongodb://127.0.0.1:27017/balkana" scripts/init-indexes.mongosh.js

db.teams.createIndex({ game: 1 });
db.players.createIndex({ nationality: 1 });
db.playerTeamTransfers.createIndex({ playerId: 1 });
db.playerTeamTransfers.createIndex({ teamId: 1 });
db.tournaments.createIndex({ game: 1 });
db.tournaments.createIndex({ organizer: 1 });
db.tournaments.createIndex({ "participants.teamId": 1 });

print("Indexes ensured on: " + db.getName());
