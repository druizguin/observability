namespace Rating.BusinessLayer.HostedServices;

using Arky.Utils.Http;
using Bogus;
using Bogus.DataSets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rating.BusinessLayer.Dom;
using Rating.Cli;
using Rating.Cli.Dtos;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Http.Json;
using Unir.Framework.Observability.Abstractions;

public class ClaseHija
{
    public string? Propiedad1 { get; set; }
    public string? Propiedad2 { get; set; }
    public SubclaseHija Subclase { get; set; } = new();

    public class SubclaseHija
    {
        public TimeSpan SubPropiedad1 { get; set; }
    }
}

// [Demo V2] 2.3.1.1 Code.Traces: Etiquetas basadas en contexto complejo
public class ContextoGlobal
{
    public DateTime Fecha { get; } = DateTime.Now;
    public string? Entorno { get; set; }

    public string? AppCard { get; set; }

    public ClaseHija ClaseHija { get; set; } = new();
}

internal class RatingGenerator : BackgroundService
{
    bool Busy = false;
    private readonly ApplicationSettings _options;
    private Microsoft.Extensions.Logging.ILogger<RatingGenerator> _logger;
    private readonly IObservabilityService _observabilityService;
    private readonly string[] tenants = new[] { "Unir", "UCM", "UPM", "UNA", "UCO", "UAM", "UPSAM" };
    private readonly string[] emails = new[] { "pepe@gmail.com", "disident@fakemail.com", "rringrose0@xrea.com",
        "fbuckeridge1@squidoo.com", "pcudd2@ustream.tv", "kmegahey3@over-blog.com", "ucarus4@qq.com","odemoge6@de.vu",
        "zcrew7@loc.gov", "omurrish8@163.com", "dsawle9@icio.us", "bcrucittia@state.gov", "cbartenb@timesonline.co.uk",
        "mzorroh@bloglines.com", "klowriei@accuweather.com", "gdurandj@theatlantic.com", "astanierk@studiopress.com",
        "ngaitl@shop-pro.jp", "afyfem@google.com.hk", "etreffryn@bloglines.com", "elenscho@merriam-webster.com",
        "gmothersolep@wsj.com", "leyerq@netscape.com", "inaldrettr@youku.com", "gorbells@thetimes.co.uk"  };

    Faker<VoteDto> voteFaker = null;
    int min, max;

 

    public RatingGenerator(
        IObservabilityService observabilityService, // [Demo V2] 2.2. Code: Inyección ObservabilityService 
        ILogger<RatingGenerator> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<ApplicationSettings> options
        )
    {
        _options = options.Value;
        _options.ServiceUrl = "http://rating.webapi:5398";
        min = (int)(_options.MinInterval ?? TimeSpan.FromSeconds(15)).TotalSeconds;
        max = (int)(_options.MaxInterval ?? TimeSpan.FromSeconds(30)).TotalSeconds;
        _logger = logger;
        _logger.LogInformation("1.0 RatingGeneratorHostedService created");
        _observabilityService = observabilityService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var contexto = new ContextoGlobal
        {
            Entorno = Environment.GetEnvironmentVariable("ENTORNO") ?? "DEV",
            AppCard = _observabilityService.ApplicationCard.Key
        };

        contexto.ClaseHija.Propiedad1 = "Valor 1";
        contexto.ClaseHija.Propiedad2 = "Valor 2";
        contexto.ClaseHija.Subclase.SubPropiedad1 = TimeSpan.MaxValue;

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        _logger.LogInformation("RatingGeneratorHostedService started");

        int i = 0;
        voteFaker = new Faker<VoteDto>()
                .RuleFor(v => v.Id, f => Guid.NewGuid())
                .RuleFor(v => v.UserKey, f => f.PickRandom(emails))
                .RuleFor(v => v.Valor, f => f.Random.Int(3, 5))
                .RuleFor(v => v.Country, f => f.Address.CountryCode(Iso3166Format.Alpha3))
                .RuleFor(v => v.Comentario, f => f.Lorem.Sentence())
                .RuleFor(v => v.Fecha, f => DateTime.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            i++;
            _logger.LogInformation("1 RatingGeneratorHostedService running {Count}", i);

            try
            {
                await EnviarVotacion(contexto, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "1 RatingGeneratorHostedService Error sending vote");
            }
            var interval = TimeSpan.FromSeconds(Random.Shared.Next(min, max));

            _logger.LogInformation("1 RatingGeneratorHostedService Wait interval {interval}", interval);
            await Task.Delay(interval, stoppingToken);
        }
    }

