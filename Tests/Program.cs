using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using StompNet;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            TryWebSockets().GetAwaiter().GetResult();
        }

        static async Task ReadmeExample()
        {
            // Establish a TCP connection with the STOMP service.
            using (TcpClient tcpClient = new TcpClient())
            {
                await tcpClient.ConnectAsync("", 15674);

                //Create a connector.
                using (IStompConnector stompConnector =
                    new Stomp12Connector(
                        tcpClient.GetStream(),
                        "WebChannel",
                        "",
                        ""))
                {
                    // Create a connection.
                    IStompConnection connection = await stompConnector.ConnectAsync();

                    // Send a message.
                    await connection.SendAsync("/queue/example", "Anybody there!?");

                    // Send two messages using a transaction.
                    IStompTransaction transaction = await connection.BeginTransactionAsync();
                    await transaction.SendAsync("/queue/example", "Hi!");
                    await transaction.SendAsync("/queue/example", "My name is StompNet");
                    await transaction.CommitAsync();

                    await transaction.SubscribeAsync(
                        new ConsoleWriterObserver(),
                        "/queue/example");

                    // Wait for messages to be received.
                    await Task.Delay(250);

                    // Disconnect.
                    await connection.DisconnectAsync();
                }
            }
        }


        static async Task TryWebSockets()
        {
            // Establish a TCP connection with the STOMP service.
            using (ClientWebSocket webSocket = new ClientWebSocket())
            {
                await webSocket.ConnectAsync(new Uri("ws://52.137.87.113:15674/ws"), CancellationToken.None);


                //Create a connector.
                using (IStompConnector stompConnector =
                    new Stomp12Connector(
                        new WebSocketTransport(webSocket),
                        "WebChannel",
                        "",
                        ""))
                {
                    // Create a connection.
                    IStompConnection connection = await stompConnector.ConnectAsync();

                    // Send a message.
                    await connection.SendAsync("/queue/example4", "Anybody there!?");

                    await connection.SubscribeAsync(
                        new ConsoleWriterObserver(),
                        "/queue/example4");

                    // Wait for messages to be received.
                    await Task.Delay(250);

                    // Disconnect.
                    await connection.DisconnectAsync();
                }
            }
        }
    }

    class ConsoleWriterObserver : IObserver<IStompMessage>
    {
        // Incoming messages are processed here.
        public void OnNext(IStompMessage message)
        {
            Console.WriteLine("MESSAGE: " + message.GetContentAsString());

            if (message.IsAcknowledgeable)
                message.Acknowledge();
        }

        // Any ERROR frame or stream exception comes through here.
        public void OnError(Exception error)
        {
            Console.WriteLine("ERROR: " + error.Message);
        }

        // OnCompleted is invoked when unsubscribing.
        public void OnCompleted()
        {
            Console.WriteLine("UNSUBSCRIBED!");
        }
    }
}
