using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Rating.BusinessLayer.HostedServices;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Unir.Framework.Observability;
using Unir.Framework.Observability.Abstractions;

public class ValidationObservabilityContext
{
    public Guid VotoId { get; set; }
    public string RabbitQueue { get; set; }
    public string RabbitRol { get; set; }
    public EstadoVotoDto ResultadoAprobacion { get; set; }
    public string? Comentario { get; set; }
    public string? Producto { get; set; }
    public string? Categoria { get; set; }
    public string Usuario { get; set; }
}

public class VoteApprovalService : BackgroundService
{
    private readonly ApplicationSettings _options;
    private readonly IObservabilityService _obs;
    private Microsoft.Extensions.Logging.ILogger<VoteApprovalService> _logger;
    private IConnection _connection;
    private IChannel _channel;

    public string RabbitQeueName => "product_rating";

    public VoteApprovalService(
        Microsoft.Extensions.Logging.ILogger<VoteApprovalService> logger, 
        IObservabilityService observabilityService,
        IOptions<ApplicationSettings> options)
    {
        _logger = logger;
        _options= options.Value;
        _obs = observabilityService;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("C. VoteApprovalService created");

        var rabbitServer = _options.RabbitServer ?? "localhost";
        var rabbitUser = _options.RabbitUser ?? "guest";
        var rabbitpwd = _options.RabbitPassword ?? "guest";

        var factory = new ConnectionFactory() { HostName = rabbitServer, UserName = rabbitUser, Password = rabbitpwd };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _channel.ExchangeDeclareAsync(exchange: RabbitQeueName, type: "direct").GetAwaiter().GetResult();

        var queue = _channel.QueueDeclareAsync().GetAwaiter().GetResult();
        var queueName = queue.QueueName;

        _channel.QueueBindAsync(queue: queueName, exchange: RabbitQeueName, routingKey: "").GetAwaiter().GetResult();

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += (model, ea) =>
        {            
            _logger.LogInformation("C.1 Rabbit ReceivedAsync");

            var context = new ValidationObservabilityContext();

            _obs.Traces
            .Configure("3.1 Validar votación", ActivityKind.Consumer)
            // [Demo V2] 2.3.3.4 Code.Traces: Correlación de actividades (Rabbit) CorrelateFrom
            .CorrelateFromRabbit(ea.BasicProperties.Headers)
            .Build()
            .Execute(
            activity =>
            {
                context.RabbitQueue = RabbitQeueName;
                context.RabbitRol = "consumer";

                RabbitRatingDto? vote;
                using (var step = activity.ChildActivity("3.2 Obtener mensaje rabbit", ActivityKind.Consumer))
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    _logger.LogInformation($"3.2 Convirtiendo voto");
                    vote = JsonSerializer.Deserialize<RabbitRatingDto>(json) ;

                    if (vote is null)
                    {
                        _logger.LogWarning("Voto no válido {TraceId}", activity.Activity?.TraceId);
                        throw new ArgumentException($"Voto no válido {activity.Activity?.TraceId}"); 
                    }
                    context.VotoId = vote.Id;
                }

                EstadoVotoDto estadoVoto;
                using (var step = activity.ChildActivity("3.3 Validar voto", ActivityKind.Internal))
                {
                    _logger.LogInformation("C.1 VoteApprovalService received rabbit message. ratingId:" + vote.Id);
                    estadoVoto =  ProcesoDeValidacionVoto().GetAwaiter().GetResult();

                    context.Comentario = vote.Comentario;
                    context.Producto  = vote.Categoria;
                    context.Categoria = vote.Categoria;
                    context.Usuario = vote.UserKey;
                    context.ResultadoAprobacion = estadoVoto;
                }

                using (var step = activity.ChildActivity("3.4 llamar al API", ActivityKind.Consumer))
                {
                    step.Label("api url", _options.ServiceUrl);
                    step.Labels["voto id"] = vote.Id;
                    step.Labels["estado"] = estadoVoto.ToString();
                    
                    _logger.LogInformation($"3.4 llamando al api {_options.ServiceUrl} para cambiar estado voto: {vote.Id}, estado: {estadoVoto}");
                    
                    var httpClient = Arky.Utils.Http.HttpClientHelper.Factory();
                    step.CorrelateTo(httpClient);

                    HttpResponseMessage response = null;
                    try
                    {
                        response = httpClient
                            .PostAsJsonAsync<object>($"{_options.ServiceUrl}/api/Voting/{vote.Id}/{estadoVoto}", null)
                            .GetAwaiter().GetResult();

                        if (!response.IsSuccessStatusCode)
                        {
                            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            _logger.LogError($"C.3.4 Error al enviar voto: {response.StatusCode} - {content}");                          
                        }
                        else
                        {
                            _obs.Metrics
                               .Counter($"votos.estado.{estadoVoto}")
                               .Label("estado", estadoVoto)
                               .Up(1);

                            _obs.Metrics
                                .Counter($"votos.estado")
                                .Label("producto", vote.Producto)
                                .Label("categoria", vote.Categoria)
                                .Label("usuario", vote.UserKey)
                                .Label("estado", estadoVoto)
                                .Up(1);

                            var upc = _obs.Metrics
                               .UpDownCounter("estados.voto.updown")
                               .Label("estado", estadoVoto);

                            if (estadoVoto == EstadoVotoDto.Approved)
                            {
                                upc.Record(1);
                            }
                            else
                            {
                                upc.Record(-1);
                            }

                            _logger.LogInformation($"3.4 Voto {vote.Id} actualizado a {estadoVoto}");
                        }
                    }
                    catch (Exception ex)
                    {
                        var content = response?.Content.ReadAsStringAsync();
                        throw;
                    }
                }

                activity.LabelContext(context, "validation");
            },
            (activity, e) =>
            {
                activity.LabelContext(context, "validation");

                _logger.LogError(e, "C. Error al cambiar estado voto");
                _obs.Metrics
                    .Counter("validation.errors")
                    .WithDescription("Errores producidos en la vaidación")
                    .LabelContext(context)
                    .WithUnit("error")
                    .Up(1);
            });

            return Task.CompletedTask;
        };

        _channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer).GetAwaiter().GetResult();
        Console.ReadLine();

        return Task.CompletedTask;
    }

    private static async Task<EstadoVotoDto> ProcesoDeValidacionVoto()
    {
        var estadoVoto = Random.Shared.Next(2) == 0 ? EstadoVotoDto.Approved : EstadoVotoDto.Rejected;

        //Simulamos un proceso de validacion
        await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(100, 200)));

        return estadoVoto;
    }

    public override void Dispose()
    {
        _connection?.Dispose();
        base.Dispose();
    }
}