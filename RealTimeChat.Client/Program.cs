using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RealTimeChat.Client;

internal static class Program
{
    private const string Prompt = "Message: ";
    private static readonly object Lock = new();
    private static readonly List<string> Messages = new();
    private static int _maxMessages = Console.BufferHeight - 3;

    //Probably best just to make a WPF or blazor app
    public static async Task Main(string[] args)
    {
        var ip = new IPEndPoint(IPAddress.Loopback, 2100);
        var client = new TcpClient();
        await client.ConnectAsync(ip);
        Console.WriteLine("Enter your username (20 characters)");
        var username = Console.ReadLine() ?? throw new InvalidOperationException();
        Console.Clear();
        await using var stream = client.GetStream();
        stream.Write(Encoding.UTF8.GetBytes($"/login {username}"));
        new Thread(async () => ReadMessages(client)).Start();
        while (true)
        {
            var userInput = ReadFromBottom(Prompt);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(userInput));
        }
    }

    private static async Task ReadMessages(TcpClient client)
    {
        var stream = client.GetStream();
        while (true)
        {
            var message = await Read(stream);
            lock (Lock)
            {
                if (message == "/quit") Environment.Exit(0);
                _maxMessages = Console.BufferHeight - 3;
                if (Messages.Count >= _maxMessages) Messages.RemoveAt(0);
                Messages.Add(message);
                DisplayMessages();
            }
        }
    }

    private static string ReadFromBottom(string prompt)
    {
        var modRow = Console.BufferHeight - 2;
        lock (Lock)
        {
            Console.SetCursorPosition(0, modRow);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, modRow);
            Console.Write(prompt);
        }

        var returnValue = Console.ReadLine() ?? throw new InvalidOperationException();

        lock (Lock)
        {
            Console.SetCursorPosition(0, modRow);
            Console.Write(new string(' ', Console.WindowWidth));
            DisplayMessages();
        }

        return returnValue;
    }

    private static void DisplayMessages()
    {
        Console.Clear();

        for (var i = 0; i < Messages.Count; i++)
        {
            Console.SetCursorPosition(0, i);
            Console.WriteLine(Messages[i]);
        }

        // Move the cursor back to the input prompt line
        var modRow = Console.BufferHeight - 2;
        Console.SetCursorPosition(0, modRow);
        Console.Write(Prompt);
    }

    private static async Task<string?> Read(NetworkStream stream)
    {
        var buffer = new byte[1_024];
        var received = await stream.ReadAsync(buffer);
        return Encoding.UTF8.GetString(buffer, 0, received);
    }
}

/*
 * // Reading the response
            string response;
            while ((response = reader.ReadLine()) != null)
            {
                Console.WriteLine(response);
            }
*/