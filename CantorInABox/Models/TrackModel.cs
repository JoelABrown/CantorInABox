using Mooseware.CantorInABox.Configuration;
using System.Xml.Serialization;

namespace Mooseware.CantorInABox.Models;

/// <summary>
/// The model for an individual audio file in a playlist with settings specific to this track and playlist
/// </summary>
[Serializable]
[XmlType("Track")]
public class TrackModel
{
    /// <summary>
    /// The GUID which identifies the library from which the track comes
    /// </summary>
    public Guid LibraryKey { get; set; }
    /// <summary>
    /// The GUID which identifies the specific library entry for this track
    /// </summary>
    public Guid LibraryEntryKey { get; set; }
    /// <summary>
    /// Whether to use the playlist default pitch (false) or a track-specific value (true)
    /// </summary>
    public bool UsePitchOverride { get; set; } = false;
    /// <summary>
    /// The track-specific override pitch value to use (if the use override flag is true)
    /// </summary>
    public int PitchOverride { get; set; } = AudioPlayback.PitchDefault;
    /// <summary>
    /// Whether to use the playlist default tempo (false) or a track-specific value (true)
    /// </summary>
    public bool UseTempoOverride { get; set; } = false;
    /// <summary>
    /// The track-specific override tempo value to use (if the use override flag is true)
    /// </summary>
    public double TempoOverride { get; set; } = AudioPlayback.TempoDefault;
    /// <summary>
    /// Whether to use the playlist default pan (false) or a track-specific value (true)
    /// </summary>
    public bool UsePanOverride { get; set; } = false;
    /// <summary>
    /// The track-specific override tempo value to use (if the use override flag is true)
    /// </summary>
    public double PanOverride { get; set; }
    /// <summary>
    /// Whether to use the playlist default volume (false) or a track-specific value (true)
    /// </summary>
    public bool UseVolumeOverride { get; set; } = false;
    /// <summary>
    /// The track-specific override volume value to use (if the use override flag is true)
    /// </summary>
    public double VolumeOverride { get; set; }

    /// <summary>
    /// Application settings used to configure the app at startup
    /// </summary>
    private AppSettings? _appSettings;

    public TrackModel()
    {
            
    }

    public void SetAppSettings(AppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    /// <summary>
    /// Reset the override value to the model default value
    /// </summary>
    public void ResetOverridePitch()
    {
        PitchOverride = AudioPlayback.PitchDefault;
    }

    /// <summary>
    /// Reset the override value to the model default value
    /// </summary>
    public void ResetOverridePan()
    {
        PanOverride = _appSettings!.PanDefault;
    }

    /// <summary>
    /// Reset the override value to the model default value
    /// </summary>
    public void ResetOverrideTempo()
    {
        TempoOverride = AudioPlayback.TempoDefault;
    }

    /// <summary>
    /// Reset the override value to the model default value
    /// </summary>
    public void ResetOverrideVolume()
    {
        VolumeOverride = _appSettings!.VolumeDefault / 100.0;
    }
}
