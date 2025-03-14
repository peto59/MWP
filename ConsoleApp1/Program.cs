using MWP;
using MWP.DatatypesAndExtensions;
using MWP.Player;

namespace ConsoleApp1;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        run();
        while (true)
        {
            
        }
        return;
    }

    static async Task run()
    {
        Player player = new Player();
        await player.Initialize();
        await player.Play("/home/adam/Downloads/dirty-magic.mp3");
        while (true)
        {
            
        }
        return;
    }
}