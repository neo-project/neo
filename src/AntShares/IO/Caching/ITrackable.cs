namespace AntShares.IO.Caching
{
    internal interface ITrackable<TKey>
    {
        TKey Key { get; }
        TrackState TrackState { get; set; }
    }
}
