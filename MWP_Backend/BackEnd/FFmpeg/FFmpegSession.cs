namespace MWP.BackEnd.FFmpeg;

public class FFmpegSession
{
    public FFmpegStatusCode ReturnCode = FFmpegStatusCode.None;
    public string? RawOutput;
}

public enum FFmpegStatusCode
{
    None = 0,
    Success = 1,
    Error = 2,
    Running = 3,
}