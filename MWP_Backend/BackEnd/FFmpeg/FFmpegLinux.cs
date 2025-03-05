using System.Diagnostics;

namespace MWP.BackEnd.FFmpeg;

public class FFmpegLinux : IFFmpeg
{
    public async Task Run(string arguments, FFmpegSession session)
    {
        using Process process = new Process();
        process.StartInfo.FileName = "ffmpeg";
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
            
        process.Start();
        session.ReturnCode = FFmpegStatusCode.Running;
        string ffmepgResult = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
    }
}