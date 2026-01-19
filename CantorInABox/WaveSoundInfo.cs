using System.Runtime.InteropServices;
using System.Text;

namespace Mooseware.CantorInABox;

/// <summary>
/// Wrapper for selected functions in WINMM.DLL
/// </summary>
internal static class WaveSoundInfo
{

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern uint mciSendString(
        string command,
        StringBuilder? returnValue,
        int returnLength,
        IntPtr winHandle);

    public static double GetSoundLength(string fileName)
    {
        double dSeconds = 0.0;
        try
        {
            StringBuilder lengthBuf = new(32);
            
            var gar1 = mciSendString(string.Format("open \"{0}\" type waveaudio alias wave", fileName), null, 0, IntPtr.Zero);
            var gar2 = mciSendString("status wave length", lengthBuf, lengthBuf.Capacity, IntPtr.Zero);
            var gar3 = mciSendString("close wave", null, 0, IntPtr.Zero);

            if (int.TryParse(lengthBuf.ToString(), out int length))
            {
                dSeconds = (double)length / 1000.0;
            }
        }
        catch (Exception ex)
        {   // For debugging only...
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
        return dSeconds;
    }
}
