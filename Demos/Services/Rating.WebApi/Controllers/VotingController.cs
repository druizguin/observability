#define ASYNCMODE
namespace Rating.BusinessLayer.Controllers;

using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Context.Propagation;
using Rating.BusinessLayer.Dom;
using Rating.BusinessLayer.HostedServices;
using Rating.BusinessLayer.Services;
using System.Diagnostics;
using Unir.Framework.Observability;
using Unir.Framework.Observability.Abstractions;

[ApiController]
[Route("api/[controller]")]
public class VotingController : ObservabilityControllerBase<VotingController>
{
    private readonly IVoteService _voteService;
    private readonly VotePublisher _votePublisher;
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    // [Demo V2] 2.2. Code: Inyección ObservabilityService 
    public VotingController(
        Microsoft.Extensions.Logging.ILogger<VotingController> logger,
        IVoteService voteService, IServiceProvider services,
        VotePublisher votePublisher) : base(services)
    {
        _voteService = voteService;
        _votePublisher = votePublisher;
    }

    /// <summary>
    /// Registra un voto para un producto.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> SubmitVote([FromBody] VoteDto vote, ApiHeaders? tenant, CancellationToken cancelToken)
    {
        IActionResult result2 = null;
        var context = new RatingObservabilityContext
        {
            Usuario = vote.UserKey,
            Valor = vote.Valor,
            Fecha = vote.Fecha,
            Pais = vote.Country,
            Tenant = tenant?.Tenant ?? "unknown"
        };
#if ASYNCMODE
#else
#endif

#if ASYNCMODE
        var result = _obs.Traces
        .Activity("B Realizar votación", ActivityKind.Server)
        .Execute<IActionResult>(activity =>
#else
        var result = await _obs.Traces
        .Activity("B Realizar votación", ActivityKind.Server)
        .ExecuteAsync<IActionResult>(async activity =>
#endif
        {
            var result1 = activity
            .ChildActivity("2.1 verificar datos del voto", ActivityKind.Internal)
            .Execute(step1 =>
            {
                step1.LabelContext(vote, "SubmitVote.2.1");

                _obs.Logger.LogInformation("2.1 Verificar que la peticion es correcta");

                if (!ModelState.IsValid)
                {
                    var result = new Dictionary<string, object>();
                    foreach (var item in ModelState)
                    {
                        var errors = item.Value.Errors.Select(x => x.ErrorMessage).ToArray();
                        result.Add(item.Key, errors);
                    }

                    return BadRequest(new { response = "Solicitud inválida", Errors = result });
                }
                return null;
            });

            if (result1 != null) return result1;

            Product? product = null;

            var result2 = activity
            .ChildActivity("2.2 verificar existencia del producto", ActivityKind.Internal)
            .Execute(step2 =>
            {
                _obs.Logger.LogInformation("2.2 Verificar que el producto existe");

                product = _voteService.DbContext.Products.FirstOrDefault(p => p.Id == vote.ProductoId);

                if (product is null)
                {
                    return BadRequest(new
                    {
                        response = "Solicitud inválida",
                        Errors = new Dictionary<string, string> { { "ProductoId", $"No existe el producto {vote.ProductoId}" } }
                    });
                }
                else
                {
                    _obs.Logger.LogInformation($"2.2 Producto encontrado: {product.Name}");
                }

                activity.Label(("step2.Producto", product.Name), ("step2.Categoria", product.Category));

                context.Producto = product.Name;
                context.Categoria = product.Category;
                return null;
            });
            if (result2 != null) return result2;

            double avg = 0;

#if ASYNCMODE
            activity
            .ChildActivity("2.3 crear voto en BD", ActivityKind.Internal)
                .Execute(step3 =>
#else
            await activity
            .ChildActivity("2.3 crear voto en BD", ActivityKind.Internal)
                .ExecuteAsync(async step3 =>
#endif
                {
                    var newVote = new Vote
                    {
                        Id = Guid.NewGuid(),
                        Comentario = vote.Comentario,
                        Estado = EstadoVoto.Pending,
                        ProductoId = vote.ProductoId,
                        Fecha = vote.Fecha,
                        UserKey = vote.UserKey,
                        Valor = vote.Valor,
                    };

                    _obs.Logger.LogInformation("2.3 Recibido voto para el producto {ProductId} con valor {Value}", newVote.ProductoId, newVote.Valor);
#if ASYNCMODE
                    avg = _voteService.SubmitVoteAsync(step3, newVote, context.Producto).GetAwaiter().GetResult();
#else
                    avg = await _voteService.SubmitVoteAsync(step3, newVote, context.Producto);
#endif
                    _obs.Logger.LogInformation("Voto registrado con ID {VoteId}", newVote.Id);
                    vote.Id = newVote.Id;

                    step3.Label("step3.avg", avg);
                    step3.Label("step3.id", newVote.Id);
                });

            activity
            .ChildActivity("2.4 Métricas", ActivityKind.Internal)
            .Execute(step4 =>
                    {
                        step4.Label(
                            ("step4.metricas", 3),
                            ("step4.avg", avg),
                            ("step4.usuario", vote.UserKey),
                            ("step4.pais", vote.Country),
                            ("step4.valor", vote.Valor));

                        // [Demo V2] 2.3.4 Code.Metrics: Métricas

                        _obs.Metrics
                            .Counter("votes", "número de votos realizado")
                            .LabelContext(context)
                            .Label("UTCDate", DateTime.UtcNow)
                            .Up(1);

                        _obs.Metrics
                            .Gauge("votes.avg", "Promedio de votos actual")
                            .LabelContext(context)
                            .Record(avg);

                        _obs.Metrics
                            .Histogram("votes.value", "Valor de la votación realizada")
                            .LabelContext(context)
                            .Record(vote.Valor);

                        // Fluent API
                        _obs.Metrics.UpDownCounter("votes.temperature")
                            .WithDescription("Descripción de la temperatura")
                            .LabelContext(context)
                            .WithUnit("C")
                            .Label(
                                ("location", "ES"), ("Indicador", Random.Shared.Next(1, 10))
                            ).Record(Random.Shared.Next(1, 10));

                        _obs.Metrics
                           .Counter("voto.valor", "Valor del voto realizado")
                           .LabelContext(context)
                           .Up(vote.Valor);

                        _obs.Logger.LogInformation($"2.4 Promedio de votos: {avg}");


                        //#if ASYNCMODE
                        //                           //   _obs.Metrics.IncrementCounterAsync<int>("votes", activity.Tags, 1).GetAwaiter().GetResult();
                        //                           //_obs.Metrics.GaugeRecord<double>("votes", avg, activity.Labels, "avg");
                        //                           //_obs.Metrics.HistogramAsync<double>("votes", vote.Valor, activity.Tags).GetAwaiter().GetResult();
                        //#else
                        //                    await activity.Metrics.IncrementCounterAsync("votes");
                        //                    await activity.Metrics.GaugeRecordAsync<double>("votes", avg, activity.Labels, "avg");
                        //                    await activity.Metrics.HistogramAsync<double>("votes", vote.Valor, activity.Labels);
                        //#endif

                    });

            if (true)
            {
#if ASYNCMODE
                activity
                .ChildActivity("2.5 Solicitar aprobación Rabbit", ActivityKind.Internal)
                .Execute(step5 =>
#else
            await activity
            .ChildActivity("2.5 Solicitar aprobación Rabbit", ActivityKind.Internal)
            .ExecuteAsync(async step5 =>
#endif
                {
                    context.RabbitQueue = _votePublisher.RabbitQeueName;
                    context.RabbitRol = "publisher";

                    _obs.Logger.LogInformation("2.5 Publicando voto con ID {VoteId} para procesamiento asíncrono con correlationId={correlationId}", vote.Id, activity.Activity?.Id);
#if ASYNCMODE
                    _votePublisher.PublishVote(vote, product, step5).GetAwaiter().GetResult();
#else
                await _votePublisher.PublishVote(vote, product, step5);
#endif
                    _obs.Logger.LogInformation("2.5 Voto con ID {VoteId} publicado correctamente", vote.Id);

                    step5.Label(
                        ("step5.RabbitQueue", context.RabbitQueue),
                        ("step5.RabbitRol", context.RabbitRol));
                });
            }

            activity.LabelContext(context);



            return Ok(new { message = "Voto registrado correctamente.", voto = vote });
        },
#if ASYNCMODE
        (activity, ex) =>  // On Error
#else
        async (activity, ex) =>  // On Error
#endif
        {
            _obs.Metrics
                .Counter("vote.errors", "número de errores en el API de votacion")
                .LabelFromDictionary(activity.Labels)
                .Label("error", ex.Message)
                .Up(1);
#if ASYNCMODE
            return BadRequest("Error: " + ex.Message);
#else
            return await Task.FromResult(BadRequest("Error: " + ex.Message));
#endif
        });

        return result;
    }


    [HttpPost("{voteId}/{estadoVoto}")]
    public async Task<IActionResult> ChangeStatus([FromRoute] Guid voteId, [FromRoute] EstadoVoto estadoVoto)
    {
        IActionResult result =
            _obs.Traces
            .Configure("4.1 Cambiar estado votación", ActivityKind.Server)
            .CorrelateFrom(this.Request)
            .Build()
            .Execute<IActionResult>(
            activity =>
            {
                activity.Labels["id"] = voteId;
                activity.Labels["status"] = estadoVoto;

                _obs.Logger.LogInformation($"B.2 Set estado voto: {voteId}. estado: {estadoVoto}");
                var vote = _voteService.ChangeVoteStatus(voteId, estadoVoto).GetAwaiter().GetResult();

                if (vote is null)
                {
                    activity.Activity?.SetStatus(ActivityStatusCode.Error, $"Not found vote id {voteId}");
                    _obs.Logger.LogWarning($"Voto con ID {voteId} no encontrado");
                    return NotFound("Voto no encontrado");
                }

                _obs.Logger.LogInformation($"Voto con ID {vote.Id} del producto  {vote.Producto.Name} cambiado correctamente a {estadoVoto}");

                _obs.Metrics.Counter($"votes.{estadoVoto}")
                    .Label("status", estadoVoto.ToString())
                    .LabelContext(vote as VoteDto)
                    .Up(1);

                _obs.Metrics.Counter($"votes.status")
                    .Label("status", estadoVoto.ToString())
                    .LabelContext(vote as VoteDto)
                    .Up(1);

                return Ok($"Estado voto registrado correctamente:" + estadoVoto);

            }, (a, ex) =>
            {
                _obs.Metrics.Counter($"voting.ChangeStatus.error")
                    .Label(("id", voteId), ("status", estadoVoto))
                    .Label($"estadovoto", estadoVoto)
                    .Label("error", ex.Message)
                    .Up(1);
                return BadRequest("Error: " + ex.Message);

            });

        return await Task.FromResult(result);
    }


    /// <summary>
    /// Devuelve el promedio de votos para un producto.
    /// </summary>
    [HttpGet("{productId}")]
    public async Task<IActionResult> GetAverage(Guid productId)
    {
        var average = await _voteService.GetAverageVoteAsync(productId);
        if (average is null)
            return NotFound("No hay votos registrados para este producto.");

        return Ok(new { ProductId = productId, Average = Math.Round(average.Value, 2) });
    }

    [HttpGet()]
    public async Task<IActionResult> GetRatings()
    {
        return Ok(await _voteService.GetAll());
    }
}
