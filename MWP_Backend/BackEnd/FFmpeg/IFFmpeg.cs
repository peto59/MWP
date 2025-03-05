namespace MWP.BackEnd.FFmpeg;

public interface IFFmpeg
{
    public Task Run(string arguments, FFmpegSession session) => throw new NotImplementedException();
}