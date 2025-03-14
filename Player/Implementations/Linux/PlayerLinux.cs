using System.Diagnostics;
using MWP.Player.DataTypes;
using MWP.Player.Interfaces;
using Newtonsoft.Json;
#if DEBUG
using MWP.BackEnd.Helpers;
#endif

namespace MWP.Player.Implementations.Linux;

public class PlayerLinux : IPlayer
{
    private Process? mpvProcess;
    private bool isDisposed;
    private Guid mpvGuid = Guid.NewGuid();
    private string? pipeName;
    private IPC? ipc;
    private readonly CancellationTokenSource cts = new CancellationTokenSource();
    private readonly MessageQueue messageQueue = new MessageQueue();
    private readonly Synchronizations sync = new Synchronizations();

    public async Task<bool> Initialize()
    {
        messageQueue.MessageReceived += ProcessMessage;
        pipeName = $"/tmp/MWP_MPV_Socket_{mpvGuid}";
#if DEBUG
        Console.WriteLine($"mpv path: {pipeName}");
#endif
        ipc = new IPC(pipeName, cts, messageQueue);
        await CreatePlayer();
        return await ipc.ConnectAsync();
    }

    private void ProcessMessage(object? sender, EventArgs e)
    {
        while (messageQueue.Count > 0)
        {
            string msg = messageQueue.ReadMessage();
            try
            {
                MPVBaseResult? result = JsonConvert.DeserializeObject<MPVBaseResult>(msg);
                if (result == null) return;
                if (result.event_name == string.Empty)
                {
                    //TODO: trigger event
                }
                else
                {
                    sync.TriggerEvent(result.request_id);
                    messageQueue.AddRequest(result.request_id, result);
                }
            }
            catch (Exception exception)
            {
#if DEBUG
                MyConsole.WriteLine(exception);
                throw;
#endif
            }
        }
    }

    private async Task CreatePlayer()
    {
        mpvProcess = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
#if DEBUG
                Arguments = $"-c \"mpv --quiet --no-video --idle --input-ipc-server={pipeName} --log-file=/tmp/mpv.log \"",
#else
                Arguments = $"-c \"mpv --quiet --no-video --idle --input-ipc-server={pipeName} \"",
#endif
                
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        mpvProcess.Start();
        await Task.Delay(500);
    }
    
    public Task<bool> IsPlaying()
    {
        if (mpvProcess is not { HasExited: false }) return Task.FromResult(false);
        ipc?.WriteMessage($"{{\"command\": [\"get_property\", \"pause\"], \"request_id\": {(int)RequestCodes.PlayState}}}");
        sync.GetWaiter((int)RequestCodes.PlayState).WaitOne();
        MPVPlayingResult result = (MPVPlayingResult)messageQueue.ReadRequest((int)RequestCodes.PlayState);
        return Task.FromResult(result.data);
    }

    public async Task TogglePlay()
    {
        if (await IsPlaying())
        {
            await Pause();
        }
        else
        {
            await Play();
        }
    }

    public Task Play()
    {
        if (mpvProcess is not { HasExited: false }) return Task.FromException(new Exception("Process has exited or been disposed."));
        ipc?.WriteMessage("{\"command\": [\"set_property\", \"pause\", false]}");
        return Task.CompletedTask;
    }

    public Task Play(string filePath)
    {
        if (mpvProcess is not { HasExited: false }) return Task.FromException(new Exception("Process has exited or been disposed."));
        ipc?.WriteMessage($"{{\"command\": [\"loadfile\", \"{filePath}\", \"replace\"]}}");
        return Task.CompletedTask;
    }

    public Task Pause()
    {
        if (mpvProcess is not { HasExited: false }) return Task.FromException(new Exception("Process has exited or been disposed."));
        ipc?.WriteMessage("{\"command\": [\"set_property\", \"pause\", true]}");
        return Task.CompletedTask;
    }

    public Task Stop()
    {
        throw new NotImplementedException();
    }

    public Task Seek(long seconds)
    {
        if (mpvProcess is not { HasExited: false }) return Task.FromException(new Exception("Process has exited or been disposed."));
        ipc?.WriteMessage($"{{ \"command\": [ \"seek\", \"{seconds}\", \"absolute\" ] }}");
        return Task.CompletedTask;
    }

    public Task SetVolume(float volume)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetDuration()
    {
        if (mpvProcess is not { HasExited: false }) return Task.FromResult(0L);
        ipc?.WriteMessage($"{{\"command\": [\"get_property\", \"duration\"], \"request_id\": {(int)RequestCodes.Duration}}}");
        sync.GetWaiter((int)RequestCodes.Duration).WaitOne();
        MPVDurationResult result = (MPVDurationResult)messageQueue.ReadRequest((int)RequestCodes.Duration);
        return Task.FromResult(result.data);
    }

    public Task<long> GetPlayTime()
    {
        if (mpvProcess is not { HasExited: false }) return Task.FromResult(0L);
        ipc?.WriteMessage($"{{\"command\": [\"get_property\", \"time-pos\"], \"request_id\": {(int)RequestCodes.PlayTime}}}");
        sync.GetWaiter((int)RequestCodes.PlayTime).WaitOne();
        MPVDurationResult result = (MPVDurationResult)messageQueue.ReadRequest((int)RequestCodes.PlayTime);
        return Task.FromResult(result.data);
    }

    public void Dispose()
    {
        if (isDisposed) return;
        mpvProcess?.Dispose();
        mpvProcess = null;
        ipc?.Dispose();
        ipc = null;
        if (pipeName != null) File.Delete(pipeName);
        GC.SuppressFinalize(this);
        isDisposed = true;
    }
}