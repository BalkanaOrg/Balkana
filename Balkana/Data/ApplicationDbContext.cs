namespace Balkana.Data
{
    using Balkana.Data.Models;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Core sets
        public DbSet<Game> Games { get; set; }
        public DbSet<GameMap> GameMaps { get; set; }
        public DbSet<ItemReference> Items { get; set; }

        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamPosition> Positions { get; set; }

        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerPicture> Pictures { get; set; }
        public DbSet<PlayerTeamTransfer> PlayerTeamTransfers { get; set; }
        public DbSet<GameProfile> GameProfiles { get; set; }   // ✅ NEW

        public DbSet<Nationality> Nationalities { get; set; }

        public DbSet<Organizer> Organizers { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<Series> Series { get; set; }

        // Match hierarchy
        public DbSet<Match> Matches { get; set; }
        public DbSet<MatchCS> MatchesCS { get; set; }
        public DbSet<MatchLoL> MatchesLoL { get; set; }

        // Player statistics hierarchy
        public DbSet<PlayerStatistic> PlayerStats { get; set; }
        public DbSet<PlayerStatistic_CS2> PlayerStatsCS { get; set; }
        public DbSet<PlayerStatistic_LoL> PlayerStatsLoL { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Match inheritance mapping (TPH)
            modelBuilder.Entity<Match>()
                .HasDiscriminator<string>("MatchType")
                .HasValue<MatchCS>("CS")
                .HasValue<MatchLoL>("LoL");

            // PlayerStatistic inheritance mapping (TPH)
            modelBuilder.Entity<PlayerStatistic>()
                .HasDiscriminator<string>("StatType")
                .HasValue<PlayerStatistic_CS2>("CS")
                .HasValue<PlayerStatistic_LoL>("LoL");

            // Relationships
            // Team → Game
            modelBuilder.Entity<Team>()
                .HasOne(t => t.Game)
                .WithMany(g => g.Teams)
                .HasForeignKey(t => t.GameId)
                .OnDelete(DeleteBehavior.NoAction);

            // Tournament → Game
            modelBuilder.Entity<Tournament>()
                .HasOne(t => t.Game)
                .WithMany(g => g.Tournaments)
                .HasForeignKey(t => t.GameId)
                .OnDelete(DeleteBehavior.NoAction);

            // TeamPosition → Game
            modelBuilder.Entity<TeamPosition>()
                .HasOne(tp => tp.Game)
                .WithMany(g => g.Positions)
                .HasForeignKey(tp => tp.GameId)
                .OnDelete(DeleteBehavior.NoAction);

            // GameMap → Game
            modelBuilder.Entity<GameMap>()
                .HasOne(m => m.Game)
                .WithMany()
                .HasForeignKey(m => m.GameId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Team>()
                .HasMany(t => t.SeriesAsTeam1)
                .WithOne(s => s.TeamA)
                .HasForeignKey(s => s.TeamAId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Team>()
                .HasMany(t => t.SeriesAsTeam2)
                .WithOne(s => s.TeamB)
                .HasForeignKey(s => s.TeamBId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Series>()
                .HasMany(s => s.Matches)
                .WithOne(m => m.Series)
                .HasForeignKey(m => m.SeriesId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Series>()
                .HasMany(s=>s.Matches)
                .WithOne(m => m.Series)
                .HasForeignKey(m => m.SeriesId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Match>()
                .HasMany(m => m.PlayerStats)
                .WithOne(ps => ps.Match)
                .HasForeignKey(ps => ps.MatchId);

            modelBuilder.Entity<Player>()
                .HasMany(p => p.Transfers)
                .WithOne(t => t.Player)
                .HasForeignKey(t => t.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Player>()
                .HasMany(p => p.PlayerPictures)
                .WithOne(pp => pp.Player)
                .HasForeignKey(pp => pp.PlayerId);

            // ✅ Player ↔ GameProfiles (One-to-Many)
            modelBuilder.Entity<Player>()
                .HasMany(p => p.GameProfiles)
                .WithOne(gp => gp.Player)
                .HasForeignKey(gp => gp.PlayerId);

            modelBuilder.Entity<Team>()
                .HasMany(t => t.Transfers)
                .WithOne(tr => tr.Team)
                .HasForeignKey(tr => tr.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TeamPosition>()
                .HasMany(tp => tp.Transfers)
                .WithOne(tr => tr.TeamPosition)
                .HasForeignKey(tr => tr.PositionId);

            // Optional: enforce unique ExternalMatchId per source
            modelBuilder.Entity<Match>()
                .HasIndex(m => new { m.ExternalMatchId, m.Source })
                .IsUnique();

            // Optional: enforce unique profile per provider
            modelBuilder.Entity<GameProfile>()
                .HasIndex(gp => new { gp.UUID, gp.Provider })
                .IsUnique();

            modelBuilder.Entity<ItemReference>()
                .HasNoKey();

            modelBuilder.Entity<Series>()
                .HasOne(s=>s.Tournament)
                .WithMany(t => t.Series)
                .HasForeignKey(s => s.TournamentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Series>()
                .HasOne(s => s.TeamA)
                .WithMany(t => t.SeriesAsTeam1)
                .HasForeignKey(s => s.TeamAId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Series>()
                .HasOne(s => s.TeamB)
                .WithMany(t => t.SeriesAsTeam2)
                .HasForeignKey(s => s.TeamBId)
                .OnDelete(DeleteBehavior.NoAction);

            foreach (var fk in modelBuilder.Model.GetEntityTypes()
             .SelectMany(e => e.GetForeignKeys()))
            {
                fk.DeleteBehavior = DeleteBehavior.NoAction;
            }
        }
    }
}