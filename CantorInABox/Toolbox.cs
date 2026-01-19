using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using NAudio.Wave;

namespace Mooseware.CantorInABox;
public static class Toolbox
{
    [DllImport("shell32.dll")]
    private static extern Int32 SHGetPathFromIDListW(UIntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath);

    /// <summary>
    /// Get the relative path from one given path to another given path.
    /// </summary>
    /// <param name="fromPath">The starting point from which the relative path is built.</param>
    /// <param name="toPath">The target of ther relative path.</param>
    /// <returns>The relative path specification.</returns>
    public static string GetRelativePath(string absolutePath, string relativeTo)
    {
        string[] absoluteDirectories = absolutePath.Split('\\');
        string[] relativeDirectories = relativeTo.Split('\\');
        //Get the shortest of the two paths            
        int length = absoluteDirectories.Length < relativeDirectories.Length ? absoluteDirectories.Length : relativeDirectories.Length;
        //Use to determine where in the loop we exited            
        int lastCommonRoot = -1;
        int index;
        //Find common root            
        for (index = 0; index < length; index++)
            if (absoluteDirectories[index] == relativeDirectories[index])
                lastCommonRoot = index;
            else
                break;
        //If we didn't find a common prefix then throw            
        if (lastCommonRoot == -1)
            throw new ArgumentException("Paths do not have a common base");
        //Build up the relative path            
        StringBuilder relativePath = new();
        //Add on the ..            
        for (index = lastCommonRoot + 1; index < absoluteDirectories.Length; index++)
            if (absoluteDirectories[index].Length > 0)
                relativePath.Append("..\\");
        //Add on the folders            
        for (index = lastCommonRoot + 1; index < relativeDirectories.Length - 1; index++)
            relativePath.Append(relativeDirectories[index] + "\\");
        relativePath.Append(relativeDirectories[^1]);
        return relativePath.ToString();
    }

    /// <summary>
    /// Gets the duration of an audio file by counting sample frames
    /// </summary>
    /// <param name="mediaFilename">Filespec of the audio file to measure</param>
    /// <returns>Duration in seconds</returns>
    public static double GetMediaDuration(string mediaFilename)
    {
        double duration = 0.0;
        try
        {
            if (File.Exists(mediaFilename))
            {
                // Get the duration of an MP3 file by counting sample frames...
                string sExtn = Path.GetExtension(mediaFilename).ToLower();
                if (sExtn == ".mp3")
                {
                    using FileStream fs = File.OpenRead(mediaFilename);
                    Mp3Frame frame = Mp3Frame.LoadFromStream(fs);
                    while (frame != null)
                    {
                        duration += (double)frame.SampleCount / (double)frame.SampleRate;
                        frame = Mp3Frame.LoadFromStream(fs);
                    }
                }
                else if (sExtn == ".wav")
                {
                    duration = WaveSoundInfo.GetSoundLength(mediaFilename);
                }
                // TODO: Add WMA support to the GetMediaDuration method.
            }

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());
            throw;
        }
        return duration;
    }
}