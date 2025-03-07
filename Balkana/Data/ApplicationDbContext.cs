namespace Balkana.Data
{
    using Balkana.Data.Models;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<csMap> csMaps { get; init; }
        public DbSet<Game> Games { get; init; }
        public DbSet<Team> Teams { get; init; }
        public DbSet<Nationality> Nationalities { get; init; }
        public DbSet<Tournament> Tournaments { get; init; }
        public DbSet<Match> Matches { get; init; }
        public DbSet<Player> Players { get; init; }
        public DbSet<PlayerTeamTransfer> PlayerTeamTransfers { get; init; }
        public DbSet<TeamPosition> Positions { get; init; }
        public DbSet<PlayerPicture> Pictures { get; init; }
        public DbSet<PlayerStatistic_CS2> PlayerStatistics_CS2 { get; init; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder
                .Entity<csMap>();
            builder
                .Entity<Game>();
            //Team to Game (CS2, LoL, etc.)
            builder
                .Entity<Team>()
                .HasOne(c => c.Game)
                .WithMany(c => c.Teams)
                .HasForeignKey(c => c.GameId)
                .OnDelete(DeleteBehavior.Restrict);
            //Team and Player to TransferList
            builder
                .Entity<PlayerTeamTransfer>()
                .HasOne(c => c.Player)
                .WithMany(c => c.Transfers)
                .HasForeignKey(c => c.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
            builder
                .Entity<PlayerTeamTransfer>()
                .HasOne(c=> c.Team)
                .WithMany(c => c.Transfers)
                .HasForeignKey(c => c.TeamId)
                .OnDelete(DeleteBehavior.Restrict);
            builder
                .Entity<PlayerTeamTransfer>()
                .HasOne(c=> c.TeamPosition)
                .WithMany(c => c.Transfers)
                .HasForeignKey(c => c.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
            builder
                .Entity<Series>()
                .HasOne(c => c.TeamA)
                .WithMany(c => c.SeriesAsTeam1)
                .HasForeignKey(c => c.TeamAId)
                .OnDelete(DeleteBehavior.Restrict);
            builder
                .Entity<Series>()
                .HasOne(c => c.TeamB)
                .WithMany(c => c.SeriesAsTeam2)
                .HasForeignKey(c => c.TeamBId)
                .OnDelete(DeleteBehavior.Restrict);

            //tuka shte si ebe mamata
            builder
                .Entity<Series>()
                .HasOne(c => c.Match1)
                .WithMany()
                .HasForeignKey(c => c.Match1Id)
                .OnDelete(DeleteBehavior.Restrict);
            builder
                .Entity<Series>()
                .HasOne(c => c.Match2)
                .WithMany()
                .HasForeignKey(c => c.Match2Id)
                .OnDelete(DeleteBehavior.Restrict);
            builder
                .Entity<Series>()
                .HasOne(c => c.Match3)
                .WithMany()
                .HasForeignKey(c => c.Match3Id)
                .OnDelete(DeleteBehavior.Restrict);
            builder
                .Entity<Series>()
                .HasOne(c => c.Match4)
                .WithMany()
                .HasForeignKey(c => c.Match4Id)
                .OnDelete(DeleteBehavior.Restrict);
            builder
                .Entity<Series>()
                .HasOne(c => c.Match5)
                .WithMany()
                .HasForeignKey(c => c.Match5Id)
                .OnDelete(DeleteBehavior.Restrict);
            builder
                .Entity<Series>()
                .HasOne(c => c.Match6)
                .WithMany()
                .HasForeignKey(c => c.Match6Id)
                .OnDelete(DeleteBehavior.Restrict);
            builder
                .Entity<Series>()
                .HasOne(c => c.Match7)
                .WithMany()
                .HasForeignKey(c => c.Match7Id)
                .OnDelete(DeleteBehavior.Restrict);


            //Player to player picture
            builder
                .Entity<PlayerPicture>()
                .HasOne(c => c.Player)
                .WithMany(c => c.PlayerPictures)
                .HasForeignKey(c => c.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
            //Player to nationality
            builder.Entity<Player>()
                .HasOne(c => c.Nationality)
                .WithMany(c=> c.Players)
                .HasForeignKey(c => c.NationalityId)
                .OnDelete(DeleteBehavior.Restrict);
            //Series to Tournament
            builder
                .Entity<Series>()
                .HasOne(c => c.Tournament)
                .WithMany(c => c.Series)
                .HasForeignKey(c => c.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            //Per Player Statistic for CS2
            builder
                .Entity<PlayerStatistic_CS2>()
                .HasOne(c=>c.Player)
                .WithMany(c=>c.Stats_CS)
                .HasForeignKey(c=>c.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
            builder
                .Entity<PlayerStatistic_CS2>()
                .HasOne(c=>c.Match)
                .WithMany(c=>c.Stats_CS2)
                .HasForeignKey(c=>c.MatchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .Entity<Tournament>()
                .HasOne(c=>c.Organizer)
                .WithMany(c=>c.Tournaments)
                .HasForeignKey(c=>c.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
            base.OnModelCreating(builder);
        }
    }
}