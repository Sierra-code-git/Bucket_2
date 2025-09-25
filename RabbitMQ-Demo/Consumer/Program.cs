using System.Text;
using Consumer.Exchanges;
using RabbitMQ.Client;

var factory = new ConnectionFactory()
{
    HostName = "localhost",
    VirtualHost = "smart-dev",
    UserName = "smart-proj",
    Password = "1234"
};

using var connection = await factory.CreateConnectionAsync();

DirectExchange directExchange = new DirectExchange(connection: connection);
TopicExchange topicExchange = new TopicExchange(connection);
HeaderExchange headerExchange = new HeaderExchange(connection);
FanoutExchange fanoutExchange = new FanoutExchange(connection);

await directExchange.ConsumeMessage();
await topicExchange.ConsumeMessage();
await headerExchange.ConsumeMessage();
await fanoutExchange.ConsumeMessage();

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();