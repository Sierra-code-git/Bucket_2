using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Consumer.Exchanges
{
    public class DirectExchange
    {
        private readonly string _exchangeName = "order.direct";
        private readonly string queueName = "q.create_order";
        private readonly string routingKey = "order.created";

        private readonly IConnection _connection;
        private IChannel? _channel; // keep channel alive


        public DirectExchange(IConnection connection)
        {
            _connection = connection;
        }
        public async Task ConsumeMessage()
        {
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Direct);
            await _channel.QueueDeclareAsync(queue: queueName, exclusive: false, autoDelete: false);

            // Bind queue to exchange with routing key
            await _channel.QueueBindAsync(queue: queueName, exchange: _exchangeName, routingKey: routingKey);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"[Direct Exchange {queueName}] Received: {message}");
                return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);
        }
    }
}