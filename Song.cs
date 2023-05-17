using System;

namespace Ass_Pain
{
    public class Song
    {
        public string[] Artists { get; }
        public string Artist
        {
            get
            {
                return Artists[0] ?? "No Artist";
            }
        }

        public override string ToString()
        {
            return $"{Name} : author> {Artist} album> {Album} path> {Path}";
        }

        public string Album { get; }
        public string Name { get; }
        public string Path { get; }

        public Song(string[] artists, string name, string path, string album = null)
        {
            Artists = artists;
            Album = album;
            Name = name;
            Path = path;
        }
        public Song(string artist, string name, string path, string album = null)
        {
            Artists = new[] { artist };
            Album = album;
            Name = name;
            Path = path;
        }

    }
}