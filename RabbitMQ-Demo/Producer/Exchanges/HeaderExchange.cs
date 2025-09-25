using System.Text;
using RabbitMQ.Client;

namespace Producer.Exchanges
{
    public class HeaderExchange
    {
        private readonly IConnectionFactory _factory;
        private readonly string _exchangeName = "order.header";

        public HeaderExchange(IConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task SendMessage(string message)
        {
            await using var connection = await _factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Headers);

            channel.BasicReturnAsync += async (sender, ea) =>
            {
                string msg = Encoding.UTF8.GetString(ea.Body.ToArray());
                Console.WriteLine($" [!] Message returned! ReplyCode={ea.ReplyCode}, ReplyText={ea.ReplyText}, Body={msg}");
                await Task.CompletedTask;
            };

            var properties = new BasicProperties();
            properties.Headers = new Dictionary<string, object?>
            {
                { "format", "pdf"},
                { "type","invoice" }
            };

            //   {
            //             { "format", "xls" },
            //             { "type", "report" }
            //         };
            var messageBody = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: string.Empty,
                mandatory: true,
                body: messageBody,
                basicProperties: properties
            );
            Console.WriteLine($"[Header Exchange] Sent: {message} with headers: {string.Join(", ", properties.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
        }
    }
}