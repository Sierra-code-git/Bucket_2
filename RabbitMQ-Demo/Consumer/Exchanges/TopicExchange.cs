using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Consumer.Exchanges
{
    public class TopicExchange
    {

        private readonly string _exchangeName = "order.topic";

        private readonly string queueName = "q.logs.all";
        private readonly string routingKey = "logs.#";

        private readonly string queueName1 = "q.logs.error";
        private readonly string routingKey1 = "logs.error.*";

        private readonly string queueName2 = "q.logs";
        private readonly string routingKey2 = "logs.*.logs";

        private readonly IConnection _connection;
        private IChannel? _channel; // keep channel alive

        public TopicExchange(IConnection connection)
        {
            _connection = connection;
        }

        public async Task ConsumeMessage()
        {
            _channel = await _connection.CreateChannelAsync();

            // Declare the topic exchange and queue
            await _channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Topic);
            await _channel.QueueDeclareAsync(queue: queueName, exclusive: false, autoDelete: false);

            // Bind queue to exchange with routing key
            await _channel.QueueBindAsync(queue: queueName, exchange: _exchangeName, routingKey: routingKey);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"[Topic Exchange {queueName}] Received: {message}");
                return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);
        }
    }
}