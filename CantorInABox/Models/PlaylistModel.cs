using System.IO;
using System.Xml.Serialization;

namespace Mooseware.CantorInABox.Models;

/// <summary>
/// A collection of audio tracks to be performed in turn
/// </summary>
[Serializable]
[XmlRoot("Playlist")]
public class PlaylistModel
{
    /// <summary>
    /// The full path and file spec of the playlist file (.cibplf)
    /// </summary>
    public string Filespec { get; set; } = string.Empty;
    /// <summary>
    /// Short descriptive name of the playlist
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Longer description of the playlist with details re: purpose, use, etc.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// The index of the default prayer book to use with this playlist (1,2,3. 0=unknown)
    /// </summary>
    public int DefaultBook { get; set; } = 0;
    /// <summary>
    /// The default value for Pitch to use with all tracks in the playlist
    /// </summary>
    public int PitchDefault { get; set; } = AudioPlayback.PitchDefault;
    /// <summary>
    /// The default value for Tempo to use with all tracks in the playlist
    /// </summary>
    public double TempoDefault { get; set; } = AudioPlayback.TempoDefault;
    /// <summary>
    /// The default value for Pan to use with all tracks in the playlist
    /// </summary>
    public double PanDefault { get; set; } = AudioPlayback.PanDefault;
    /// <summary>
    /// The default value for Volume to use with all tracks in the playlist
    /// </summary>
    public double VolumeDefault { get; set; } = AudioPlayback.VolumeDefault;
    /// <summary>
    /// The list of audio tracks which make up the playlist
    /// </summary>
    public List<TrackModel> Tracks { get; set; } = [];

    public PlaylistModel()
    {
        
    }

    /// <summary>
    /// Persists the playlist to a .cibplf file
    /// </summary>
    /// <param name="playlist">The model of the playlist to be persisted</param>
    /// <param name="playlistFilespec">The full path and file spec of the playlist (.cibplf) file</param>
    public static void SavePlaylistFile(PlaylistModel playlist, string playlistFilespec)
    {
        const string plfFileExt = ".cibplf";

        // Make sure the playlist knows where it's being saved
        playlist.Filespec = playlistFilespec.Trim();
        // Enforce the .cibplf file extension
        if (!playlist.Filespec.ToLower().EndsWith(plfFileExt))
        {
            playlist.Filespec += plfFileExt;
        }

        try
        {
            // Now persist the Library data structure.
            XmlSerializer xs = new(typeof(PlaylistModel));
            TextWriter xmlTextWriter = new StreamWriter(playlist.Filespec, false);  // Overwrite any existing file.
            xs.Serialize(xmlTextWriter, playlist);
            xmlTextWriter.Flush();
            xmlTextWriter.Close();
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Loads a playlist from a given .cibplf file
    /// </summary>
    /// <param name="filespec">The full path and file spec of the playlist to load</param>
    /// <returns>The model of the loaded playlist</returns>
    public static PlaylistModel? LoadPlaylistFile(string filespec)
    {
        PlaylistModel? result = null;
        try
        {
            if (File.Exists(filespec))
            {
                XmlSerializer xs = new(typeof(PlaylistModel));
                using var fileStream = new FileStream(filespec, FileMode.Open);
                result = (PlaylistModel)xs.Deserialize(fileStream)!;

                // Make sure the Playlist object knows where it actually came from...
                result.Filespec = filespec;
            }
        }
        catch (Exception)
        {
            throw;
        }
        return result;
    }

    /// <summary>
    /// Resets the default pitch to the default value defined by the AudioPlayback class
    /// </summary>
    public void ResetDefaultPitch()
    {
        PitchDefault = AudioPlayback.PitchDefault;
    }

    /// <summary>
    /// Resets the default pan to the default value defined by the AudioPlayback class
    /// </summary>
    public void ResetDefaultPan()
    {
        PanDefault = AudioPlayback.PanDefault;
    }

    /// <summary>
    /// Resets the default tempo to the default value defined by the AudioPlayback class
    /// </summary>
    public void ResetDefaultTempo()
    {
        TempoDefault = AudioPlayback.TempoDefault;
    }

    /// <summary>
    /// Resets the default volume to the default value defined by the AudioPlayback class
    /// </summary>
    public void ResetDefaultVolume()
    {
        VolumeDefault = AudioPlayback.VolumeDefault;
    }
}
