using MWP.FFmpeg.DataTypes;
namespace MWP.FFmpeg.Interfaces;

public interface IFFmpeg
{
    public Task Run(string arguments, FFmpegSession session) => throw new NotImplementedException();
}