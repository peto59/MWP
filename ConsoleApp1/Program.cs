﻿using MWP;
using MWP.DatatypesAndExtensions;
using MWP.Player;

namespace ConsoleApp1;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        Player player = new Player();
        player.Play("/home/lenovolegion/Downloads/a.mp3");
        while (true)
        {
            
        }
        return;
    }
}