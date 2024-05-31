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
        const string loginPattern = @"/login [a-zA-Z0-9]{1,20}\b";
        const string channelPattern = @"/channel [a-zA-Z0-9]{1,20}\b";
        var login = await Read(stream);
        if (login is null && !Regex.IsMatch(login!, loginPattern))
        {
            return;
        }
        var username = login!.Split(" ")[1];
        var user = new User(client, username, Channels[0]);
        Channels[0].AddUser(user);
        Console.WriteLine($"{username} Connected");
        while (true)
        {
            var message = await Read(stream);
            if (message is null)
            {
                continue;
            }

            if (message[0] == '/')
            {
                if (message == "/quit")
                {
                    user.Channel.SendToAll($"User {username} has disconnected");
                    stream.Write(Encoding.UTF8.GetBytes("/quit"));
                    user.Dispose();
                    return;
                }
                if (Regex.IsMatch(message, channelPattern))
                {
                    user.Channel.SendToAll($"User {username} has left the {user.Channel.Name} channel");
                    user.Channel.Users.Remove(user);
                    var channelName = message.Split(" ")[1];
                    var channelExists = ChannelExists(channelName);
                    if(channelExists == null)
                    {
                        var channel = new Channel(channelName);
                        Channels.Add(channel);
                        channel.AddUser(user);
                        user.Channel = channel;
                        user.Channel.SendToAll($"User {username} has joined the {user.Channel.Name} channel");
                    }
                    else
                    {
                        user.Channel = channelExists;
                        user.Channel.Users.Add(user);
                        user.Channel.SendToAll($"User {username} has joined the {user.Channel.Name} channel");
                    }
                }
                else
                {
                    stream.Write(Encoding.UTF8.GetBytes("Invalid command"));
                    continue;
                }
            }

            var date = DateTime.Now;
            user.Channel.SendToAll($"[{date.Hour}:{date.Minute}] - {username}: {message}");
            //Handle commands
            /*
             * \/quit
             * \/channel name; switches channel
             */
        }
    }

    private static async Task<string?> Read(NetworkStream stream)
    {
        var buffer = new byte[1_024];
        var received = await stream.ReadAsync(buffer);
        return Encoding.UTF8.GetString(buffer, 0, received);
    }

    private static Channel? ChannelExists(string name)
    {
        return Channels.FirstOrDefault(channel => channel.Name == name);
    }
}