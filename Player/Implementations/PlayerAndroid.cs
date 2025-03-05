using MWP.Player.Interfaces;

namespace MWP.Player.Implementations;

public class PlayerAndroid : IPlayer
{
    public Task<bool> IsPlaying()
    {
        throw new NotImplementedException();
    }

    public Task TogglePlay()
    {
        throw new NotImplementedException();
    }

    public Task Play()
    {
        throw new NotImplementedException();
    }

    public Task Play(string fileName)
    {
        throw new NotImplementedException();
    }

    public Task Pause()
    {
        throw new NotImplementedException();
    }
    
    public Task Stop()
    {
        throw new NotImplementedException();
    }

    public Task Seek(long seconds)
    {
        throw new NotImplementedException();
    }

    public Task SetVolume(float volume)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetDuration()
    {
        throw new NotImplementedException();
    }

    public Task<long> GetPlayTime()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}