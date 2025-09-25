using System.Text;
using RabbitMQ.Client;

namespace Producer.Exchanges
{
    public class DirectExchange
    {
        private readonly ConnectionFactory _factory;
        private readonly string _exchangeName = "order.direct";
        private readonly string _routingKey = "order.created";

        public DirectExchange(ConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task SendMessage(string message)
        {
            using var connection = await _factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Direct);

            channel.BasicReturnAsync += async (sender, ea) =>
            {
                string msg = Encoding.UTF8.GetString(ea.Body.ToArray());
                Console.WriteLine($" [!] Message returned! RoutingKey={ea.RoutingKey}, ReplyCode={ea.ReplyCode}, ReplyText={ea.ReplyText}, Body={msg}");
                await Task.CompletedTask;
            };

            var messageBody = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: _routingKey,
                body: messageBody,
                mandatory: true
            );

            Console.WriteLine($" [x] Sent {message}");
        }
    }
}