using Producer.Exchanges;
using RabbitMQ.Client;

var factory = new ConnectionFactory()
{
    HostName = "localhost",
    VirtualHost = "smart-dev",
    UserName = "smart-proj",
    Password = "1234"
};

DirectExchange directExchange = new DirectExchange(factory);
FanoutExchange fanoutExchange = new FanoutExchange(factory);
HeaderExchange headerExchange = new HeaderExchange(factory);
TopicExchange topicExchange = new TopicExchange(factory);


Console.WriteLine("RabbitMQ Producer Started!");
Console.WriteLine("Select exchange type:");
Console.WriteLine("1 - Direct Exchange");
Console.WriteLine("2 - Fanout Exchange");
Console.WriteLine("3 - Topic Exchange");
Console.WriteLine("4 - Header Exchange");
Console.WriteLine("Type 'exit' to quit");

while (true)
{
    Console.Write("Choice (1-4)> ");
    string? choice = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(choice)) continue;
    if (choice.ToLower() == "exit") break;

    Console.Write("Message> ");
    string? message = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(message)) continue;

    switch (choice)
    {
        case "1":
            await directExchange.SendMessage(message);
            Console.WriteLine("✔ Sent via Direct Exchange");
            break;

        case "2":
            await fanoutExchange.SendMessage(message);
            Console.WriteLine("✔ Sent via Fanout Exchange");
            break;

        case "3":
            await topicExchange.SendMessage(message);
            Console.WriteLine("✔ Sent via Topic Exchange");
            break;

        case "4":
            await headerExchange.SendMessage(message);
            Console.WriteLine("✔ Sent via Header Exchange");
            break;

        default:
            Console.WriteLine("❌ Invalid choice. Please enter 1, 2, 3, or 4.");
            break;
    }
}
