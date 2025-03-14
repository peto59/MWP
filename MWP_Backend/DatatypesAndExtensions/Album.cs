using MWP_Backend.BackEnd;
using MWP.BackEnd.Helpers;
using MWP;
using MWP.BackEnd;
using MWP.DatatypesAndExtensions;
using Newtonsoft.Json;

namespace MWP_Backend.DatatypesAndExtensions
{
    /// <summary>
    /// Custom Album object
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Album : MusicBaseContainer
    {
        /// <inheritdoc />
        [JsonProperty]
        public override string Title { get; protected internal set; }

        /// <inheritdoc />
        public override List<Song> Songs { get; } = new List<Song>();
        /// <summary>
        /// First or default <see cref="DatatypesAndExtensions.Song"/>
        /// </summary>
        public Song Song => Songs.Count > 0 ? Songs[0] : new Song("No Name", new DateTime(1970, 1, 1), "Default", false);
        /// <summary>
        /// List of <see cref="DatatypesAndExtensions.Artist"/>s collaborating on this <see cref="Album"/>
        /// </summary>
        public List<Artist> Artists { get; } = new List<Artist>();
        /// <summary>
        /// First or default <see cref="DatatypesAndExtensions.Artist"/>
        /// </summary>
        public Artist Artist => Artists.Count > 0 ? Artists[0] : new Artist("No Artist", "Default", false);
        /// <summary>
        /// Path to image
        /// </summary>
        public string ImgPath { get; }
        /// <summary>
        /// Whether this instance is usable
        /// </summary>
        public bool Initialized { get; private set; } = true;
        /// <summary>
        /// Whether this instance is show-able
        /// </summary>
        public bool Showable { get; private set; } = true;

        /// <summary>
        /// Adds <see cref="DatatypesAndExtensions.Artist"/>s from <paramref name="artists"/>
        /// </summary>
        /// <param name="artists">List of <see cref="DatatypesAndExtensions.Artist"/>s to be added</param>
        public void AddArtist(ref List<Artist> artists)
        {
            foreach (Artist artist in artists.Where(artist => !Artists.Contains(artist)))
            {
                Artists.Add(artist);
            }
        }
        /// <summary>
        /// Adds <see cref="DatatypesAndExtensions.Artist"/> from <paramref name="artist"/>
        /// </summary>
        /// <param name="artist">artist to be added</param>
        public void AddArtist(ref Artist artist)
        {
            if(!Artists.Contains(artist))
                Artists.Add(artist);
        }
        
        /// <summary>
        /// Adds <see cref="DatatypesAndExtensions.Song"/>s from <paramref name="songs"/>
        /// </summary>
        /// <param name="songs">List of <see cref="DatatypesAndExtensions.Song"/>s to be added</param>
        public void AddSong(ref List<Song> songs)
        {
            foreach (Song song in songs.Where(song => !Songs.Contains(song)))
            {
                Songs.Add(song);
            }
        }
        /// <summary>
        /// Adds <see cref="DatatypesAndExtensions.Song"/> from <paramref name="song"/>
        /// </summary>
        /// <param name="song"><see cref="DatatypesAndExtensions.Song"/> to be added</param>
        public void AddSong(ref Song song)
        {
            if (!Songs.Contains(song))
                Songs.Add(song);
        }
        
        /// <summary>
        /// Removes <see cref="DatatypesAndExtensions.Song"/> matching <paramref name="song"/>
        /// </summary>
        /// <param name="song"><see cref="DatatypesAndExtensions.Song"/> to be removed</param>
        public void RemoveSong(Song song)
        {
            Songs.Remove(song);
        }
        
        /// <summary>
        /// Removes <see cref="DatatypesAndExtensions.Song"/>s matching <paramref name="songs"/>
        /// </summary>
        /// <param name="songs">List of <see cref="DatatypesAndExtensions.Song"/>s to be removed</param>
        public void RemoveSong(List<Song> songs)
        {
            songs.ForEach(RemoveSong);
        }
        
        /// <summary>
        /// Removes <see cref="DatatypesAndExtensions.Artist"/> matching <paramref name="artist"/>
        /// </summary>
        /// <param name="artist"><see cref="DatatypesAndExtensions.Artist"/> to be removed</param>
        public void RemoveArtist(Artist artist)
        {
            Artists.Remove(artist);
        }
        
        /// <summary>
        /// Removes <see cref="DatatypesAndExtensions.Artist"/>s matching <paramref name="artists"/>
        /// </summary>
        /// <param name="artists">List of <see cref="DatatypesAndExtensions.Artist"/>s to be removed</param>
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
            StateHandler.Albums.Remove(this);
            Initialized = false;
        }
        
