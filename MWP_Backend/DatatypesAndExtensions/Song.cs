using MWP_Backend.BackEnd;
using MWP.BackEnd.Helpers;
using MWP;
using MWP.BackEnd;
using MWP.DatatypesAndExtensions;
using File = TagLib.File;

namespace MWP_Backend.DatatypesAndExtensions
{
    /// <summary>
    /// Custom Song object
    /// </summary>
    [Serializable]
    public sealed class Song : MusicBaseClass
    {
        /// <summary>
        /// List of <see cref="DatatypesAndExtensions.Artist"/>s collaborating on this <see cref="MWP_Backend.DatatypesAndExtensions.Album"/>
        /// </summary>
        public List<Artist> Artists { get; } = new List<Artist>();
        /// <summary>
        /// First or default <see cref="DatatypesAndExtensions.Artist"/>
        /// </summary>
        public Artist Artist => Artists.Count > 0 ? Artists[0] : new Artist("No Artist", "Default", false);
        /// <summary>
        /// List of <see cref="MWP_Backend.DatatypesAndExtensions.Album"/>s this <see cref="Song"/> belongs to
        /// </summary>
        public List<Album> Albums { get; } = new List<Album>();
        /// <summary>
        /// List of <see cref="MWP_Backend.DatatypesAndExtensions.Album"/>s this <see cref="Song"/> belongs to and are usable in serialization
        /// </summary>
        public List<Album> XmlAlbums => Albums.Where(a => a.Title != "No Album").ToList();
        /// <summary>
        /// First or default <see cref="MWP_Backend.DatatypesAndExtensions.Album"/>
        /// </summary>
        public Album Album => Albums.Count > 0 ? Albums[0] : new Album("No Album", "Default", false);

        /// <summary>
        /// Which playlists ist this song member of
        /// </summary>
        public List<string> Playlists { get; } = new List<string>();

        /// <inheritdoc />
        public override string Title { get; protected internal set; }

        /// <summary>
        /// Timestamp when was this <see cref="Song"/> added to device
        /// </summary>
        public DateTime DateCreated { get; }
        /// <summary>
        /// Path to <see cref="Song"/> on disk
        /// </summary>
        public string Path { get; }
        /// <summary>
        /// Whether this instance is usable
        /// </summary>
        public bool Initialized { get; private set; } = true;

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
        /// Adds <see cref="MWP_Backend.DatatypesAndExtensions.Album"/>s from <paramref name="albums"/>
        /// </summary>
        /// <param name="albums">List of <see cref="MWP_Backend.DatatypesAndExtensions.Album"/>s to be added</param>
        public void AddAlbum(ref List<Album> albums)
        {
            foreach (Album album in albums.Where(album => !Albums.Contains(album)))
            {
                Albums.Add(album);
            }
        }
        /// <summary>
        /// Adds <see cref="MWP_Backend.DatatypesAndExtensions.Album"/> from <paramref name="album"/>
        /// </summary>
        /// <param name="album">Album to be added</param>
        public void AddAlbum(ref Album album)
        {
            if (!Albums.Contains(album))
                Albums.Add(album);
        }

        /// <summary>
        /// Adds <paramref name="playlist"/> to <see cref="Playlists"/>
        /// </summary>
        /// <param name="playlist">playlist to be added to <see cref="Playlists"/></param>
        public void AddPlaylist(string playlist)
        {
            Playlists.Add(playlist);
        }
        
        /// <summary>
        /// Removes <see cref="MWP_Backend.DatatypesAndExtensions.Album"/> matching <paramref name="album"/>
        /// </summary>
        /// <param name="album"><see cref="MWP_Backend.DatatypesAndExtensions.Album"/> to be removed</param>
        public void RemoveAlbum(Album album)
        {
            Albums.Remove(album);
        }
        /// <summary>
        /// Removes <see cref="MWP_Backend.DatatypesAndExtensions.Album"/>s matching <paramref name="albums"/>
        /// </summary>
        /// <param name="albums">List of <see cref="MWP_Backend.DatatypesAndExtensions.Album"/>s to be removed</param>
        public void RemoveAlbum(List<Album> albums)
        {
            albums.ForEach(RemoveAlbum);
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
            //TODO: delete from queue pripadne aj inde
            Albums.ForEach(album =>
            {
                album.RemoveSong(this);
                if (album.Songs.Count == 0)
                {
                    album.Nuke();
                }

            });
            
            Artists.ForEach(artist =>
            {
                artist.RemoveSong(this);
                if (artist.Songs.Count == 0 && artist.Albums.Sum(album => album.Songs.Count) == 0)
                {
                    artist.Nuke();
                }
            });
            
            StateHandler.Songs.Remove(this);
            Initialized = false;
        }

        /// <summary>
        /// Deletes this <see cref="Song"/> from device
        /// </summary>
        public void Delete()
        {
            Nuke();
            FileManager.Delete(Path);
        }

