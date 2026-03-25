using Balkana.Data;
using Balkana.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Balkana.Services.Tournaments
{
    /// <summary>
    /// Background worker that imports Riot pending matches automatically.
    /// </summary>
    public class RiotPendingMatchAutoImportWorker : BackgroundService
    {
        private readonly IRiotPendingMatchAutoImportQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;

        public RiotPendingMatchAutoImportWorker(
            IRiotPendingMatchAutoImportQueue queue,
            IServiceScopeFactory scopeFactory)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            // On startup, enqueue any still-pending callbacks so automation resumes after restarts.
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var pendingIds = await db.RiotPendingMatches
                    .Where(p => p.Status == RiotPendingMatchStatus.Pending)
                    .OrderBy(p => p.CreatedAt)
                    .Select(p => p.Id)
                    .Take(500)
                    .ToListAsync(cancellationToken);

                foreach (var id in pendingIds)
                    await _queue.EnqueueAsync(id, cancellationToken);
            }
            catch
            {
                // Non-fatal: worker will still process new enqueued items.
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var pendingMatchId in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                await ProcessOneAsync(pendingMatchId, stoppingToken);
            }
        }

        private async Task ProcessOneAsync(int pendingMatchId, CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var importService = scope.ServiceProvider.GetRequiredService<IRiotPendingMatchImportService>();

                var pending = await db.RiotPendingMatches
                    .Include(p => p.RiotTournamentCode)
                    .FirstOrDefaultAsync(p => p.Id == pendingMatchId, ct);

                if (pending == null)
                    return;

                if (pending.Status != RiotPendingMatchStatus.Pending)
                    return;

                if (pending.RiotTournamentCode?.SeriesId == null)
                {
                    pending.Status = RiotPendingMatchStatus.Failed;
                    pending.ErrorMessage = "Cannot auto-import: no SeriesId configured for this Riot tournament code.";
                    await db.SaveChangesAsync(ct);
                    return;
                }

                var seriesId = pending.RiotTournamentCode.SeriesId.Value;
                var (success, error) = await importService.ImportAsync(pendingMatchId, seriesId);
                if (success)
                    return;

                pending.Status = RiotPendingMatchStatus.Failed;
                pending.ErrorMessage = error ?? "Auto-import failed.";
                await db.SaveChangesAsync(ct);
            }
            catch
            {
                // Best-effort: never crash the worker loop.
            }
        }
    }
}

