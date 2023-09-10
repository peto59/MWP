using System;
using System.Collections.Generic;
using Android.App;
using Android.Graphics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Ass_Pain.BackEnd;
using Newtonsoft.Json;
#if DEBUG
using Ass_Pain.Helpers;
#endif

namespace Ass_Pain
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class Album : MusicBaseContainer
    {
        [JsonProperty]
        public override string Title { get; }
        public override List<Song> Songs { get; } = new List<Song>();
        public Song Song => Songs.Count > 0 ? Songs[0] : new Song("No Name", new DateTime(1970, 1, 1), "Default", false);
        public List<Artist> Artists { get; } = new List<Artist>();
        public Artist Artist => Artists.Count > 0 ? Artists[0] : new Artist("No Artist", "Default", false);
        public string ImgPath { get; }
        public bool Initialized { get; private set; } = true;
        public bool Showable { get; private set; } = true;
        public override Bitmap Image => GetImage();

        public void AddArtist(ref List<Artist> artists)
        {
            foreach (Artist artist in artists.Where(artist => !Artists.Contains(artist)))
            {
                Artists.Add(artist);
            }
        }
        public void AddArtist(ref Artist artist)
        {
            if(!Artists.Contains(artist))
                Artists.Add(artist);
        }
        
        public void AddSong(ref List<Song> songs)
        {
            foreach (Song song in songs.Where(song => !Songs.Contains(song)))
            {
                Songs.Add(song);
            }
        }
        public void AddSong(ref Song song)
        {
            if (!Songs.Contains(song))
                Songs.Add(song);
        }
        
        public void RemoveSong(Song song)
        {
            Songs.Remove(song);
        }
        
        public void RemoveSong(List<Song> songs)
        {
            songs.ForEach(RemoveSong);
        }
        
        public void RemoveArtist(Artist artist)
        {
            Artists.Remove(artist);
        }
        
        public void RemoveArtist(List<Artist> artists)
        {
            artists.ForEach(RemoveArtist);
        }
        
        ///<summary>
        ///Nukes this object out of existence
        ///</summary>
        public void Nuke()
        {
            Songs.ForEach(song => song.RemoveAlbum(this));
            Artists.ForEach(artist => artist.RemoveAlbum(this));
            MainActivity.stateHandler.Albums.Remove(this);
            Initialized = false;
        }
        
        public static string GetImagePath(string name, string artistPart)
        {
            string albumPart = FileManager.Sanitize(name);
            if (File.Exists($"{FileManager.MusicFolder}/{artistPart}/{albumPart}/cover.jpg"))
                return $"{FileManager.MusicFolder}/{artistPart}/{albumPart}/cover.jpg";
            if (File.Exists($"{FileManager.MusicFolder}/{artistPart}/{albumPart}/cover.png"))
                return $"{FileManager.MusicFolder}/{artistPart}/{albumPart}/cover.png";
            return "Default";
        }

        public override Bitmap GetImage(bool shouldFallBack = true)
        {
            Bitmap image = null;

            try
            {
                if (!string.IsNullOrEmpty(ImgPath) && ImgPath != "Default")
                {
                    using FileStream f = File.OpenRead(ImgPath);
                    image = BitmapFactory.DecodeStream(f);
                    f.Close();
                }
                else if (shouldFallBack)
                {
                    foreach (Song song in Songs.Where(song => song.Initialized))
                    {
                        image = song.GetImage(false);
                        if (image != null)
                        {
                            break;
                        }
                    }

                    if (image == null)
                    {
                        foreach (Artist artist in Artists.Where(artist => artist.Initialized))
                        {
                            image = artist.GetImage(false);
                            if (image != null)
                            {
                                break;
                            }
                        }
                    }
                }
                if (image == null)
                {
                    if (Application.Context.Assets != null)
                        image = BitmapFactory.DecodeStream(
                            Application.Context.Assets.Open(
                                "music_placeholder.png")); //In case of no cover and no embedded picture show default image from assets 
                }
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                if (Application.Context.Assets != null)
                    image = BitmapFactory.DecodeStream(
                        Application.Context.Assets.Open(
                            "music_placeholder.png")); //In case of no cover and no embedded picture show default image from assets
            }
            return image;
        }

        public Album(string title, Song song, Artist artist, string imgPath)
        {
            Title = title;
            Songs = new List<Song> {song};
            Artists = new List<Artist> {artist};
            ImgPath = imgPath;
        }
        
        public Album(string title, List<Song> song, Artist artist, string imgPath)
        {
            Title = title;
            Songs = song;
            Artists = new List<Artist> {artist};
            ImgPath = imgPath;
        }
        
        public Album(string title, Song song, List<Artist> artist, string imgPath)
        {
            Title = title;
            Songs = new List<Song> {song};
            Artists = artist;
            ImgPath = imgPath;
        }
        
        public Album(string title, List<Song> song, List<Artist> artist, string imgPath)
        {
            Title = title;
            Songs = song;
            Artists = artist;
            ImgPath = imgPath;
        }
        
        public Album(string title, string imgPath, bool initialized = true, bool showable = true)
        {
            Title = title;
            ImgPath = imgPath;
            Initialized = initialized;
            Showable = showable;
        }

        [JsonConstructor]
        public Album(string title)
        {
            Title = title;
            Showable = false;
            Initialized = false;
        }

        public Album(Album album, string imgPath)
        {
            Title = album.Title;
            Songs = album.Songs;
            Artists = album.Artists;
            ImgPath = imgPath;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is Album item && Equals(item);
        }

        protected bool Equals(Album other)
        {
            //todo: stack smashing?
            return Title == other.Title && Equals(Songs, other.Songs) && Equals(Artists, other.Artists) && Equals(ImgPath, other.ImgPath);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Title, Songs, Artists, ImgPath);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Album: title> {Title} song> {Song.Title} artist> {Artist.Title}";
        }
    }
}