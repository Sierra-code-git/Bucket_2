using System.Text;
using RabbitMQ.Client;

namespace Producer.Exchanges
{
    public class TopicExchange
    {

        private readonly ConnectionFactory _factory;
        private readonly string _exchangeName = "order.topic";
        private readonly string _routingKey = "order.error.logs";

        public TopicExchange(ConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task SendMessage(string message)
        {
            await using var connection = await _factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Topic);

            var messageBody = Encoding.UTF8.GetBytes(message);

            channel.BasicReturnAsync += async (sender, ea) =>
            {
                string msg = Encoding.UTF8.GetString(ea.Body.ToArray());
                Console.WriteLine($" [!] Message returned! RoutingKey={ea.RoutingKey}, ReplyCode={ea.ReplyCode}, ReplyText={ea.ReplyText}, Body={msg}");
                await Task.CompletedTask;
            };

            // Routing key is ignored in fanout
            await channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: _routingKey,
                body: messageBody,
                mandatory: true
            );

            Console.WriteLine($"[Topic Exchange] Sent: {message}");
        }
    }
}