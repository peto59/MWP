using MWP.Player.DataTypes;

namespace MWP.Player.Implementations.Linux;

public class MessageQueue
{
    public event EventHandler MessageReceived;
    private Queue<string> messages = new Queue<string>();
    private Dictionary<int, Queue<MPVBaseResult>> requests = new Dictionary<int, Queue<MPVBaseResult>>();
    public int Count => messages.Count;
    
    public void AddMessage(string message)
    {
        messages.Enqueue(message);
        MessageReceived.Invoke(this, EventArgs.Empty);
    }

    public void AddRequest(int id, MPVBaseResult request)
    {
        if (!requests.ContainsKey(id))
        {
            requests[id] = new Queue<MPVBaseResult>();
        }
        requests[id].Enqueue(request);
    }

    public string ReadMessage()
    {
        try
        {
            return messages.Dequeue();
        }
        catch (InvalidOperationException e)
        {
            return string.Empty;
        }
    }

    public MPVBaseResult ReadRequest(int id)
    {
        if (requests.TryGetValue(id, out Queue<MPVBaseResult>? value))
        {
            try
            {
                return value.Dequeue();
            }
            catch (InvalidOperationException e)
            {
                //ignored
            }
        }
        return new MPVPlayingResult();
    }

    public void ClearRequestId(int id)
    {
        if (requests.TryGetValue(id, out Queue<MPVBaseResult>? value))
        {
            value.Clear();
        }
    }
    public void ClearRequests()
    {
        requests.Clear();
    }

    public void ClearMessages()
    {
        messages.Clear();
    }

    public void ClearAll()
    {
        ClearRequests();
        ClearMessages();
        
    }
}