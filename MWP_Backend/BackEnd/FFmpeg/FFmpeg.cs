namespace MWP.BackEnd.FFmpeg;

public class FFmpeg
{
    private readonly IFFmpeg ffmpeg;
    public FFmpeg()
    {
            
        if (OperatingSystem.IsWindows())
        {
            ffmpeg = new FFmpegWindows();
        }
        else if (OperatingSystem.IsLinux())
        {
            ffmpeg = new FFmpegLinux();
        }
        else if (OperatingSystem.IsAndroid())
        {
            ffmpeg = new FFmpegAndroid();
        }
        else
        {
            throw new PlatformNotSupportedException("FFmpeg not supported");
        }
    }

    public Task Run(string command, FFmpegSession session)
    {
        return ffmpeg.Run(command, session);
    }
}