    public async Task EnviarVotacion(ContextoGlobal contexto, CancellationToken stoppingToken)
    {
        var contextoVoto = new ClientObservabilityContext();

        // [Demo V2] 2.3. Code: Uso de trazas

        await _observabilityService.Traces
            .Activity("1 RatingGenerator ExecuteAsync", ActivityKind.Client)
            .ExecuteAsync(async activity =>
            {
                {
                    // [Demo V2] 2.3.1 Code.Traces: Inclusión de etiquetas de datos
                    // 4 formas diferentes de etiquetar trazas: 
                    activity.Labels["etiqueta 1"] = "valor 1"; // Modo diccionario
                    activity.Label("etiqueta 2", "valor 2");   // Método unitario
                    activity.Label(
                        ("etiqueta 3", 3), ("etiqueta 4", 4)   // Array de valores
                        );

                    activity.LabelContext(contexto, prefix: "ejemplo"); //Usando un contexto de datos complejo

                    // Serializando un contexto
                    // El resultado de las etiquetas será:
                    //      etiqueta 1="valor 1"
                    //      etiqueta 2="valor 2"
                    //      etiqueta 3=3
                    //      etiqueta 4=3
                    //      fecha   = <fecha>
                    //      entorno = <entorno>
                    //      Appcard = <appcard>
                    //      clasehija.propiedad1 = <valor>
                    //      clasehija.propiedad2 = <valor>
                    //      clasehija.subclase.propiedad1 = <valor>


                    if (Busy || Random.Shared.Next(10) == 0)
                    {
                        activity
                        .ChildActivity(
                             "1.A Proceso ocupado",
                             ActivityKind.Internal) // <- Tipo de actividad Client, Server, Internal, Producer, Consumer, ...
                        .Execute(activity => _logger.LogWarning("1.A El proceso está aun ocupado"));
                        return;
                    }

                    Busy = true;

                    ProductDTO[]? productos = null;
                    ProductDTO? product = null;
                    var httpClient = HttpClientHelper.Factory();

                    // [Demo V2] 2.3.2 Code.Traces: Creación de trazas hijas
                    await activity
                    .ChildActivity("1.1 Obtener productos", ActivityKind.Internal)
                    .ExecuteAsync(async step1 =>
                    {
                        _logger.LogInformation($"1.1 Obtener productos desde {_options.ServiceUrl}/api/Products");

                        // [Demo V2] 2.3.3.1 Code.Traces: Correlación de actividades (HttpClient) CorrelateTo
                        step1.CorrelateTo(httpClient);

                        productos = await httpClient
                            .GetFromJsonAsync<ProductDTO[]>($"{_options.ServiceUrl}/api/Products", stoppingToken);

                        if (productos is null || !productos.Any())
                            throw new Exception("1.1 No se han obtenido productos para generar votos");

                        step1.Label(("step1.productos.count", productos.Length)); //Extendido
                        step1.Label("step1.productos.time", activity.Activity?.Duration); // Nativo
                    });

                    activity
                            .ChildActivity("1.2 Elegir un producto aleatorio")
                            .Execute(step2 =>
                            {
                                var pindex = Random.Shared.Next(productos.Length);
                                product = productos[pindex];

                                step2.Label("step2.productos.count", productos.Length);
                                step2.Label("step2.producto.seleccionado", product.Name);

                                contextoVoto.Producto = product.Name; // <- Rellenamos en nombre del producto
                                contextoVoto.Categoria = product.Category;

                                _logger.LogInformation($"1.2 Producto elegido: {product.Name} {product.Id}");
                            });

                    VoteDto? vote = null;

                    activity
                        .ChildActivity("1.3 Generar voto Mock", ActivityKind.Internal)
                        .Execute(step3 =>
                        {
                            vote = voteFaker.Generate();
                            vote.ProductoId = product.Id;

                            if (Random.Shared.Next(0, 10) ==0)
                            {
                                //Hater
                                vote.Valor = Random.Shared.Next(0, 2);
                                _logger.LogInformation($"1.3 Voto Hater: {vote.Valor}");
                                step3.Label("step3.usertendence", "hater");
                            }

                            step3.Label(
                                    ("step3.producto", product.Name),
                                    ("step3.Valor", vote.Valor),
                                    ("step3.user", vote.UserKey),
                                    ("step3.date", vote.Fecha),
                                    ("step3.country", vote.Country)

                                    );

                            contextoVoto.User = vote.UserKey;
                            contextoVoto.Rating = vote.Valor;

                            _logger.LogInformation($"1.3 Voto generado para: {product.Name}. Valoración: {vote.Valor}, {vote.Comentario}");

                        });

                    await activity
                        .ChildActivity("1.4 Enviar voto al API", ActivityKind.Client)
                        .ExecuteAsync(async step4 =>
                        {
                            _logger.LogInformation($"1.4 envia voto {_options.ServiceUrl}/api/Voting: Id: {vote.Id}");

                            var tenant = tenants[Random.Shared.Next(tenants.Length)];
                            httpClient.DefaultRequestHeaders.Add("X-tenant", tenant);

                            contextoVoto.Tenant = tenant;

                            var responsevote = await httpClient.PutAsJsonAsync($"{_options.ServiceUrl}/api/Voting", vote);

                            step4.Label(
                                    ("step4.tenant", tenant),
                                    ("step4.time", step4.Activity.Duration),
                                    ("step4.responseCode", responsevote.StatusCode),
                                    ("step4.responseSuccess", responsevote.IsSuccessStatusCode));

                            if (responsevote.IsSuccessStatusCode)
                            {
                                _logger.LogInformation("1.4 Voto generado correctamente " +
                                        $"para el producto {product.Name}, id: {vote.Id}");
                            }
                            else
                            {
                                var content = responsevote.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                                _logger.LogError($"1.4 Error al enviar voto: {responsevote.StatusCode} - {content}");
                            }
                        });

                    Busy = false;

                    activity.LabelContext(contextoVoto, prefix: "contextovoto");
                }
            }, async (activity, exception) =>
            {
                Busy = false;
                activity.Label("resultado", "error");
                _logger.LogError($"1.4 Error al generar  voto: " + exception.Message);
                await Task.CompletedTask;
            });

        Busy = false;
    }
}
