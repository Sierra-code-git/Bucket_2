using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Consumer.Exchanges
{
    public class FanoutExchange
    {
        private readonly IConnection _connection;
        private IChannel? _channel; // keep channel alive

        private readonly string _exchangeName = "order.fanout";
        private readonly string _queueName = "q.order.update";

        public FanoutExchange(IConnection connection)
        {
            _connection = connection;
        }

        public async Task ConsumeMessage()
        {
            _channel = await _connection.CreateChannelAsync();

            // Declare the fanout exchange and queue
            await _channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Fanout);
            await _channel.QueueDeclareAsync(queue: _queueName, exclusive: false, autoDelete: false);

            // Bind queue to exchange
            await _channel.QueueBindAsync(queue: _queueName, exchange: _exchangeName, routingKey: string.Empty);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"[Fanout Exchange {_queueName}] Received: {message}");
                return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(_queueName, autoAck: true, consumer: consumer);
        }
    }
}