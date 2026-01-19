namespace Mooseware.CantorInABox.ViewModels;

/// <summary>
/// The information returned (in JSON format) when the Status API is called
/// </summary>
public class StatusApiViewModel
{
    private string _transport = AudioPlayback.PlaybackMode.Unloaded.ToString();

    /// <summary>
    /// The page number from the selected Playlist book for the current track
    /// </summary>
    public string PageNumber { get; set; } = string.Empty;
    /// <summary>
    /// The current track number from the playlist in n of m format
    /// </summary>
    public string TrackNumber { get; set; } = string.Empty;
    /// <summary>
    /// The current state of the playback transport where "Stopped" is remapped as "Cued"
    /// </summary>
    public string Transport
    {
        get
        {
            return _transport switch
            {
                "Stopped" => "Cued",
                _ => _transport.ToString(),
            };
        }
        set => _transport = value;
    }

    /// <summary>
    /// Creates a new StatusApiViewModel
    /// </summary>
    public StatusApiViewModel()
    {

    }

    /// <summary>
    /// Creates a new StatusApiViewModel
    /// </summary>
    /// <param name="pageNumber">The value to use for PageNumber</param>
    /// <param name="trackNumber">The value to use for TrackNumber</param>
    /// <param name="transport">The current AudioPlayback.PlaybackMode value. Used to set Transport</param>
    public StatusApiViewModel(string pageNumber, string trackNumber, AudioPlayback.PlaybackMode transport)
    {
        PageNumber = pageNumber;
        TrackNumber = trackNumber;
        Transport = transport.ToString();
    }

    /// <summary>
    /// Create a new StatusApiViewModel as a copy of a provided instance
    /// </summary>
    /// <param name="model">The nullable instance of StatusApiViewModel to copy</param>
    public StatusApiViewModel(StatusApiViewModel? model)
    {
        if (model is not null)
        {
            PageNumber = model.PageNumber;
            TrackNumber = model.TrackNumber;
            _transport = model.Transport;
        }
        else
        {
            PageNumber = string.Empty;
            TrackNumber = string.Empty;
            _transport = AudioPlayback.PlaybackMode.Unloaded.ToString();
        }
    }
}
