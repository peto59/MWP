using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ass_Pain
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
        public override void WriteJson(JsonWriter writer, Song song, JsonSerializer serializer)
        {
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
        public override Song ReadJson(JsonReader reader, Type objectType, Song existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);
            Song song;
            List<Artist> artists = jObject["Artists"]?.ToObject<List<Artist>>(serializer);
            List<Album> albums = jObject["Albums"]?.ToObject<List<Album>>(serializer);
            if ((bool)jObject["PrivateInfo"] && jObject["DateCreated"] != null)
            {
                song = new Song(
                    artists,
                    (string)jObject["Title"],
                    jObject["DateCreated"].ToObject<DateTime>(serializer),
                    (string)jObject["Path"],
                    albums,
                    (bool)jObject["Initialized"]
                );
            }
            else
            {
                song = new Song(
                    artists,
                    albums,
                    (string)jObject["Title"]
                );
            }
            return song;
        }
    }
}