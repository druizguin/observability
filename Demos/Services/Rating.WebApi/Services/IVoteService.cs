using Microsoft.EntityFrameworkCore;
using Rating.BusinessLayer.Data;
using Rating.BusinessLayer.HostedServices;
using Unir.Framework.Observability.Abstractions;

namespace Rating.BusinessLayer.Services
{
    public interface IVoteService
    {
        VotingDbContext DbContext {get; }

        Task<double> SubmitVoteAsync(IActivityProcess step, Dom.Vote vote, string productName);
        Task<double?> GetAverageVoteAsync(Guid productoId);
        Task<List<Dom.Vote>> GetAll();
        Task<Dom.Vote?> ChangeVoteStatus(Guid voteId, EstadoVoto estadoVoto);
    }
}