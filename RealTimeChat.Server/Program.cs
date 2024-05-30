using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace RealTimeChat.Server;

internal static class Program
{

    private static readonly List<Channel> Channels = new List<Channel>(){new Channel("default")};

    public static async Task Main(string[] args)
    {
        var ip = new IPEndPoint(IPAddress.Any, 2100);
        TcpListener listener = new(ip);
        listener.Start();
        while (true)
        {
            var handler = await listener.AcceptTcpClientAsync();
            new Thread(async () => HandleConn(handler)).Start();
        }
    }

    private static async void HandleConn(TcpClient client)
    {
        var stream = client.GetStream();
        const string loginPattern = "/login [a-zA-Z0-9]{1,20}\b";
        var login = await Read(stream);
        if (login is null && !Regex.IsMatch(login!, loginPattern))
        {
            return;
        }
        var username = login!.Split(" ")[1];
        var user = new User(client, username, Channels[0]);
        Channels[0].AddUser(user);
        Console.WriteLine($"{username} Connected");
    }

    private static async Task<string?> Read(NetworkStream stream)
    {
        var buffer = new byte[1_024];
        var received = await stream.ReadAsync(buffer);
        return Encoding.UTF8.GetString(buffer, 0, received);
    }
}