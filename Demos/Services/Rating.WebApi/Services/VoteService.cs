using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Rating.BusinessLayer.Data;
using Rating.BusinessLayer.HostedServices;
using System.Diagnostics;
using Unir.Framework.Observability.Abstractions;

namespace Rating.BusinessLayer.Services;

public class VoteService : IVoteService
{
    private readonly VotingDbContext _context;
    private readonly RedisCacheService _cache;
    private readonly ActivitySource _activitySource;
    private readonly Activity? _activity;

    public VotingDbContext DbContext => _context;

    public VoteService(VotingDbContext context, RedisCacheService cache, ActivitySource activitySource)
    {
        _context = context;
        _cache = cache;
        _activitySource = activitySource;
        _activity = Activity.Current;
    }

    public async Task<double> SubmitVoteAsync(IActivityProcess step, Dom.Vote vote, string productName)
    {
        using (var activity = step.ChildActivity("2.3.1 Guardar en DB"))
        {
            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();
            activity.Label("voteId", vote.Id);
            activity.Label("valor", vote.Valor);
            activity.Label("producto", productName);
            activity.Label("componente", nameof(VoteService));
        }

        double average;

        using (var activity = step.ChildActivity("2.3.2 Calcular promedio"))
        {
            average = await _context.Votes
                .Where(v => v.ProductoId == vote.ProductoId)
                .AverageAsync(v => v.Valor);

            activity.Label("average", average);
            activity.Label("Componente", nameof(VoteService));
        }

        using (var activity = step.ChildActivity("2.3.3 Enviar promedio a Redis"))
        {
            await _cache.SetAverageAsync(vote.ProductoId, average);
        }

        return average;
    }

    public async Task<double?> GetAverageVoteAsync(Guid productoId)
    {
        if (_cache == null) return 0;

        var cached = await _cache.GetAverageAsync(productoId);
        if (cached is not null) return cached;

        var average = await _context.Votes
            .Where(v => v.ProductoId == productoId)
            .AverageAsync(v => (double?)v.Valor);

        if (average is not null)
            await _cache.SetAverageAsync(productoId, average.Value);

        return average;
    }

    public async Task<List<Dom.Vote>> GetAll()
    {
        return await _context.Votes.ToListAsync();
    }

    public async Task<Dom.Vote?> ChangeVoteStatus(Guid voteId, EstadoVoto estadoVoto)
    {
        var vote = await _context.Votes
            .Include(p=>p.Producto)
            .FirstOrDefaultAsync(v => v.Id == voteId)
            ;

        if (vote != null)
        {
            vote.Estado = estadoVoto;
            await _context.SaveChangesAsync();
        }

        return vote;
    }
}