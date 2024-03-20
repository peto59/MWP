namespace MWP.DatatypesAndExtensions
{
    /// <summary>
    /// Class for deserialization of Chromaprint json result
    /// </summary>
    public class ChromaprintResult
    {
        /// <summary>
        /// Duration of song
        /// </summary>
        public float duration = 0;
        /// <summary>
        /// Fingerprint of song
        /// </summary>
        public string fingerprint = string.Empty;
    }
}