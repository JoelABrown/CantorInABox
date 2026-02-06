namespace Mooseware.CantorInABox.ViewModels;

/// <summary>
/// Utility functions used by various view model classes
/// </summary>
internal static class ViewModelUtilities
{
    /// <summary>
    /// Compose a description of the pitch playback setting value
    /// </summary>
    /// <param name="pitch">The value of the pitch setting</param>
    /// <returns>A description of the setting</returns>
    internal static string ComposePitchDescription(int? pitch)
    {
        string result = "Original";
        if (pitch is not null)
        {
            if (pitch < 0)
            {
                result = pitch.ToString() + " semitone" + ((pitch == -1) ? string.Empty : "s");
            }
            else if (pitch > 0)
            {
                result = "+" + pitch.ToString() + " semitone" + ((pitch == 1) ? string.Empty : "s");
            }
        }
        return result;
    }

    /// <summary>
    /// Compose a description of the tempo playback setting value
    /// </summary>
    /// <param name="tempo">The value of the tempo setting</param>
    /// <returns>A description of the setting</returns>
    internal static string ComposeTempoDescription(double? tempo)
    {
        string result = "Original";
        if (tempo is not null)
        {
            if (Math.Abs((double)(tempo - 100.0)) > 0.1)
            {
                result = Math.Round((double)tempo, 0).ToString() + "%";
            }
        }
        return result;
    }

    /// <summary>
    /// Compose a description of the pan playback setting value
    /// </summary>
    /// <param name="pan">The value of the pitch setting</param>
    /// <returns>A description of the setting</returns>
    internal static string ComposePanDescription(double? pan)
    {
        string result = "50/50";
        if (pan is not null)
        {
            int guitar = (int)Math.Round((double)(((pan + 1.0) / 2.0) * 100.0), 0);
            int voice = 100 - guitar;
            if (guitar == 100)
            {
                result = "Guitar Only";
            }
            else if (voice == 100)
            {
                result = "Voice Only";
            }
            else
            {
                result = $"{voice}/{guitar}";
            }
        }
        return result;
    }

    /// <summary>
    /// Compose a description of the volume playback setting value
    /// </summary>
    /// <param name="volume">The value of the pitch setting</param>
    /// <returns>A description of the setting</returns>
    internal static string ComposeVolumeDescription(double? volume)
    {
        string result = "(unknown)";
        if (volume is not null)
        {
            int volumeAsInt = (int)Math.Round((double)(volume), 0);
            if (volumeAsInt == 0)
            {
                result = "Mute";
            }
            else if (volumeAsInt == 100)
            {
                result = "Full";
            }
            else
            {
                result = $"{volumeAsInt}%";
            }
        }
        return result;
    }
}
