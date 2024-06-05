namespace RealTimeChat.Server;

public class Channel
{
    public Channel(string name)
    {
        Users = new List<User>();
        Name = name;
    }

    public List<User> Users { get; }

    public string Name { get; }

    public void AddUser(User user)
    {
        Users.Add(user);
    }

    public void SendToAll(string message)
    {
        var taskList = new List<Task>();
        foreach (var user in Users) taskList.Add(user.Send(message));
        Task.WhenAll(taskList);
    }
}