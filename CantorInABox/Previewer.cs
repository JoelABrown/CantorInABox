using Mooseware.CantorInABox.ViewModels;
using NAudio.Wave;
using System.ComponentModel;
using System.IO;

namespace Mooseware.CantorInABox;

/// <summary>
/// Simple audio file player used for previewing audio tracks (either a library entry or a playlist track) 
/// without any playback setting manipulations
/// </summary>
public class Previewer : INotifyPropertyChanged, IDisposable
{
    private WaveOut? _waveOutDevice;
    private WaveStream? _mainOutputStream;
    private WaveChannel32? _volumeStream;
    private bool _disposed = false;
    private enum TrackMode
    {
        None,
        LibraryEntry,
        PlaylistTrack
    }
    private TrackMode _mode;
    private LibraryEntryViewModel? _libraryEntry;
    private TrackViewModel? _playlistTrack;

    public Previewer()
    {
        _mode = TrackMode.None;
    }

    public bool Playing
    {
        get
        {
            bool result = false;
            if (_waveOutDevice is not null
                && ((_mode==TrackMode.LibraryEntry && _libraryEntry is not null)
                || (_mode==TrackMode.PlaylistTrack && _playlistTrack is not null)
                ))
            {
                result = (_waveOutDevice.PlaybackState == PlaybackState.Playing);
            }
            return result;
        }
    }

