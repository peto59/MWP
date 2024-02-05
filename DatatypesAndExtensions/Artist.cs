using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.Graphics;
using Android.Support.V4.Media;
using MWP.BackEnd;
using MWP.DatatypesAndExtensions;
using Newtonsoft.Json;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP
{
    /// <summary>
    /// Custom Artist object
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Artist : MusicBaseContainer
    {
        /// <inheritdoc />
        [JsonProperty]
        public override string Title { get; protected internal set; }

        /// <inheritdoc />
        public override List<Song> Songs { get; } = new List<Song>();
        /// <summary>
        /// First or default <see cref="MWP.Song"/>
        /// </summary>
        public Song Song => Songs.Count > 0 ? Songs[0] : new Song("No Name", new DateTime(1970, 1, 1), "Default", false);

        /// <summary>
        /// List of <see cref="MWP.Album"/>s composed by this <see cref="MWP.Artist"/>
        /// </summary>
        public List<Album> Albums { get; } = new List<Album> {new Album("Uncategorized", "Default", true, false)};
        /// <summary>
        /// First or default <see cref="MWP.Album"/>
        /// </summary>
        public Album Album => Albums.Count > 0 ? Albums[0] : new Album("No Album", "Default", false);
        /// <summary>
        /// Path to image
        /// </summary>
        public string ImgPath { get; }
        /// <summary>
        /// Whether this instance is usable
        /// </summary>
        public bool Initialized { get; private set; } = true;
        //public override Bitmap Image => GetImage() ?? throw new InvalidOperationException();

        /// <summary>
        /// Adds <see cref="MWP.Album"/>s from <paramref name="albums"/>
        /// </summary>
        /// <param name="albums">List of <see cref="MWP.Album"/>s to be added</param>
        public void AddAlbum(ref List<Album> albums)
        {
            foreach (Album album in albums.Where(album => !Albums.Contains(album)))
            {
                Albums.Add(album);
            }
        }
        
        /// <summary>
        /// Adds <see cref="MWP.Album"/> from <paramref name="album"/>
        /// </summary>
        /// <param name="album">Album to be added</param>
        public void AddAlbum(ref Album album)
        {
            if (!Albums.Contains(album))
                Albums.Add(album);
        }
        
        /// <summary>
        /// Adds <see cref="MWP.Song"/>s from <paramref name="songs"/>
        /// </summary>
        /// <param name="songs">List of <see cref="MWP.Song"/>s to be added</param>
        public void AddSong(ref List<Song> songs)
        {
            foreach (Song song in songs.Where(song => !Songs.Contains(song)))
            {
                Songs.Add(song);
            }
        }
        /// <summary>
        /// Adds <see cref="MWP.Song"/> from <paramref name="song"/>
        /// </summary>
        /// <param name="song"><see cref="MWP.Song"/> to be added</param>
        public void AddSong(ref Song song)
        {
            if (!Songs.Contains(song))
                Songs.Add(song);
        }
        /// <summary>
        /// Removes <see cref="MWP.Song"/> matching <paramref name="song"/>
        /// </summary>
        /// <param name="song"><see cref="MWP.Song"/> to be removed</param>
        public void RemoveSong(Song song)
        {
            Songs.Remove(song);
        }
        /// <summary>
        /// Removes <see cref="MWP.Song"/>s matching <paramref name="songs"/>
        /// </summary>
        /// <param name="songs">List of <see cref="MWP.Song"/>s to be removed</param>
        public void RemoveSong(List<Song> songs)
        {
            songs.ForEach(RemoveSong);
        }
        /// <summary>
        /// Removes <see cref="MWP.Album"/> matching <paramref name="album"/>
        /// </summary>
        /// <param name="album"><see cref="MWP.Album"/> to be removed</param>
        public void RemoveAlbum(Album album)
        {
            Albums.Remove(album);
        }
        /// <summary>
        /// Removes <see cref="MWP.Album"/>s matching <paramref name="albums"/>
        /// </summary>
        /// <param name="albums">List of <see cref="MWP.Album"/>s to be removed</param>
        public void RemoveAlbum(List<Album> albums)
        {
            albums.ForEach(RemoveAlbum);
        }
        
        ///<summary>
        ///Nukes this object out of existence
        ///</summary>
        public void Nuke()
        {
            Songs.ForEach(song =>
            {
                song.RemoveArtist(this);
            });
            Albums.ForEach(album =>
            {
                album.RemoveArtist(this);
            });
            MainActivity.StateHandler.Artists.Remove(this);
            Initialized = false;
        }

        /// <summary>
        /// Gets full path to <see cref="MWP.Artist"/>'s image
        /// </summary>
        /// <param name="name">name of <see cref="MWP.Artist"/></param>
        /// <returns>Path to image></returns>
        public static string GetImagePath(string name)
        {
            string artistPart = FileManager.Sanitize(name);
            if (File.Exists($"{FileManager.MusicFolder}/{artistPart}/cover.jpg"))
                return $"{FileManager.MusicFolder}/{artistPart}/cover.jpg";
            if (File.Exists($"{FileManager.MusicFolder}/{artistPart}/cover.png"))
                return $"{FileManager.MusicFolder}/{artistPart}/cover.png";
            return "Default";
        }
        
        /// <summary>
        /// Gets <see cref="MWP.Artist"/>'s image
        /// </summary>
        /// <param name="shouldFallBack">Whether to look for image in <see cref="Albums"/> or <see cref="Songs"/></param>
        /// <returns><see cref="MWP.Artist"/>'s image</returns>
        public override Bitmap? GetImage(bool shouldFallBack = true)
        {
            Bitmap? image = null;

            try
            {
                if (!string.IsNullOrEmpty(ImgPath) && ImgPath != "Default")
                {
                    using FileStream f = File.OpenRead(ImgPath);
                    image = BitmapFactory.DecodeStream(f);
                    f.Close();
                }
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                return null;
            }   
            
            if (image != null || !shouldFallBack)
            {
                return image;
            }
            
            foreach (Song song in Songs.Where(song => song.Initialized))
            {
                image = song.GetImage(false);
                if (image != null)
                {
                    return image;
                }
            }
            
            foreach (Album album in Albums.Where(album => album.Initialized))
            {
                image = album.GetImage(false);
                if (image != null)
                {
                    return image;
                }
            }
            return MusicBaseClassStatic.Placeholder;
        }

        /// <summary>
        /// Adds new alias to this <see cref="MWP.Artist"/>
        /// </summary>
        /// <param name="newAlias">new alias for this <see cref="MWP.Artist"/></param>
        public void AddAlias(string newAlias)
        {
            string json = File.ReadAllText($"{FileManager.MusicFolder}/aliases.json");
            Dictionary<string, string> aliases = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            aliases.Add(Title, newAlias);
            File.WriteAllTextAsync($"{FileManager.MusicFolder}/aliases.json", JsonConvert.SerializeObject(aliases));
            Title = newAlias;
        }

        /// <inheritdoc />
        public Artist(string title, Song song, Album album, string imgPath)
        {
            Title = title;
            Songs = new List<Song> {song};
            Albums = new List<Album> {album};
            ImgPath = imgPath;
        }

        /// <inheritdoc />
        public Artist(string title, List<Song> song, Album album, string imgPath)
        {
            Title = title;
            Songs = song;
            Albums = new List<Album> {album};
            ImgPath = imgPath;
        }

        /// <inheritdoc />
        public Artist(string title, Song song, List<Album> album, string imgPath)
        {
            Title = title;
            Songs = new List<Song> {song};
            Albums = album;
            ImgPath = imgPath;
        }

        /// <inheritdoc />
        public Artist(string title, List<Song> song, List<Album> album, string imgPath)
        {
            Title = title;
            Songs = song;
            Albums = album;
            ImgPath = imgPath;
        }

        /// <inheritdoc />
        public Artist(string title, string imgPath, bool initialized = true)
        {
            Title = title;
            ImgPath = imgPath;
            Initialized = initialized;
        }

        /// <inheritdoc />
        [JsonConstructor]
        public Artist(string title)
        {
            Title = title;
            ImgPath = string.Empty;
            Initialized = false;
        }

        /// <inheritdoc />
        public Artist(Artist artist, string imgPath)
        {
            Title = artist.Title;
            Songs = artist.Songs;
            Albums = artist.Albums;
            ImgPath = imgPath;
        }

        /// <inheritdoc />
        public override MediaBrowserCompat.MediaItem? ToMediaItem()
        {
            if (Description == null) return null;
            //MediaBrowserCompat.MediaItem item = new MediaBrowserCompat.MediaItem(Description, MediaBrowserCompat.MediaItem.FlagPlayable | MediaBrowserCompat.MediaItem.FlagBrowsable);
            MediaBrowserCompat.MediaItem item = new MediaBrowserCompat.MediaItem(Description, MediaBrowserCompat.MediaItem.FlagBrowsable);
            return item;
        }

        /// <inheritdoc />
        protected override MediaDescriptionCompat? GetDescription()
        {
            return Builder?.Build();
        }

        /// <inheritdoc />
        protected override MediaDescriptionCompat.Builder? GetBuilder()
        {
            return new MediaDescriptionCompat.Builder()
                .SetMediaId($"{(byte)MediaType.Artist}{IdString}")?
                .SetTitle(Title)?
                .SetIconBitmap(Image);
        }
        
        /// <summary>
        /// Gets <see cref="MWP.Artist"/> from <paramref name="id"/>
        /// </summary>
        /// <param name="id">Id of <see cref="MWP.Artist"/> to return</param>
        /// <returns><see cref="MWP.Artist"/> matching <paramref name="id"/></returns>
        public static Artist FromId(Guid id)
        {
            return MainActivity.StateHandler.Artists.Find(a => a.Id.Equals(id));
        }
        
        /// <summary>
        /// Gets <see cref="MWP.Artist"/> from <paramref name="id"/>
        /// </summary>
        /// <param name="id">Id of <see cref="MWP.Artist"/> to return</param>
        /// <returns><see cref="MWP.Artist"/> matching <paramref name="id"/></returns>
        public static Artist FromId(string id)
        {
            return MainActivity.StateHandler.Artists.Find(a => a.IdString.Equals(id));
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is Artist item && Equals(item);
        }

        /// <summary>
        /// Whether two <see cref="MWP.Artist"/>s are equal
        /// </summary>
        /// <param name="other">other <see cref="MWP.Artist"/></param>
        /// <returns>true if albums match, false otherwise</returns>
        private bool Equals(Artist other)
        {
            return Title == other.Title && Equals(Songs, other.Songs) && Equals(Albums, other.Albums) && Equals(ImgPath, other.ImgPath);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return HashCode.Combine(Title, Songs, Albums, ImgPath);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Artist: title> {Title} song> {Song.Title} album> {Album.Title}";
        }
    }
}