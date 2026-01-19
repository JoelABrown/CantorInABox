namespace Mooseware.CantorInABox;

/// <summary>
/// Web API Message Container
/// </summary>
public enum ApiMessageVerb
{
    /// <summary>
    /// No API message defined (yet)
    /// </summary>
    None = 0,
    /// <summary>
    /// Transport controls (e.g. play|pause|stop)
    /// </summary>
    Transport,
    /// <summary>
    /// Track selection controls (e.g. previous|next|from-the-top)
    /// </summary>
    Playlist,
    /// <summary>
    /// Volume contol (overall) (e.g. louder|quieter|reset)
    /// </summary>
    Volume,
    /// <summary>
    /// Balance control (L/R, voice vs instrument) (e.g. more-voice | more-instrument | reset)
    /// </summary>
    Pan,
    /// <summary>
    /// Tempo control (slower|faster|reset)
    /// </summary>
    Tempo,
    /// <summary>
    /// Pitch control (transpose-up|transpose-down|reset)
    /// </summary>
    Pitch
}

/// <summary>
/// An API message from a remote controlling app
/// </summary>
public class ApiMessage
{
    /// <summary>
    /// The action type of the message
    /// </summary>
    public ApiMessageVerb Verb { get; set; } = ApiMessageVerb.None;
    /// <summary>
    /// Particular details of what has been requested within the context of the type of message
    /// </summary>
    public string Parameters { get; set; } = string.Empty;

    // String constants that define the acceptable query parameters for the various verbs
    // ----------------------------------------------------------------------------------

    public const string TransportPlay = "play";
    public const string TransportPause = "pause";
    public const string TransportStop = "stop";
    public const string PlaylistPrevious = "previous";
    public const string PlaylistNext = "next";
    public const string PlaylistRestart = "from-the-top";
    public const string VolumeLouder = "louder";
    public const string VolumeQuieter = "quieter";
    public const string VolumeReset = "reset";
    public const string PanMoreVoice = "more-voice";
    public const string PanMoreInstrument = "more-instrument";
    public const string PanReset = "reset";
    public const string TempoSlower = "slower";
    public const string TempoFaster = "faster";
    public const string TempoReset = "reset";
    public const string PitchUp = "transpose-up";
    public const string PitchDown = "transpose-down";
    public const string PitchReset = "reset";

}