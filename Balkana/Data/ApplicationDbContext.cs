namespace Balkana.Data
{
    using Balkana.Data.Models;
    using Balkana.Data.Models.Store;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
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

        public DbSet<PlayerSocials> PlayerSocials { get; set; }
        public DbSet<TeamSocials> TeamSocials { get; set; }
        public DbSet<TournamentSocials> TournamentSocials { get; set; }

        public DbSet<Trophy> Trophies { get; set; }
        public DbSet<TrophyTournament> TrophyTournaments { get; set; }
        public DbSet<TrophyAward> TrophyAwards { get; set; }
        public DbSet<PlayerTrophy> PlayerTrophies { get; set; }
        public DbSet<TeamTrophy> TeamTrophies { get; set; }

        // Match hierarchy
        public DbSet<Match> Matches { get; set; }
        public DbSet<MatchCS> MatchesCS { get; set; }
        public DbSet<MatchLoL> MatchesLoL { get; set; }

        public DbSet<TournamentTeam> TournamentTeams { get; set; }

        // Player statistics hierarchy
        public DbSet<PlayerStatistic> PlayerStats { get; set; }
        public DbSet<PlayerStatistic_CS2> PlayerStatsCS { get; set; }
        public DbSet<PlayerStatistic_LoL> PlayerStatsLoL { get; set; }

        // Tournament Core Player and Circuit Points
        public DbSet<TournamentPlacement> TournamentPlacements { get; set; }
        public DbSet<Core> Cores { get; set; }
        public DbSet<CorePlayer> CorePlayers { get; set; }
        public DbSet<CoreTournamentPoints> CoreTournamentPoints { get; set; }

        //Community
        public DbSet<CommunityTeam> CommunityTeams { get; set; }
        public DbSet<CommunityTeamMember> CommunityTeamMembers { get; set; }
        public DbSet<CommunityInvite> CommunityInvites { get; set; }
        public DbSet<CommunityJoinRequest> CommunityJoinRequests { get; set; }
        public DbSet<CommunityTeamTransfer> CommunityTeamTransfers { get; set; }

        //Articles
        public DbSet<Article> Articles { get; set; }
        
        //Faceit Clubs
        public DbSet<FaceitClub> FaceitClubs { get; set; }

        //Riot Tournament API
        public DbSet<RiotTournament> RiotTournaments { get; set; }
        public DbSet<RiotTournamentCode> RiotTournamentCodes { get; set; }

        //Store
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductCollection> ProductCollections { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }
        public DbSet<InventoryLog> InventoryLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TournamentTeam>()
                .HasKey(tt => new { tt.TournamentId, tt.TeamId });

            modelBuilder.Entity<TournamentTeam>()
                .HasOne(tt => tt.Tournament)
                .WithMany(t => t.TournamentTeams)
                .HasForeignKey(tt => tt.TournamentId);

            modelBuilder.Entity<TournamentTeam>()
                .HasOne(tt => tt.Team)
                .WithMany(t => t.TournamentTeams)
                .HasForeignKey(tt => tt.TeamId);

            modelBuilder.Entity<Series>()
                .HasOne(s => s.NextSeries)
                .WithMany()
                .HasForeignKey(s => s.NextSeriesId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<Series>()
                .HasOne(s => s.WinnerTeam)
                .WithMany()
                .HasForeignKey(s => s.WinnerTeamId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            // Match inheritance mapping (TPH)
            modelBuilder.Entity<Match>()
                .HasDiscriminator<string>("MatchType")
                .HasValue<MatchCS>("CS")
                .HasValue<MatchLoL>("LoL");

            modelBuilder.Entity<Trophy>()
                .HasDiscriminator<string>("TrophyType")
                .HasValue<TrophyTournament>("Tournament")
                .HasValue<TrophyAward>("Player");

            // PlayerStatistic inheritance mapping (TPH)
            modelBuilder.Entity<PlayerStatistic>()
                .HasDiscriminator<string>("StatType")
                .HasValue<PlayerStatistic_CS2>("CS")
                .HasValue<PlayerStatistic_LoL>("LoL");

            // Relationships
            modelBuilder.Entity<TrophyTournament>()
                .HasOne(tt => tt.Tournament)
                .WithMany(t => t.Trophies)
                .HasForeignKey(tt => tt.TournamentId);

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

            //modelBuilder.Entity<Team>()
            //    .HasMany(t => t.SeriesAsTeam1)
            //    .WithOne(s => s.TeamA)
            //    .HasForeignKey(s => s.TeamAId)
            //    .OnDelete(DeleteBehavior.NoAction);

            //modelBuilder.Entity<Team>()
            //    .HasMany(t => t.SeriesAsTeam2)
            //    .WithOne(s => s.TeamB)
            //    .HasForeignKey(s => s.TeamBId)
            //    .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Series>()
                .HasMany(s => s.Matches)
                .WithOne(m => m.Series)
                .HasForeignKey(m => m.SeriesId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Series>()
                .HasMany(s => s.Matches)
                .WithOne(m => m.Series)
                .HasForeignKey(m => m.SeriesId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Series>()
                .HasOne(c=>c.TeamA)
                .WithMany(t => t.SeriesAsTeam1)
                .HasForeignKey(c => c.TeamAId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);
            modelBuilder.Entity<Series>()
                .HasOne(c=>c.TeamB)
                .WithMany(t => t.SeriesAsTeam2)
                .HasForeignKey(c => c.TeamBId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<Match>()
                .HasMany(m => m.PlayerStats)
                .WithOne(ps => ps.Match)
                .HasForeignKey(ps => ps.MatchId);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.WinnerTeam)
                .WithMany()
                .HasForeignKey(m => m.WinnerTeamId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

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
                .HasOne(s => s.Tournament)
                .WithMany(t => t.Series)
                .HasForeignKey(s => s.TournamentId)
                .OnDelete(DeleteBehavior.NoAction);

            //SOCIALS
            modelBuilder.Entity<PlayerSocials>()
                .HasOne(ps => ps.Player)
                .WithMany(p => p.Socials)
                .HasForeignKey(ps => ps.PlayerId);
            modelBuilder.Entity<TeamSocials>()
                .HasOne(ts => ts.Team)
                .WithMany(t => t.Socials)
                .HasForeignKey(ts => ts.TeamId);
            modelBuilder.Entity<TournamentSocials>()
                .HasOne(ts => ts.Tournament)
                .WithMany(t => t.Socials)
                .HasForeignKey(ts => ts.TournamentId);

            //TROPHIES RELATIONS
            modelBuilder.Entity<PlayerTrophy>()
                .HasOne(ta => ta.Player)
                .WithMany(p => p.PlayerTrophies)
                .HasForeignKey(ta => ta.PlayerId);
            modelBuilder.Entity<PlayerTrophy>()
                .HasOne(ta => ta.Trophy)
                .WithMany(t => t.PlayerTrophies)
                .HasForeignKey(ta => ta.TrophyId);
            modelBuilder.Entity<TeamTrophy>()
                .HasOne(ta => ta.Team)
                .WithMany(t => t.TeamTrophies)
                .HasForeignKey(ta => ta.TeamId);
            modelBuilder.Entity<TeamTrophy>()
                .HasOne(ta => ta.Trophy)
                .WithMany(t => t.TeamTrophies)
                .HasForeignKey(ta => ta.TrophyId);

            // Tournament and Seasonal Circuit Points
            modelBuilder.Entity<CorePlayer>()
            .HasKey(cp => new { cp.CoreId, cp.PlayerId });

            modelBuilder.Entity<CorePlayer>()
                .HasOne(cp => cp.Core)
                .WithMany(c => c.Players)
                .HasForeignKey(cp => cp.CoreId);

            modelBuilder.Entity<CorePlayer>()
                .HasOne(cp => cp.Player)
                .WithMany()
                .HasForeignKey(cp => cp.PlayerId);


            //Community
            //modelBuilder.Entity<CommunityTeam>()
            //    .HasOne(c=>c.ApprovedBy)


            // Composite key for CommunityTeamMember
            modelBuilder.Entity<CommunityTeamMember>()
                .HasKey(ctm => new { ctm.CommunityTeamId, ctm.UserId });

            modelBuilder.Entity<CommunityTeamMember>()
                .HasOne(ctm => ctm.CommunityTeam)
                .WithMany(ct => ct.Members)
                .HasForeignKey(ctm => ctm.CommunityTeamId);

            modelBuilder.Entity<CommunityTeamMember>()
                .HasOne(ctm => ctm.User)
                .WithMany() // not mapping back from ApplicationUser
                .HasForeignKey(ctm => ctm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Invite relationships
            modelBuilder.Entity<CommunityInvite>()
                .HasOne(i => i.CommunityTeam)
                .WithMany(t => t.Invites)
                .HasForeignKey(i => i.CommunityTeamId);

            modelBuilder.Entity<CommunityInvite>()
                .HasOne(i => i.InviterUser)
                .WithMany()
                .HasForeignKey(i => i.InviterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommunityInvite>()
                .HasOne(i => i.InviteeUser)
                .WithMany()
                .HasForeignKey(i => i.InviteeUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // CommunityTeamTransfer relationships
            modelBuilder.Entity<CommunityTeamTransfer>()
                .HasOne(ctt => ctt.CommunityTeam)
                .WithMany(ct => ct.Transfers)
                .HasForeignKey(ctt => ctt.CommunityTeamId);

            modelBuilder.Entity<CommunityTeamTransfer>()
                .HasOne(ctt => ctt.User)
                .WithMany()
                .HasForeignKey(ctt => ctt.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Join request relation
            modelBuilder.Entity<CommunityJoinRequest>()
                .HasOne(r => r.CommunityTeam)
                .WithMany() // if you want, add a collection on CommunityTeam and use .WithMany(t=>t.JoinRequests)
                .HasForeignKey(r => r.CommunityTeamId)
                .OnDelete(DeleteBehavior.Cascade);

            // (optional) indexes / other constraints
            modelBuilder.Entity<CommunityTeam>()
                .HasIndex(ct => new { ct.Tag, ct.GameId });

            // Riot Tournament relationships
            modelBuilder.Entity<RiotTournament>()
                .HasOne(rt => rt.Tournament)
                .WithMany()
                .HasForeignKey(rt => rt.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RiotTournament>()
                .HasMany(rt => rt.TournamentCodes)
                .WithOne(tc => tc.RiotTournament)
                .HasForeignKey(tc => tc.RiotTournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RiotTournamentCode>()
                .HasOne(tc => tc.Series)
                .WithMany()
                .HasForeignKey(tc => tc.SeriesId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RiotTournamentCode>()
                .HasOne(tc => tc.TeamA)
                .WithMany()
                .HasForeignKey(tc => tc.TeamAId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RiotTournamentCode>()
                .HasOne(tc => tc.TeamB)
                .WithMany()
                .HasForeignKey(tc => tc.TeamBId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RiotTournamentCode>()
                .HasOne(tc => tc.Match)
                .WithMany()
                .HasForeignKey(tc => tc.MatchDbId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint on tournament code
            modelBuilder.Entity<RiotTournamentCode>()
                .HasIndex(tc => tc.Code)
                .IsUnique();

            // Store relationships
            // ProductCategory self-reference
            modelBuilder.Entity<ProductCategory>()
                .HasOne(pc => pc.ParentCategory)
                .WithMany(pc => pc.SubCategories)
                .HasForeignKey(pc => pc.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product → Category
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product → Team (optional)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Team)
                .WithMany()
                .HasForeignKey(p => p.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product → Player (optional)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Player)
                .WithMany()
                .HasForeignKey(p => p.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProductVariant → Product
            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProductImage → Product
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProductCollection many-to-many
            modelBuilder.Entity<ProductCollection>()
                .HasKey(pc => new { pc.ProductId, pc.CollectionId });

            modelBuilder.Entity<ProductCollection>()
                .HasOne(pc => pc.Product)
                .WithMany(p => p.ProductCollections)
                .HasForeignKey(pc => pc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductCollection>()
                .HasOne(pc => pc.Collection)
                .WithMany(c => c.ProductCollections)
                .HasForeignKey(pc => pc.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order → User (optional for guest checkout)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrderItem → Order
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderItem → ProductVariant
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.ProductVariant)
                .WithMany()
                .HasForeignKey(oi => oi.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ShoppingCart → User
            modelBuilder.Entity<ShoppingCart>()
                .HasOne(sc => sc.User)
                .WithMany()
                .HasForeignKey(sc => sc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ShoppingCartItem → ShoppingCart
            modelBuilder.Entity<ShoppingCartItem>()
                .HasOne(sci => sci.ShoppingCart)
                .WithMany(sc => sc.Items)
                .HasForeignKey(sci => sci.ShoppingCartId)
                .OnDelete(DeleteBehavior.Cascade);

            // ShoppingCartItem → ProductVariant
            modelBuilder.Entity<ShoppingCartItem>()
                .HasOne(sci => sci.ProductVariant)
                .WithMany()
                .HasForeignKey(sci => sci.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventoryLog → ProductVariant
            modelBuilder.Entity<InventoryLog>()
                .HasOne(il => il.ProductVariant)
                .WithMany()
                .HasForeignKey(il => il.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventoryLog → Order (optional)
            modelBuilder.Entity<InventoryLog>()
                .HasOne(il => il.Order)
                .WithMany()
                .HasForeignKey(il => il.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventoryLog → User
            modelBuilder.Entity<InventoryLog>()
                .HasOne(il => il.ChangedByUser)
                .WithMany()
                .HasForeignKey(il => il.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for store
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Slug)
                .IsUnique();

            modelBuilder.Entity<ProductCategory>()
                .HasIndex(pc => pc.Slug)
                .IsUnique();

            modelBuilder.Entity<Collection>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(pv => pv.SKU)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            foreach (var fk in modelBuilder.Model.GetEntityTypes()
             .SelectMany(e => e.GetForeignKeys()))
            {
                fk.DeleteBehavior = DeleteBehavior.NoAction;
            }
        }
    }
}