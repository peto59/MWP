using MWP;
using MWP.DatatypesAndExtensions;

namespace ConsoleApp1;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        MWP_Backend.BackEnd.Downloader.Download("https://www.youtube.com/watch?v=sVQJXjTYAbk", DownloadActions.DownloadWithMbid);
    }
}