using System.Net.Sockets;
using System.Text;

namespace RealTimeChat.Server;

public class User
{
    private readonly TcpClient _client;

    private readonly NetworkStream _stream;

    public Channel Channel;

    public User(TcpClient client, string username, Channel channel)
    {
        _client = client;
        _stream = client.GetStream();
        Username = username;
        Channel = channel;
    }

    public string Username { get; }

    public async Task Send(string message)
    {
        await _stream.WriteAsync(Encoding.UTF8.GetBytes(message));
    }

    public void Dispose()
    {
        _stream.Dispose();
        _client.Dispose();
    }
}