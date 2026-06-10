using RabbitMQ.Client;
using Rating.BusinessLayer.Dom;
using Rating.BusinessLayer.HostedServices;
using System.Text;
using System.Text.Json;
using Unir.Framework.Observability.Abstractions;

namespace Rating.BusinessLayer.Services;

public class VotePublisher 
{
    private readonly IConfiguration config;
    private readonly IObservabilityService _obs;

    public string RabbitQeueName => "product_rating";

    public VotePublisher(IConfiguration config, IObservabilityService observabilityService)
    {
        this.config = config;
        _obs = observabilityService;
    }

    public async Task PublishVote(Dom.VoteDto vote, Product product, IActivityProcess step5)
    {
        var rabbitServer = config.GetValue<string>("settings:rabbitServer") ?? "localhost";
        var rabbitUser = config.GetValue<string>("settings:rabbitUser") ?? "guest";
        var rabbitpwd = config.GetValue<string>("settings:rabbitPassword") ?? "guest";

        var factory = new ConnectionFactory() { HostName = rabbitServer, UserName = rabbitUser, Password = rabbitpwd };
        using (var _connection = await factory.CreateConnectionAsync())
        {
            using (var _channel = await _connection.CreateChannelAsync())
            {
                _channel.QueueDeclareAsync(queue: RabbitQeueName, durable: true,
                    exclusive: false,
                    autoDelete: false).GetAwaiter().GetResult();

                var rabbitvote = new RabbitRatingDto
                {
                    Categoria = product.Category,
                    Producto = product.Name,
                    Comentario = vote.Comentario,
                    Valor = vote.Valor,
                    Fecha = vote.Fecha,
                    UserKey = vote.UserKey,
                    Id = vote.Id,
                };

                var json = JsonSerializer.Serialize(rabbitvote);
                var body = Encoding.UTF8.GetBytes(json);

                try
                {
                    await _channel.BasicPublishAsync(
                        exchange: RabbitQeueName,
                        routingKey: "",
                        body: body,
                        mandatory: true,
                        // [Demo V2] 2.3.3.3 Code.Traces: Correlación de actividades (Rabbit) CorrelateTo
                        basicProperties: RabbitTracesPropagator.CorrelateTo(step5.Activity)); 
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Rabbit Error: " + ex.ToString());
                }
            }
        }
    }
}
