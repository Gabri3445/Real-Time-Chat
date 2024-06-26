﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace RealTimeChat.Server;

/*
 * Words that start with $ can be any UTF-8 alphanumeric value
 * Example communication between the server and client
 * 1. Client sends "/login username" to the server
 * 1.1 If the username is already used refuse the connection ("/error" is sent to the client)
 * 2. Client gets assigned to default channel
 * 3. Client sends messages which are set to other people on the channel
 * 4 Every message NOT starting with "/" is interpreted as messages
 * 4.1 "/quit" = disconnects the client
 * 4.2 "/channel $channelName" switches the channel
 * 4.3 (To implement) "/whisper $username $message" sends a message privately to the user
 * 4.4 (To implement) "/username $username" changes username
 */

internal static class Program
{
    private static readonly List<Channel> Channels = new() { new Channel("default") };
    private static readonly List<User> Users = new();

    public static async Task Main(string[] args)
    {
        var ip = new IPEndPoint(IPAddress.Any, 2100);
        TcpListener listener = new(ip);
        listener.Start();
        while (true)
        {
            var handler = await listener.AcceptTcpClientAsync();

            async void Start()
            {
                await HandleConn(handler);
            }

            new Thread(Start).Start();
        }
    }

    private static async Task HandleConn(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();
            const string loginPattern = @"/login [a-zA-Z0-9]{1,20}\b";
            const string channelPattern = @"/channel [a-zA-Z0-9]{1,20}\b";
            var login = await Read(stream);
            if (login is null && !Regex.IsMatch(login!, loginPattern)) return;
            var username = login!.Split(" ")[1];
            if (UserExists(username) != null)
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes("/error"));
                return;
            }

            var user = new User(client, username, Channels[0]);
            Channels[0].AddUser(user);
            Console.WriteLine($"{username} Connected");
            while (true)
            {
                var message = await Read(stream);
                if (message is null) continue;

                if (message[0] == '/')
                {
                    if (message == "/quit")
                    {
                        user.Channel.SendToAll($"User {username} has disconnected");
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("/quit"));
                        user.Dispose();
                        return;
                    }

                    if (Regex.IsMatch(message, channelPattern))
                    {
                        user.Channel.SendToAll($"User {username} has left the {user.Channel.Name} channel");
                        user.Channel.Users.Remove(user);
                        var channelName = message.Split(" ")[1];
                        var channelExists = ChannelExists(channelName);
                        if (channelExists == null)
                        {
                            var channel = new Channel(channelName);
                            Channels.Add(channel);
                            channel.AddUser(user);
                            user.Channel = channel;
                            user.Channel.SendToAll($"User {username} has joined the {user.Channel.Name} channel");
                            continue;
                        }
                        else
                        {
                            user.Channel = channelExists;
                            user.Channel.Users.Add(user);
                            user.Channel.SendToAll($"User {username} has joined the {user.Channel.Name} channel");
                            continue;
                        }
                    }
                    else
                    {
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("Invalid command"));
                        continue;
                    }
                }

                var date = DateTime.Now;
                user.Channel.SendToAll($"{user.Channel.Name} : [{date.Hour}:{date.Minute:D2}] - {username}: {message}");
            }
        }
        catch (Exception e)
        {
            // ignored
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

    private static User? UserExists(string username)
    {
        return Users.FirstOrDefault(user => user.Username == username);
    }
}