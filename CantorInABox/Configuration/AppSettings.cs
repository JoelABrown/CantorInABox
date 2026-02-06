namespace Mooseware.CantorInABox.Configuration;

/// <summary>
/// Application configuration settings (read-only at application start) for configuring the application
/// The settings are loaded from appsettings.json in a section called "ApplicationSettings".
/// </summary>
public class AppSettings
{
    /// <summary>
    /// The list of prayerbooks for which track page references are recorded
    /// </summary>
    public string[] BookNames { get; set; } = [];

    /// <summary>
    /// The lower end of the legal range for pitch bending (in semi-tones)
    /// </summary>
    public int PitchFloor { get; set; } = -12;

    /// <summary>
    /// The upper end of the legal range for pitch bending (in semi-tones)
    /// </summary>
    public int PitchCeiling { get; set; } = 12;

    /// <summary>
    /// The left-most end of the legal range for panning
    /// </summary>
    public float PanFloor { get; set; } = -1.0f;

    /// <summary>
    /// The right-most end of th legal range for panning
    /// </summary>
    public float PanCeiling { get; set; } = 1.0f;

    public float PanDefault { get; set; } = 0.5f;

    public float PanIncrement { get; set; } = 0.1f;

    /// <summary>
    /// The lowest end of the legal range for tempo as an integer percentage (% * 100) of original
    /// </summary>
    public double TempoFloor { get; set; } = 0.1;

    /// <summary>
    /// The highest end of the legal range for tempo as an integer percentage (% * 100) of original
    /// </summary>
    public double TempoCeiling { get; set; } = 200.0;

    /// <summary>
    /// The bottom of the legal volume range as an integer percentage (% * 100) of original
    /// </summary>
    public float VolumeFloor { get; set; } = 0.0f;

    /// <summary>
    /// The top of the legal volume range as an integer percentage (% * 100) of original
    /// </summary>
    public float VolumeCeiling { get; set; } = 100.0f;

    public float VolumeDefault { get; set; } = 70.0f;

    public float VolumeIncrement { get; set; } = 5.0f;

    public float PanVolumeSwap { get; set; } = -1.0f;

    /// <summary>
    /// The base of the URL for the HTTP Web API
    /// </summary>
    public string WebApiUrlRoot { get; set; } = "0.0.0.0";

    /// <summary>
    /// The port number of the URL for the HTTP Web API
    /// </summary>
    public string WebApiUrlPort { get; set; } = "80";
}
