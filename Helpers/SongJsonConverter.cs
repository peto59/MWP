using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MWP
{
    /// <inheritdoc />
    public class SongJsonConverter : JsonConverter<Song>
    {
        private bool includePrivateInfo;

        /// <inheritdoc />
        public SongJsonConverter(bool includePrivateInfo)
        {
            this.includePrivateInfo = includePrivateInfo;
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, Song? song, JsonSerializer serializer)
        {
            if (song == null) return;
            JObject jObject;
            if (includePrivateInfo)
            {
                jObject = new JObject
                {
                    { "PrivateInfo", true},
                    { "Title", song.Title },
                    { "DateCreated", song.DateCreated },
                    { "Path", song.Path },
                    { "Initialized", song.Initialized },
                    { "Artists", JArray.FromObject(song.Artists, serializer) },
                    { "Albums", JArray.FromObject(song.XmlAlbums, serializer) }
                };
            }
            else
            {
                jObject = new JObject
                {
                    { "PrivateInfo", false},
                    { "Title", song.Title },
                    { "Artists", JArray.FromObject(song.Artists, serializer) },
                    { "Albums", JArray.FromObject(song.XmlAlbums, serializer) }
                };
            }
            jObject.WriteTo(writer);
        }

        /// <inheritdoc />
        public override Song ReadJson(JsonReader reader, Type objectType, Song? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);
            Song song;
            List<Artist>? artists = jObject["Artists"]?.ToObject<List<Artist>>(serializer);
            List<Album>? albums = jObject["Albums"]?.ToObject<List<Album>>(serializer);
            if ((bool)(jObject["PrivateInfo"] ?? false) && jObject["DateCreated"] != null)
            {
                string? title = (string?)jObject["Title"];
                DateTime? date = jObject["DateCreated"]?.ToObject<DateTime>(serializer);
                string? path = (string?)jObject["Path"];
                if (title != null && date != null && path != null)
                {
                    song = new Song(
                        artists,
                        title,
                        (DateTime)date,
                        path,
                        albums,
                        (bool)(jObject["Initialized"] ?? false)
                    );
                    return song;
                }
            }
            else
            {
                string? title = (string?)jObject["Title"];
                if (title != null)
                {
                    song = new Song(
                        artists,
                        albums,
                        title
                    );
                    return song;
                }
            }
            return new Song(null, null, "a", false);
        }
    }
}