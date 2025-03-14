namespace MWP.Player.Implementations.Linux;

public class Synchronizations : IDisposable
{
    private bool isDisposed = false;
    private readonly Dictionary<int, AutoResetEvent> events = new Dictionary<int, AutoResetEvent>();
    
    public AutoResetEvent AddWaiter(int id, bool state = false)
    {
        AutoResetEvent ev = new AutoResetEvent(state);
        events.Add(id, ev);
        return ev;
    }

    public AutoResetEvent GetWaiter(int id, bool state = false)
    {
        return events.TryGetValue(id, out AutoResetEvent? waiter) ? waiter : AddWaiter(id, state);
    }

    /// <summary>
    /// Triggers event with particular <paramref name="id"/>
    /// </summary>
    /// <param name="id">id of event to be triggered</param>
    /// <returns>true if operation succeeds; otherwise, false</returns>
    public bool TriggerEvent(int id)
    {
        return events.TryGetValue(id, out AutoResetEvent? @event) && @event.Set();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (isDisposed) return;
        foreach (AutoResetEvent ev in events.Values)
        {
            ev.Dispose();
        }
        events.Clear();
        isDisposed = true;
    }
}