    public void Stop()
    {
        try
        {
            // Stop previewing any current track...
            if (_waveOutDevice != null && _waveOutDevice.PlaybackState == PlaybackState.Playing)
            {
                DisposeAudioResources();
            }
            switch (_mode)
            {
                case TrackMode.None:
                    // Shouldn't happen
                    break;
                case TrackMode.LibraryEntry:
                    _libraryEntry!.Previewing = false;
                    _libraryEntry = null;
                    _mode = TrackMode.None;
                    break;
                case TrackMode.PlaylistTrack:
                    _playlistTrack!.Previewing = false;
                    _playlistTrack = null;
                    _mode = TrackMode.None;
                    break;
                default:
                    break;
            }
            RefreshPreviewProperties();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());
            throw;
        }
    }

    /// <summary>
    /// Previews a given Libary Entry given its view model.
    /// If another file is already playing then stop it first. 
    /// If the same file is already playing then just stop it (and quit).
    /// </summary>
    /// <param name="entryToPreview">The entry to be previewed</param>
    public void PreviewTrack(LibraryEntryViewModel? entryToPreview)
    {
        // Were we given a file to start playing?
        bool startPlaying = (entryToPreview is not null && entryToPreview.Filespec is not null && entryToPreview.Filespec.Length > 0);
        try
        {
            // Stop previewing the current media (if any, either way)
            if (this.Playing)
            {
                if (entryToPreview is not null)
                {
                    // If this call is to preview the current media then just stop it and move on.
                    startPlaying = (_libraryEntry is null 
                        || _libraryEntry.Filespec is null
                        || (_libraryEntry!.Filespec != entryToPreview.Filespec));
                }
                // Stop whatever is playing now.
                this.Stop();
            }

            if (startPlaying)
            {
                if (entryToPreview is not null && entryToPreview.Filespec is not null)
                {
                    PreviewAudioFile(entryToPreview.Filespec);
                
                entryToPreview.Previewing = true;
                _mode = TrackMode.LibraryEntry;
                _libraryEntry = entryToPreview;
                }
            }
            RefreshPreviewProperties();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());
            throw;
        }
    }

    private void PreviewAudioFile(string audioFilespec)
    {
        if (audioFilespec.Length == 0 || !File.Exists(audioFilespec))
        { 
            return; 
        }

        // Were we given a file to start playing?
        try
        {
            _waveOutDevice = new WaveOut();
            WaveChannel32 inputStream;
            string sExtension = Path.GetExtension(audioFilespec).ToLower();
            if (sExtension == ".mp3")
            {
                WaveStream mp3Reader = new Mp3FileReader(audioFilespec);
                inputStream = new WaveChannel32(mp3Reader)
                {
                    PadWithZeroes = false
                };
            }
            else if (sExtension == ".wav")
            {
                WaveStream wavReader = new WaveFileReader(audioFilespec);
                inputStream = new WaveChannel32(wavReader)
                {
                    PadWithZeroes = false
                };
            }
            // TODO: Add WMA support to the PreviewTrack method.
            //else if (sExtension == ".wma")
            //{
            //}
            else
            {
                throw new InvalidOperationException("Unable to preview files with the " + sExtension + " extension.");
            }
            _volumeStream = inputStream;
            _mainOutputStream = inputStream;
            _waveOutDevice.Init(_mainOutputStream);
            _waveOutDevice.Play();

            this._waveOutDevice.PlaybackStopped += OnPlaybackStopped;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());
            throw;
        }

    }

    /// <summary>
    /// Previews a given Playlist Track given its view model.
    /// If another file is already playing then stop it first. 
    /// If the same file is already playing then just stop it (and quit).
    /// </summary>
    /// <param name="trackToPreview">The track to be previewed</param>
    public void PreviewTrack(TrackViewModel? trackToPreview)
    {
        // Were we given a file to start playing?
        bool startPlaying = (trackToPreview is not null && trackToPreview.Filespec is not null && trackToPreview.Filespec.Length > 0);
        try
        {
            // Stop previewing the current media (if any, either way)
            if (this.Playing)
            {
                if (trackToPreview is not null)
                {
                    // If this call is to preview the current media then just stop it and move on.
                    startPlaying = (_playlistTrack is null
                        || _playlistTrack.Filespec is null
                        || (_playlistTrack!.Filespec != trackToPreview.Filespec));
                }
                // Stop whatever is playing now.
                this.Stop();
            }

            if (startPlaying)
            {
                if (trackToPreview is not null && trackToPreview.Filespec is not null)
                {
                    PreviewAudioFile(trackToPreview.Filespec);
                    trackToPreview.Previewing = true;
                    _mode = TrackMode.PlaylistTrack;
                    _playlistTrack = trackToPreview;
                }
            }
            RefreshPreviewProperties();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());
            throw;
        }
    }

    private void OnPlaybackStopped(object? sender, EventArgs e)
    {
        // Clean up the audio stream resources...
        DisposeAudioResources();
        RefreshPreviewProperties();

        if (_mode == TrackMode.LibraryEntry)
        {
            if (_libraryEntry is not null)
            {
                _libraryEntry.Previewing = false;
            }
        }
        else if (_mode == TrackMode.PlaylistTrack)
        {
            if (_playlistTrack is not null)
            {
                _playlistTrack.Previewing = false;
            }
        }
    }

    public void RefreshPreviewProperties()
    {
        try
        {
            OnPropertyChanged("Playing");
            OnPropertyChanged("Paused");
            OnPropertyChanged("Message");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());
            throw;
        }
    }

    private void DisposeAudioResources()
    {
        try
        {
            _waveOutDevice?.Stop();
            if (_mainOutputStream is not null)
            {
                // this one really closes the file and ACM conversion
                if (_volumeStream is not null)
                {
                    _volumeStream.Close();
                    _volumeStream = null;
                }
                // this one does the metering stream
                _mainOutputStream.Close();
                _mainOutputStream = null;
            }
            if (_waveOutDevice != null)
            {
                _waveOutDevice.Dispose();
                _waveOutDevice = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());
            throw;
        }
    }

    protected void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }

    protected void OnPropertyChanged(string sPropertyName)
    {
        OnPropertyChanged(new PropertyChangedEventArgs(sPropertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;


    // Implement IDisposable.
    public void Dispose()
    {
        Dispose(true);
        // Take ourselves off the Finalization queue to prevent finalization code for this object from executing a second time...
        GC.SuppressFinalize(this);
    }

    // Internally referenced method for disposing of unmanaged transf32.dll resources.
    // This can be called deterministically.
    protected virtual void Dispose(bool bDisposing)
    {
        try
        {   // Check to see if Dispose has already been called...
            if (!_disposed)
            {
                DisposeAudioResources();
            }
        }
        catch (Exception ex)
        {   // For debugging...
            System.Diagnostics.Debug.WriteLine(ex.ToString());
            throw;
        }
        finally
        {
            _disposed = true;
        }
    }
}

