using System.Net.Sockets;
using System.Text;

namespace RealTimeChat.Server;

public class User
{
    public User(TcpClient client, string username, Channel channel)
    {
        _client = client;
        _stream = client.GetStream();
        Username = username;
    }

    public Channel Channel;

    private readonly TcpClient _client;

    private readonly NetworkStream _stream;
    public string Username { get; private set; }

    public async void Send(string message)
    {
        await _stream.WriteAsync(Encoding.UTF8.GetBytes(message));
    }
    
    public void Dispose()
    {
        _stream.Dispose();
        _client.Dispose();
    }
}