        /// <summary>
        /// Gets <see cref="Song"/>'s image
        /// </summary>
        /// <param name="shouldFallBack">Whether to look for image in <see cref="Albums"/> or <see cref="Artists"/></param>
        /// <returns><see cref="Song"/>'s image</returns>
        public override byte[]? GetImage(bool shouldFallBack = true)
        {
            byte[]? image = null;
            if (Path != "Default")
            {
                try
                {
                    using File tagFile = File.Create(Path);
                    tagFile.Mode = File.AccessMode.Read;
                    using MemoryStream ms = new MemoryStream(tagFile.Tag.Pictures[0].Data.Data);
                    image = ms.ToArray();
                }
                catch (Exception ex)
                {
#if DEBUG
                    MyConsole.WriteLine(ex);
                    MyConsole.WriteLine($"Doesnt contain image: {Path}");
#endif
                }
            }

            if (image != null || !shouldFallBack) return image;
            
            foreach (Album album in Albums.Where(album => album.Initialized))
            {
                image = album.GetImage(false);
                if (image != null)
                {
                    return image;
                }
            }

            if (image != null) return image;
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
        public Song(List<Artist>? artists, List<Album>? albums, string title, bool redundant = false)
        {
            if (artists != null)
            {
                Artists = artists.Distinct().ToList();
            }
            if (albums != null)
            {
                Albums = albums.Distinct().ToList();
            }
            Title = title;
            Path = string.Empty;
            Initialized = false;
        }

        /// <inheritdoc />
        public Song(List<Artist> artists, string title, DateTime dateCreated, string path, Album? album = null)
        {
            Artists = artists.Distinct().ToList();
            if (album != null)
            {
                Albums = new List<Album> {album};
            }
            Title = title;
            DateCreated = dateCreated;
            Path = path;
        }

        /// <inheritdoc />
        public Song(Artist artist, string title, DateTime dateCreated, string path, Album? album = null)
        {
            Artists = new List<Artist> {artist};
            if (album != null)
            {
                Albums = new List<Album> {album};
            }
            Title = title;
            DateCreated = dateCreated;
            Path = path;
        }

        /// <inheritdoc />
        public Song(List<Artist>? artists, string title, DateTime dateCreated, string path, List<Album>? albums, bool initialized = true)
        {
            if (artists != null) Artists = artists.Distinct().ToList();
            if (albums != null) Albums = albums.Distinct().ToList();
            Title = title;
            DateCreated = dateCreated;
            Path = path;
            Initialized = initialized;
        }

        /// <inheritdoc />
        public Song(Artist artist, string title, DateTime dateCreated, string path, List<Album> albums)
        {
            Artists = new List<Artist> {artist};
            Albums = albums.Distinct().ToList();
            Title = title;
            DateCreated = dateCreated;
            Path = path;
        }

        /// <inheritdoc />
        public Song(string title, DateTime dateCreated, string path, bool initialized = true)
        {
            Title = title;
            DateCreated = dateCreated;
            Path = path;
            Initialized = initialized;
        }

        /// <inheritdoc />
        public Song(Song song, string title, string path)
        {
            Artists = song.Artists;
            Albums = song.Albums;
            Title = title;
            DateCreated = song.DateCreated;
            Path = path;
        }
        #if ANDROID
        /// <inheritdoc />
        public override MediaBrowserCompat.MediaItem? ToMediaItem()
        {
            if (Description == null) return null;
            MediaBrowserCompat.MediaItem item = new MediaBrowserCompat.MediaItem(Description, MediaBrowserCompat.MediaItem.FlagPlayable);
            return item;
        }
        
        /// <summary>
        /// This <see cref="MWP.Song"/> as <see cref="MediaSessionCompat.QueueItem"/>
        /// </summary>
        /// <param name="id">id in <see cref="MediaSessionCompat.QueueItem"/></param>
        /// <returns><see cref="MediaSessionCompat.QueueItem"/></returns>
        public MediaSessionCompat.QueueItem? ToQueueItem(long id)
        {
            if (Description == null) return null;
            MediaSessionCompat.QueueItem item = new MediaSessionCompat.QueueItem(Description, id);
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
                .SetMediaId($"{(byte)MediaType.Song}{IdString}")?
                .SetTitle(Title)?
                .SetSubtitle(Artist.Title)?
                .SetIconBitmap(Image);
        }

        /// <summary>
        /// Gets <see cref="Song"/> from <paramref name="id"/>
        /// </summary>
        /// <param name="id">Id of <see cref="Song"/> to return</param>
        /// <returns><see cref="Song"/> matching <paramref name="id"/></returns>
        public static Song FromId(Guid id)
        {
            return MainActivity.StateHandler.Songs.Find(s => s.Id.Equals(id));
        }
        
        /// <summary>
        /// Gets <see cref="Song"/> from <paramref name="id"/>
        /// </summary>
        /// <param name="id">Id of <see cref="Song"/> to return</param>
        /// <returns><see cref="Song"/> matching <paramref name="id"/></returns>
        public static Song FromId(string id)
        {
            return MainActivity.StateHandler.Songs.Find(s => s.IdString.Equals(id));
        }
        #endif

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is Song item && Equals(item);
        }

        private bool Equals(Song other)
        {
            return Equals(Artists, other.Artists) && Equals(Albums, other.Albums) && Title == other.Title && DateCreated.Equals(other.DateCreated) && Path == other.Path;
        }
        
        /// <summary>
        /// Whether two <see cref="DatatypesAndExtensions.Artist"/>s are equal without chance of stack smashing occuring
        /// </summary>
        /// <param name="other">other <see cref="DatatypesAndExtensions.Artist"/></param>
        /// <returns>true if albums match, false otherwise</returns>
        public bool ShallowEquals(Song other)
        {
            bool equals = Artists.Aggregate(true, (current1, thisArtist) => other.Artists.Aggregate(current1, (current, otherArtist) => current && thisArtist.Title == otherArtist.Title));

            if (!equals)
            {
                return false;
            }
            equals = Albums.Aggregate(true, (current1, thisAlbum) => other.Albums.Aggregate(current1, (current, otherAlbum) => current && thisAlbum.Title == otherAlbum.Title));
            if (!equals)
            {
                return false;
            }
            return Title == other.Title;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return HashCode.Combine(Artists, Albums, Title, DateCreated, Path);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Song: title> {Title} author> {Artist.Title} album> {Album.Title} dateCreated> {DateCreated} path> {Path}";
        }
    }
}