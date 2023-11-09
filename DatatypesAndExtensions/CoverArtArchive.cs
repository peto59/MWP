using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace MWP
{
    // CoverArt myDeserializedClass = JsonConvert.DeserializeObject<CoverArt>(myJsonResponse);
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

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public record CoverArt(
        [property: JsonProperty("images", NullValueHandling = NullValueHandling.Ignore)] IReadOnlyList<Image> images,
        [property: JsonProperty("release", NullValueHandling = NullValueHandling.Ignore)] string release
    );

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
