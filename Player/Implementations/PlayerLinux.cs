using System.Diagnostics;
using MWP.Player.DataTypes;
using MWP.Player.Interfaces;
using Newtonsoft.Json;

namespace MWP.Player.Implementations;

public class PlayerLinux : IPlayer
{
    private Process? mpvProcess;
    private Process? socatProcess;
    private bool isDisposed;
    public PlayerLinux()
    {
        CreatePlayer();
    }

    private void CreatePlayer()
    {
        mpvProcess = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \"mpv --quiet --idle --input-ipc-server=/tmp/MWP_MPV_Socket1 \"",
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        mpvProcess.Start();
        Thread.Sleep(500);
        
        socatProcess = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \"socat - /tmp/MWP_MPV_Socket1 \"",
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        socatProcess.Start();
    }
    
    public Task<bool> IsPlaying()
    {
        
        if (mpvProcess is { HasExited: false } && socatProcess is { HasExited: false })
        {
            socatProcess.StandardOutput.DiscardBufferedData();
            socatProcess.StandardInput.WriteLine("{\"command\": [\"get_property\", \"pause\"], \"request_id\": 100}");
            Thread.Sleep(5);
            string? mpvOutput = String.Empty;
            while (!mpvOutput?.Contains("\"request_id\":100") ?? false)
            {
                mpvOutput = socatProcess.StandardOutput.ReadLine();
            }
            if (mpvOutput != null)
            {
                MPVPlayingResult? mpvResult = JsonConvert.DeserializeObject<MPVPlayingResult>(mpvOutput);
                return Task.FromResult(!mpvResult?.data ?? false);
            }
        }
        return Task.FromResult(false);
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
        if (mpvProcess != null && socatProcess != null && !mpvProcess.HasExited && !socatProcess.HasExited)
        {
            socatProcess.StandardInput.WriteLine("{\"command\": [\"set_property\", \"pause\", false]}");
            return Task.CompletedTask;
        }
        return Task.FromException(new Exception("Process has exited or been disposed."));
    }

    public Task Play(string filePath)
    {
        if (mpvProcess != null && socatProcess != null && !mpvProcess.HasExited && !socatProcess.HasExited)
        {
            socatProcess.StandardInput.WriteLine($"{{\"command\": [\"loadfile\", \"{filePath}\", \"replace\"]}}");
            return Task.CompletedTask;
        }
        return Task.FromException(new Exception("Process has exited or been disposed."));
    }

    public Task Pause()
    {
        if (mpvProcess != null && socatProcess != null && !mpvProcess.HasExited && !socatProcess.HasExited)
        {
            socatProcess.StandardInput.WriteLine("{\"command\": [\"set_property\", \"pause\", true]}");
            return Task.CompletedTask;
        }
        return Task.FromException(new Exception("Process has exited or been disposed."));
    }

    public Task Stop()
    {
        throw new NotImplementedException();
    }

    public Task Seek(long seconds)
    {
        
        if (mpvProcess != null && socatProcess != null && !mpvProcess.HasExited && !socatProcess.HasExited)
        {
            socatProcess.StandardInput.WriteLine($"{{ \"command\": [ \"seek\", \"{seconds}\", \"absolute\" ] }}");
            return Task.CompletedTask;
        }
        return Task.FromException(new Exception("Process has exited or been disposed."));
    }

    public Task SetVolume(float volume)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetDuration()
    {
        if (mpvProcess != null && socatProcess != null && !mpvProcess.HasExited && !socatProcess.HasExited)
        {
            socatProcess.StandardOutput.DiscardBufferedData();
            socatProcess.StandardInput.WriteLine("{ \"command\": [ \"get_property\", \"duration\"], \"request_id\": 101 }");
            Thread.Sleep(5);
            string? mpvOutput = String.Empty;
            while (!mpvOutput?.Contains("\"request_id\":101") ?? false)
            {
                mpvOutput = socatProcess.StandardOutput.ReadLine();
            }
            if (mpvOutput != null)
            {
                MPVDurationResult? mpvResult = JsonConvert.DeserializeObject<MPVDurationResult>(mpvOutput);
                return Task.FromResult((long)(mpvResult?.data ?? 0L));
            }
        }
        return Task.FromResult(0L);
    }

    public Task<long> GetPlayTime()
    {
        if (mpvProcess != null && socatProcess != null && !mpvProcess.HasExited && !socatProcess.HasExited)
        {
            socatProcess.StandardOutput.DiscardBufferedData();
            socatProcess.StandardInput.WriteLine("{ \"command\": [ \"get_property\", \"time-pos\"], \"request_id\": 102 }");
            Thread.Sleep(5);
            string? mpvOutput = String.Empty;
            while (!mpvOutput?.Contains("\"request_id\":102") ?? false)
            {
                mpvOutput = socatProcess.StandardOutput.ReadLine();
            }
            if (mpvOutput != null)
            {
                MPVDurationResult? mpvResult = JsonConvert.DeserializeObject<MPVDurationResult>(mpvOutput);
                return Task.FromResult((long)(mpvResult?.data ?? 0L));
            }
        }
        return Task.FromResult(0L);
    }

    public void Dispose()
    {
        if (isDisposed) return;
        mpvProcess?.Dispose();
        socatProcess?.Dispose();
        mpvProcess = null;
        socatProcess = null;
        GC.SuppressFinalize(this);
        isDisposed = true;
    }
}