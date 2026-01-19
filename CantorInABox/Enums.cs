namespace Mooseware.CantorInABox;

/// <summary>
/// Parts of the Playback tab user interface. Used to control what is refreshed.
/// </summary>
[Flags]
public enum PlaybackTabUiParts
{
    None = 0b_0000_0000,  // 0
    /// <summary>
    /// Playlist title and filespec for the top of the Playback tab
    /// </summary>
    PlaylistDescription = 0b_0000_0001,  // 1
    /// <summary>
    /// Text describing the current track (if any)
    /// </summary>
    CurrentTrackDescription = 0b_0000_0010,  // 2
    /// <summary>
    /// Current track status visual feedback (e.g. colours etc.)
    /// </summary>
    CurrentTrackStatusVisuals = 0b_0000_0100,  // 4
    /// <summary>
    /// Play, pause and stop button enabled state and visual appearance
    /// </summary>
    TransportButtons = 0b_0000_1000,
    /// <summary>
    /// Playback progress bar, time played and time remaining
    /// </summary>
    PlaybackProgress = 0b_0001_0000,  // 16
    /// <summary>
    /// Effective pitch, tempo, pan and volume settings
    /// </summary>
    EffectiveSettings = 0b_0010_0000,  // 32
    /// <summary>
    /// Previous and next track descriptions and scroll button enabled states
    /// </summary>
    AdjacentTracks = 0b_0100_0000,  // 64
    /// <summary>
    /// Like PlaybackProgress, but omitting the position slider. Used for manual scrubbing.
    /// </summary>
    PositionRemainingOnly = 0b_1000_0000,  // 128
    /// <summary>
    /// All of the elements in the Playback User Interface
    /// </summary>
    FullPlaybackUI = PlaylistDescription | CurrentTrackDescription | CurrentTrackStatusVisuals
                   | TransportButtons | PlaybackProgress | EffectiveSettings | AdjacentTracks
}
