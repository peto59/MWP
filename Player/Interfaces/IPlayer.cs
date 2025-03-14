namespace MWP.Player.Interfaces;

public interface IPlayer : IDisposable
{
    public Task<bool> IsPlaying();
    public Task TogglePlay();
    public Task Play();
    public Task Play(string filePath);
    public Task Pause();
    public Task Stop();
    public Task Seek(long seconds);
    public Task SetVolume(float volume);
    public Task<long> GetDuration();
    public Task<long> GetPlayTime();
    public Task<bool> Initialize();
}