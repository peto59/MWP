using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace MWP
{
    // CoverArt myDeserializedClass = JsonConvert.DeserializeObject<CoverArt>(myJsonResponse);
    /// <summary>
    /// Class for deserialization of CoverArt API call
    /// </summary>
    /// <param name="approved"></param>
    /// <param name="back"></param>
    /// <param name="comment"></param>
    /// <param name="edit"></param>
    /// <param name="front"></param>
    /// <param name="id"></param>
    /// <param name="image"></param>
    /// <param name="thumbnails"></param>
    /// <param name="types"></param>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public record Image(
        [property: JsonProperty("approved", NullValueHandling = NullValueHandling.Ignore)] bool approved,
        [property: JsonProperty("back", NullValueHandling = NullValueHandling.Ignore)] bool back,
        [property: JsonProperty("comment", NullValueHandling = NullValueHandling.Ignore)] string comment,
        [property: JsonProperty("edit", NullValueHandling = NullValueHandling.Ignore)] int edit,
        [property: JsonProperty("front", NullValueHandling = NullValueHandling.Ignore)] bool front,
        [property: JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)] object id,
        [property: JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)] string image,
        [property: JsonProperty("thumbnails", NullValueHandling = NullValueHandling.Ignore)] Thumbnails thumbnails,
        [property: JsonProperty("types", NullValueHandling = NullValueHandling.Ignore)] IReadOnlyList<string> types
    );

    /// <summary>
    /// Class for deserialization of CoverArt API call
    /// </summary>
    /// <param name="images"></param>
    /// <param name="release"></param>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public record CoverArt(
        [property: JsonProperty("images", NullValueHandling = NullValueHandling.Ignore)] IReadOnlyList<Image> images,
        [property: JsonProperty("release", NullValueHandling = NullValueHandling.Ignore)] string release
    );

    /// <summary>
    /// Class for deserialization of CoverArt API call
    /// </summary>
    /// <param name="_1200"></param>
    /// <param name="_250"></param>
    /// <param name="_500"></param>
    /// <param name="large"></param>
    /// <param name="small"></param>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public record Thumbnails(
        [property: JsonProperty("1200", NullValueHandling = NullValueHandling.Ignore)] string _1200,
        [property: JsonProperty("250", NullValueHandling = NullValueHandling.Ignore)] string _250,
        [property: JsonProperty("500", NullValueHandling = NullValueHandling.Ignore)] string _500,
        [property: JsonProperty("large", NullValueHandling = NullValueHandling.Ignore)] string large,
        [property: JsonProperty("small", NullValueHandling = NullValueHandling.Ignore)] string small
    );
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}
