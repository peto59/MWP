// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

namespace MWP.Player;
public class Program
{
    public static async Task Main(string[] args)
    {
        var player = new Player();
        while (true)
        {
            await player.Play("gardens.mp3");
            await player.GetDuration();
            await player.GetPlayTime();
            await player.Seek(90);
            await player.GetPlayTime();
            Thread.Sleep(1000);
            await player.Play("/home/adam/Downloads/dirty-magic.mp3");
            Thread.Sleep(1000);
        }
    }
}