        /// <summary>
        /// Gets full path to <see cref="Album"/>'s image
        /// </summary>
        /// <param name="name">name of <see cref="Album"/></param>
        /// <param name="artistPart">name of first <see cref="Album"/>'s <see cref="DatatypesAndExtensions.Artist"/></param>
        /// <returns>Path to image></returns>
        public static string GetImagePath(string name, string artistPart)
        {
            string albumPart = FileManager.Sanitize(name);
            if (File.Exists($"{FileManager.MusicFolder}/{artistPart}/{albumPart}/cover.jpg"))
                return $"{FileManager.MusicFolder}/{artistPart}/{albumPart}/cover.jpg";
            if (File.Exists($"{FileManager.MusicFolder}/{artistPart}/{albumPart}/cover.png"))
                return $"{FileManager.MusicFolder}/{artistPart}/{albumPart}/cover.png";
            return "Default";
        }

        /// <summary>
        /// Gets <see cref="Album"/>'s image
        /// </summary>
        /// <param name="shouldFallBack">Whether to look for image in <see cref="Artists"/> or <see cref="Songs"/></param>
        /// <returns><see cref="Album"/>'s image</returns>
        public override byte[]? GetImage(bool shouldFallBack = true)
        {
            byte[]? image = null;

            try
            {
                if (!string.IsNullOrEmpty(ImgPath) && ImgPath != "Default")
                {
                    using FileStream f = File.OpenRead(ImgPath);
                    using MemoryStream ms = new MemoryStream();
                    f.CopyTo(ms);
                    image = ms.ToArray();
                    ms.Close();
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
            foreach (Artist artist in Artists.Where(artist => artist.Initialized))
            {
                image = artist.GetImage(false);
                if (image != null)
                {
                    return image;
                }
            }

            return MusicBaseClassStatic.Placeholder;
        }

        /// <inheritdoc />
        public Album(string title, Song song, Artist artist, string imgPath)
        {
            Title = title;
            Songs = new List<Song> {song};
            Artists = new List<Artist> {artist};
            ImgPath = imgPath;
        }

        /// <inheritdoc />
        public Album(string title, List<Song> song, Artist artist, string imgPath)
        {
            Title = title;
            Songs = song;
            Artists = new List<Artist> {artist};
            ImgPath = imgPath;
        }

        /// <inheritdoc />
        public Album(string title, Song song, List<Artist> artist, string imgPath)
        {
            Title = title;
            Songs = new List<Song> {song};
            Artists = artist;
            ImgPath = imgPath;
        }

        /// <inheritdoc />
        public Album(string title, List<Song> song, List<Artist> artist, string imgPath)
        {
            Title = title;
            Songs = song;
            Artists = artist;
            ImgPath = imgPath;
        }

        /// <inheritdoc />
        public Album(string title, string imgPath, bool initialized = true, bool showable = true)
        {
            Title = title;
            ImgPath = imgPath;
            Initialized = initialized;
            Showable = showable;
        }

        /// <inheritdoc />
        [JsonConstructor]
        public Album(string title)
        {
            Title = title;
            ImgPath = string.Empty;
            Showable = false;
            Initialized = false;
        }

        /// <inheritdoc />
        public Album(Album album, string imgPath)
        {
            Title = album.Title;
            Songs = album.Songs;
            Artists = album.Artists;
            ImgPath = imgPath;
        }

        #if ANDROID
        /// <inheritdoc />
        public override MediaBrowserCompat.MediaItem? ToMediaItem()
        {
            if (Description == null) return null;
            //int flags = MediaBrowserCompat.MediaItem.FlagBrowsable | MediaBrowserCompat.MediaItem.FlagPlayable;
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
                .SetMediaId($"{(byte)MediaType.Album}{IdString}")?
                .SetTitle(Title)?
                .SetSubtitle(Artist.Title)?
                .SetIconBitmap(Image);
        }
        #endif
        
        /// <summary>
        /// Gets <see cref="Album"/> from <paramref name="id"/>
        /// </summary>
        /// <param name="id">Id of <see cref="Album"/> to return</param>
        /// <returns><see cref="Album"/> matching <paramref name="id"/></returns>
        public static Album FromId(Guid id)
        {
            return StateHandler.Albums.Find(a => a.Id.Equals(id));
        }
        
        /// <summary>
        /// Gets <see cref="Album"/> from <paramref name="id"/>
        /// </summary>
        /// <param name="id">Id of <see cref="Album"/> to return</param>
        /// <returns><see cref="Album"/> matching <paramref name="id"/></returns>
        public static Album FromId(string id)
        {
            return StateHandler.Albums.Find(a => a.IdString.Equals(id));
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is Album item && Equals(item);
        }

        /// <summary>
        /// Whether two <see cref="Album"/>s are equal
        /// </summary>
        /// <param name="other">other <see cref="Album"/></param>
        /// <returns>true if albums match, false otherwise</returns>
        private bool Equals(Album other)
        {
            return Title == other.Title && Equals(Songs, other.Songs) && Equals(Artists, other.Artists) && Equals(ImgPath, other.ImgPath);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return HashCode.Combine(Title, Songs, Artists, ImgPath);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Album: title> {Title} song> {Song.Title} artist> {Artist.Title}";
        }
    }
}