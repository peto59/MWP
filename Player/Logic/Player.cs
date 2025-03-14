using MWP.Player;
using MWP.Player.Implementations.Android;
using MWP.Player.Implementations.Linux;
using MWP.Player.Implementations.Windows;
using MWP.Player.Interfaces;

namespace MWP.Player;

public class Player :  IPlayer
{
    private readonly IPlayer player;
    private bool isDisposed;

    public Player()
    {
        if (OperatingSystem.IsWindows())
        {
            player = new PlayerWindows();
        }
        else if (OperatingSystem.IsLinux())
        {
            player = new PlayerLinux();
        }
        else if (OperatingSystem.IsAndroid())
        {
            player = new PlayerAndroid();
        }
        else
        {
            throw new PlatformNotSupportedException("Player not supported");
        }
    }
    
    public Task<bool> Initialize()
    {
        return player.Initialize();
    }
    
    public Task<bool> IsPlaying()
    {
        return player.IsPlaying();
    }

    public Task TogglePlay()
    {
        return player.TogglePlay();
    }

    public Task Play()
    {
        return player.Play();
    }

    public Task Play(string filePath)
    {
        return player.Play(filePath);
    }

    public Task Pause()
    {
        return player.Pause();
    }

    public Task Stop()
    {
        return player.Stop();
    }

    public Task Seek(long seconds)
    {
        return player.Seek(seconds);
    }

    public Task SetVolume(float volume)
    {
        return player.SetVolume(volume);
    }

    public Task<long> GetDuration()
    {
       return player.GetDuration();
    }

    public Task<long> GetPlayTime()
    {
        return player.GetPlayTime();
    }

    public void Dispose()
    {
        if (isDisposed) return;
        player.Dispose();
        GC.SuppressFinalize(this);
        isDisposed = true;
    }
}