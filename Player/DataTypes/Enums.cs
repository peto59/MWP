namespace MWP.Player.DataTypes
{
    /// <summary>
    /// Enumerator for all loop states
    /// </summary>
    public enum LoopState
    {
        /// <summary>
        /// no looping
        /// </summary>
        None,
        /// <summary>
        /// looping through entire queue
        /// </summary>
        All,
        /// <summary>
        /// looping of single song
        /// </summary>
        Single
    }

    public enum RequestCodes : int
    {
        None = 0,
        PlayState = 100,
        Duration = 101,
        PlayTime = 102,
    }
}