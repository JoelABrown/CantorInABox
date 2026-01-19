using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mooseware.CantorInABox.Models;
using Mooseware.CantorInABox.ViewModels;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Mooseware.CantorInABox;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // TODO: Test the snot out of stuff once the general cleanup is completed to make sure nothing is (newly) broken
    // TODO: Consider making pan HTTP APIs do an offset trick with pan and volume (where room exists) so more voice is not less guitar
    //       Note that this might imply a default volume of less than 100% (e.g. 70%) to give headroom for more vol as there is less guitar.
    // TODO: Sort out bug with playlist drag and drop not reflecting right away, especially on the Playlist
    //       or at least being sorted out by the time the playlist tab is activated.
    // TODO: Consider saving pointer to current track (for playback) on exit & restoring (see: trackindex)
    // TODO: Fix the editability of the new track after adding to the library bug
    // TODO: Consider context-sensitive starup tab: Libraries if none loaded, Playlist if none loaded, Playback if playlist IS loaded
    // TODO: Build a setup project for deployment
    // TODO: Download and install VS2026 and upgrade to .NET 10

    /// <summary>
    /// Host for listening to HTTP API calls (for example, from a Companion-powered Stream Deck)
    /// </summary>
    private readonly IHost _host;

    /// <summary>
    /// The queue of messages being received via the HTTP API
    /// </summary>
    private readonly ConcurrentQueue<ApiMessage> _messageQueue;

    /// <summary>
    /// Information to be recorded in a temporary (shared) file that passes current state information back via the HTTP API
    /// </summary>
    private StatusApiViewModel _lastRecordedStatusApiViewModel = new();

    /// <summary>
    /// Timer for ticking through a loop to look for queued messages
    /// </summary>
    private readonly DispatcherTimer _heartbeat;

    /// <summary>
    /// The main/root view model for interacting with the user interface
    /// </summary>
    private readonly PrimeViewModel _primeViewModel = new();

    /// <summary>
    /// The simple sound file player used for playing a preview of tracks on the Library and Playlist tabs
    /// </summary>
    private readonly Previewer _preview = new();

    /// <summary>
    /// Base constructor for the MainWindow
    /// </summary>
    /// <param name="msgQueue">ConcurrentQueue<ApiMessage> for the HTTP API injected via DI</param>
    public MainWindow(ConcurrentQueue<ApiMessage> msgQueue)
    {
        InitializeComponent();

        // Hook up the data context...
        this.DataContext = _primeViewModel;

        // Request that power plan settings be ignored...
        Utilities.SetAlwaysOnPowerHandling();

        // Set the local reference to the (singleton) ConcurrentQueue for the UI thread.
        _messageQueue = msgQueue;

        // Set up the heartbeat time that watches for incoming HTTP requests
        _heartbeat = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _heartbeat.Tick += Heartbeat_Tick;

        // Restore any settings from the previous session and load application settings...
        Properties.Settings.Default.Reload();

        // Set the limits on playback setting sliders based on application settings.
        _primeViewModel.PitchFloor = Properties.Settings.Default.PitchFloor;
        _primeViewModel.PitchCeiling = Properties.Settings.Default.PitchCeiling;
        _primeViewModel.PanFloor = Properties.Settings.Default.PanFloor;
        _primeViewModel.PanCeiling = Properties.Settings.Default.PanCeiling;
        _primeViewModel.TempoFloor = Properties.Settings.Default.TempoFloor;
        _primeViewModel.TempoCeiling = Properties.Settings.Default.TempoCeiling;
        _primeViewModel.VolumeFloor = Properties.Settings.Default.VolumeFloor;
        _primeViewModel.VolumeCeiling = Properties.Settings.Default.VolumeCeiling;

        //Get the base URL for .UseUrls() from the App.config settings file
        string webApiRootUrl = Properties.Settings.Default.WebApiUrlRoot;
        string webApiUrlPort = Properties.Settings.Default.WebApiUrlPort;
        
        // Create the background Web API server running within this WPF app...
        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls(webApiRootUrl + ":" + webApiUrlPort);
            });
        
        // Pass a reference to the ConcurrentQueue for the web server thread so that web APIs can add to the queue
        builder.ConfigureServices((hostContext, services) =>
        {
            services.AddSingleton<ConcurrentQueue<ApiMessage>>(_messageQueue);
        });
        _host = builder.Build();
        _host.Start();
    }
    
    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        _heartbeat.Stop();

        // See https://stackoverflow.com/questions/67674514/hosting-an-asp-web-api-inside-a-wpf-app-wont-stop-gracefully
        // for why this particular sequence of events are required to allow the _host to be shut down gracefully when
        // the WPF application is shut down.
        e.Cancel = true;
        await _host.StopAsync();
        _host.Dispose();

        try
        {
            // Save settings for the next visit...
            if (_primeViewModel.CurrentLibrary is not null)
            {
                Properties.Settings.Default.LastLibraryFile = _primeViewModel.CurrentLibrary.Filespec;
            }
            System.Collections.Specialized.StringCollection knowLibFiles = [];
            foreach (var item in _primeViewModel.KnownLibraries)
            {
                if (item.IsValid)
                {
                    knowLibFiles.Add(item.Filespec);
                }
            }
            Properties.Settings.Default.KnownLibraries = knowLibFiles;

            // Save the last open playlist file to settings as the app is shutting down
            if (_primeViewModel.CurrentPlaylist is not null)
            {
                Properties.Settings.Default.LastPlaylistFile = _primeViewModel.CurrentPlaylist.Filespec;
            }

            // Persist the settings updates
            Properties.Settings.Default.Save();

            // Dispose of the previewer. (the full playback device will handle itself in the GC)
            //_playback?.Dispose();
            _preview?.Dispose();

            Utilities.ReturnToNormalPowerHandling();
        }
        catch (Exception ex)
        {
            App.WriteLog(ex);
        }

        this.Closing -= Window_Closing;
        Close();
    }

    private void Heartbeat_Tick(object? sender, EventArgs e)
    {
        // Check the current status (to be reported via the HTTP API) and update it if necessary.
        string pageValue = string.Empty;
        string trackValue = string.Empty;
        if (_primeViewModel.CurrentPlaylistTrack is not null && _primeViewModel.CurrentPlaylist is not null)
        {
            pageValue = _primeViewModel.CurrentPlaylistTrack.ShownPages;
            trackValue = (_primeViewModel.CurrentTrackIndex + 1).ToString()
                       + " of " + _primeViewModel.CurrentPlaylist.Tracks.Count.ToString();
        }
        AudioPlayback.PlaybackMode transportValue = _primeViewModel.TransportMode;
        // Are there any differences?
        if (pageValue != _lastRecordedStatusApiViewModel.PageNumber
            || trackValue != _lastRecordedStatusApiViewModel.TrackNumber
            || (_lastRecordedStatusApiViewModel.Transport == "Cued" && transportValue != AudioPlayback.PlaybackMode.Stopped)
            || (_lastRecordedStatusApiViewModel.Transport != "Cued" 
                && _lastRecordedStatusApiViewModel.Transport != transportValue.ToString()))
        {
            var newStatus = new StatusApiViewModel(pageValue, trackValue, transportValue);
            WriteStatusApiDetailsFile(newStatus);
            _lastRecordedStatusApiViewModel = newStatus;
        }

        // Look for API messages arrived since the last cycle...
        while (!_messageQueue.IsEmpty)
        {
            if (_messageQueue.TryDequeue(out ApiMessage? queueItem))
            {
                if (queueItem is not null)
                {
                    HandleApiCommand(queueItem);
                }
            }
        }
    }

    /// <summary>
    /// Process the commands received through the REST API 
    /// </summary>
    /// <param name="message">The API message details which specify what command to execute</param>
    private void HandleApiCommand(ApiMessage message)
    {
        string? flashMessage;

        // Make a message to flash to the UI so the operator knows that things are happening in the background
        flashMessage = message.Verb.ToString() + ": (" + message.Parameters + ")";

        // Do the actions requested by the API...
        switch (message.Verb)
        {
            case ApiMessageVerb.None:
                break;
            case ApiMessageVerb.Transport:
                switch (message.Parameters)
                {
                    case ApiMessage.TransportPlay:
                        flashMessage = "Play current track";
                        if (_primeViewModel.CurrentPlaylistTrack is not null
                            && _primeViewModel.CanPlay)
                        {
                            _primeViewModel.CurrentPlaylistTrack.PlayCurrentTrack();
                        }
                        else
                        {
                            flashMessage += " (unavailable)";
                        }
                        break;
                    case ApiMessage.TransportPause:
                        flashMessage = "Pause playback";
                        if (_primeViewModel.CurrentPlaylistTrack is not null
                            && _primeViewModel.CanPause)
                        {
                            _primeViewModel.CurrentPlaylistTrack.PauseCurrentTrack();
                        }
                        else
                        {
                            flashMessage += " (unavailable)";
                        }
                        break;
                    case ApiMessage.TransportStop:
                        flashMessage = "Stop playback";
                        if (_primeViewModel.CurrentPlaylistTrack is not null
                            && _primeViewModel.CanStop)
                        {
                            _primeViewModel.CurrentPlaylistTrack.StopCurrentTrack();
                        }
                        else
                        {
                            flashMessage = " (unavailable)";
                        }
                        break;
                    default:
                        break;
                }
                break;
            case ApiMessageVerb.Playlist:
                switch (message.Parameters)
                {
                    case ApiMessage.PlaylistPrevious:
                        flashMessage = "Move to previous track";
                        if (_primeViewModel.CurrentPlaylistTrack is not null
                                                    && _primeViewModel.CanPreviousTrack)
                        {
                            _primeViewModel.PreviousPlaybackTrack();
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break;
                    case ApiMessage.PlaylistNext:
                        flashMessage = "Move to next track";
                        if (_primeViewModel.CurrentPlaylistTrack is not null
                                                    && _primeViewModel.CanNextTrack)
                        {
                            _primeViewModel.NextPlaybackTrack();
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break;
                    case ApiMessage.PlaylistRestart:
                        flashMessage = "Move to start of playlist";
                        if (_primeViewModel.CurrentPlaylist is not null && _primeViewModel.CurrentPlaylist.Tracks.Count > 1)
                        {
                            _primeViewModel.CurrentPlaylistTrack = _primeViewModel.CurrentPlaylist.Tracks[0];
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break;
                    default:
                        break;
                }
                break;
            case ApiMessageVerb.Volume:
                // TODO: Read the API volume increment from a settings file at startup
                double volumeIncrement = 5.0;
                switch (message.Parameters)
                {
                    case ApiMessage.VolumeQuieter:
                        flashMessage = "Volume: quieter";
                        if (_primeViewModel.CurrentPlaylistTrack is not null
                         && _primeViewModel.CurrentPlaylistTrack.EffectiveVolume > 0.0)
                        {
                            double currentVolume = _primeViewModel.CurrentPlaylistTrack.EffectiveVolume ?? 100.0;
                            double newVolume = Math.Max(0.0, currentVolume - volumeIncrement);
                            _primeViewModel.CurrentPlaylistTrack.VolumeOverride = newVolume;
                            // If the track is using the playlist default, then we need to turn the override on.
                            if (_primeViewModel.CurrentPlaylistTrack.UseVolumeOverride == false)
                            {
                                _primeViewModel.CurrentPlaylistTrack.UseVolumeOverride = true;
                            }
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break;
                    case ApiMessage.VolumeLouder:
                        flashMessage = "Volume: louder";
                        if (_primeViewModel.CurrentPlaylistTrack is not null
                         && _primeViewModel.CurrentPlaylistTrack.EffectiveVolume < 100.0)
                        {
                            double currentVolume = _primeViewModel.CurrentPlaylistTrack.EffectiveVolume ?? 100.0;
                            double newVolume = Math.Min(100.0, currentVolume + volumeIncrement);
                            _primeViewModel.CurrentPlaylistTrack.VolumeOverride = newVolume;
                            // If the track is using the playlist default, then we need to turn the override on.
                            if (_primeViewModel.CurrentPlaylistTrack.UseVolumeOverride == false)
                            {
                                _primeViewModel.CurrentPlaylistTrack.UseVolumeOverride = true;
                            }
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break;
                    case ApiMessage.VolumeReset:
                        flashMessage = "Volume: reset to default";
                        if (_primeViewModel.CurrentPlaylistTrack is not null)
                        {
                            _primeViewModel.CurrentPlaylistTrack.ResetOverrideVolume();
                            // If the track is using the playlist default, then we need to turn the override on.
                            if (_primeViewModel.CurrentPlaylistTrack.UseVolumeOverride == false)
                            {
                                _primeViewModel.CurrentPlaylistTrack.UseVolumeOverride = true;
                            }
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break;
                    default:
                        break;
                }
                break;
            case ApiMessageVerb.Pan:
                // TODO: Read the API pan increment from a settings file at startup
                double panIncrement = 0.1;
                switch (message.Parameters)
                {
                    case ApiMessage.PanMoreVoice:
                        flashMessage = "Pan: more voice";
                        if (_primeViewModel.CurrentPlaylistTrack is not null
                         && _primeViewModel.CurrentPlaylistTrack.EffectivePan > -1.0)
                        {
                            double currentPan = _primeViewModel.CurrentPlaylistTrack.EffectivePan ?? 0.0;
                            double newPan = Math.Max(-1.0, currentPan - panIncrement);
                            _primeViewModel.CurrentPlaylistTrack.PanOverride = newPan;
                            // If the track is using the playlist default, then we need to turn the override on.
                            if (_primeViewModel.CurrentPlaylistTrack.UsePanOverride == false)
                            {
                                _primeViewModel.CurrentPlaylistTrack.UsePanOverride = true;
                            }
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break; 
                    case ApiMessage.PanMoreInstrument:
                        flashMessage = "Pan: more instrument";
                        if (_primeViewModel.CurrentPlaylistTrack is not null
                         && _primeViewModel.CurrentPlaylistTrack.EffectivePan < 1.0)
                        {
                            double currentPan = _primeViewModel.CurrentPlaylistTrack.EffectivePan ?? 0.0;
                            double newPan = Math.Min(1.0, currentPan + panIncrement);
                            _primeViewModel.CurrentPlaylistTrack.PanOverride = newPan;
                            // If the track is using the playlist default, then we need to turn the override on.
                            if (_primeViewModel.CurrentPlaylistTrack.UsePanOverride == false)
                            {
                                _primeViewModel.CurrentPlaylistTrack.UsePanOverride = true;
                            }
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break;
                    case ApiMessage.PanReset:
                        flashMessage = "Pan: reset to default";
                        if (_primeViewModel.CurrentPlaylistTrack is not null)
                        {
                            _primeViewModel.CurrentPlaylistTrack.ResetOverridePan();
                            // If the track is using the playlist default, then we need to turn the override on.
                            if (_primeViewModel.CurrentPlaylistTrack.UsePanOverride == false)
                            {
                                _primeViewModel.CurrentPlaylistTrack.UsePanOverride = true;
                            }
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break;
                    default:
                        break;
                }
                break;
            case ApiMessageVerb.Tempo:
                // TODO: Read the API tempo increment from a settings file at startup
                double tempoIncrement = 3.0; 
                switch (message.Parameters)
                {
                    case ApiMessage.TempoSlower:
                        flashMessage = "Tempo: slower";
                        if (_primeViewModel.CurrentPlaylistTrack is not null
                         && _primeViewModel.CurrentPlaylistTrack.EffectiveTempo > 50.0)
                        {
                            double currentTempo = _primeViewModel.CurrentPlaylistTrack.EffectiveTempo ?? 0.0;
                            double newTempo = Math.Max(50.0, currentTempo - tempoIncrement);
                            _primeViewModel.CurrentPlaylistTrack.TempoOverride = newTempo;
                            // If the track is using the playlist default, then we need to turn the override on.
                            if (_primeViewModel.CurrentPlaylistTrack.UseTempoOverride == false)
                            {
                                _primeViewModel.CurrentPlaylistTrack.UseTempoOverride = true;
                            }
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break;
                    case ApiMessage.TempoFaster:
                        flashMessage = "Tempo: faster";
                        if (_primeViewModel.CurrentPlaylistTrack is not null
                         && _primeViewModel.CurrentPlaylistTrack.EffectiveTempo < 200.0)
                        {
                            double currentTempo = _primeViewModel.CurrentPlaylistTrack.EffectiveTempo ?? 0.0;
                            double newTempo = Math.Min(200.0, currentTempo + tempoIncrement);
                            _primeViewModel.CurrentPlaylistTrack.TempoOverride = newTempo;
                            // If the track is using the playlist default, then we need to turn the override on.
                            if (_primeViewModel.CurrentPlaylistTrack.UseTempoOverride == false)
                            {
                                _primeViewModel.CurrentPlaylistTrack.UseTempoOverride = true;
                            }
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break;
                    case ApiMessage.TempoReset:
                        flashMessage = "Tempo: reset to default";
                        if (_primeViewModel.CurrentPlaylistTrack is not null)
                        {
                            _primeViewModel.CurrentPlaylistTrack.ResetOverrideTempo();
                            // If the track is using the playlist default, then we need to turn the override on.
                            if (_primeViewModel.CurrentPlaylistTrack.UseTempoOverride == false)
                            {
                                _primeViewModel.CurrentPlaylistTrack.UseTempoOverride = true;
                            }
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break;
                    default:
                        break;
                }
                break;
            case ApiMessageVerb.Pitch:
                int pitchIncrement = 1;
                switch (message.Parameters)
                {
                    case ApiMessage.PitchDown:
                        flashMessage = "Pitch: transpose down";
                        if (_primeViewModel.CurrentPlaylistTrack is not null
                         && _primeViewModel.CurrentPlaylistTrack.EffectivePitch > -12)
                        {
                            int currentPitch = _primeViewModel.CurrentPlaylistTrack.EffectivePitch ?? 0;
                            int newPitch = Math.Max(-12, currentPitch - pitchIncrement);
                            _primeViewModel.CurrentPlaylistTrack.PitchOverride = newPitch;
                            // If the track is using the playlist default, then we need to turn the override on.
                            if (_primeViewModel.CurrentPlaylistTrack.UsePitchOverride == false)
                            {
                                _primeViewModel.CurrentPlaylistTrack.UsePitchOverride = true;
                            }
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break;
                    case ApiMessage.PitchUp:
                        flashMessage = "Pitch: transpose up";
                        if (_primeViewModel.CurrentPlaylistTrack is not null
                         && _primeViewModel.CurrentPlaylistTrack.EffectivePitch < 12)
                        {
                            int currentPitch = _primeViewModel.CurrentPlaylistTrack.EffectivePitch ?? 0;
                            int newPitch = Math.Min(12, currentPitch + pitchIncrement);
                            _primeViewModel.CurrentPlaylistTrack.PitchOverride = newPitch;
                            // If the track is using the playlist default, then we need to turn the override on.
                            if (_primeViewModel.CurrentPlaylistTrack.UsePitchOverride == false)
                            {
                                _primeViewModel.CurrentPlaylistTrack.UsePitchOverride = true;
                            }
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break;
                    case ApiMessage.PitchReset:
                        flashMessage = "Pitch: reset to default";
                        if (_primeViewModel.CurrentPlaylistTrack is not null)
                        {
                            _primeViewModel.CurrentPlaylistTrack.ResetOverridePitch();
                            // If the track is using the playlist default, then we need to turn the override on.
                            if (_primeViewModel.CurrentPlaylistTrack.UseTempoOverride == false)
                            {
                                _primeViewModel.CurrentPlaylistTrack.UseTempoOverride = true;
                            }
                        }
                        else
                        {
                            flashMessage += " (unable)";
                        }
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }

        if (!string.IsNullOrEmpty(flashMessage))
        {
            FlashPlaybackMessage(flashMessage);
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _heartbeat.Start();

        try
        {
            // Get the three books that have pages noted from the settings.
            _primeViewModel.Prayerbooks.Add(new BookCodeViewModel());
            _primeViewModel.Prayerbooks.Add(new BookCodeViewModel());
            _primeViewModel.Prayerbooks.Add(new BookCodeViewModel());

            for (int i = 0; i < 3; i++)
            {
                _primeViewModel.Prayerbooks[i] = new();
            }
            var listOfBookSettings = Properties.Settings.Default.BookNames;
            if (listOfBookSettings.Count == 3)
            {
                _primeViewModel.Prayerbooks =
                    [new BookCodeViewModel(0, listOfBookSettings[0] ?? "Book A"),
                     new BookCodeViewModel(1, listOfBookSettings[1] ?? "Book B"),
                     new BookCodeViewModel(2, listOfBookSettings[2] ?? "Book C")];
            }
            else
            {
                _primeViewModel.Prayerbooks =
                    [new BookCodeViewModel(0, "Book A"),
                     new BookCodeViewModel(1, "Book B"),
                     new BookCodeViewModel(2, "Book C")];
            }

            // NOTE: Make sure that the App.Config has been loaded (in the ctor)
            //       before trying to read the settings in the following code...

            // Load the list of last-known libraries
            _primeViewModel.KnownLibraries.Clear();
            _primeViewModel.OpenLibraries.Clear();
            var lastKnownLibraryFiles = Properties.Settings.Default.KnownLibraries;
            if (lastKnownLibraryFiles is not null && lastKnownLibraryFiles.Count > 0)
            {
                foreach (var libFilespec in lastKnownLibraryFiles)
                {
                    if (libFilespec is not null)
                    {
                        LoadSingleLibraryFile(libFilespec);
                    }
                }
            }
            KnownLibraryListComboBox.ItemsSource = _primeViewModel.KnownLibraries;

            // On startup, select the last used library, if it is loaded...
            string lastLoadedLibraryFilespec = Properties.Settings.Default.LastLibraryFile;
            if (lastLoadedLibraryFilespec.Length > 0)
            {
                // Is this one of the known (and valid) libraries?
                var lastLoaded = _primeViewModel.KnownLibraries
                               .FirstOrDefault(x => x.Filespec == lastLoadedLibraryFilespec);
                if (lastLoaded is not null && lastLoaded.IsValid)
                {
                    // Select this one.
                    KnownLibraryListComboBox.SelectedValue = lastLoaded;
                }
            }

            // Open the last used playlist, if it can be found.
            string lastLoadedPlaylistFilespec = Properties.Settings.Default.LastPlaylistFile;
            _primeViewModel.OpenPlaylistFile(lastLoadedPlaylistFilespec);

            // Select the first track of the playlist, if one is found...
            if (_primeViewModel.CurrentPlaylist is not null)
            {
                _primeViewModel.CurrentPlaylistTrack = _primeViewModel.CurrentPlaylist.Tracks[0];
            }

            // Refresh the whole Playback tab UI...
            _primeViewModel.RefreshPlaybackUI(PlaybackTabUiParts.FullPlaybackUI);

            // Clear the development time content of the status message textblock
            FlashPlaybackMessage(string.Empty);

        }
        catch (Exception ex)
        {
            App.WriteLog(ex);
        }
    }

    /// <summary>
    /// Load a library (.ciblib) file
    /// </summary>
    /// <param name="filespec">The full path and filespec of the file to be loaded</param>
    private void LoadSingleLibraryFile(string filespec)
    {
        if (File.Exists(filespec))
        {
            var library = LibraryModel.OpenLibraryFile(filespec);
            if (library is not null)
            {
                var libraryVM = new LibraryViewModel(library);

                // Do we already have this library loaded?
                // If so, reload it, if not, add it...
                if (_primeViewModel.OpenLibraries.ContainsKey(library.LibraryKey))
                {
                    _primeViewModel.OpenLibraries.Remove(library.LibraryKey);
                }
                // (Re-)Add it to the open libraries list...
                _primeViewModel.OpenLibraries.Add(library.LibraryKey, libraryVM);

                string libName = Path.GetFileNameWithoutExtension(filespec);
                // Same thing with the KnownLibraries list...
                var preExistingKnownLibrary = _primeViewModel.KnownLibraries
                    .FirstOrDefault(x => x.LibraryKey == library.LibraryKey);
                if (preExistingKnownLibrary is not null)
                {
                    _primeViewModel.KnownLibraries.Remove(preExistingKnownLibrary);
                }
                _primeViewModel.KnownLibraries.Add(new KnownLibraryViewModel(library.LibraryKey, true, filespec, library.Title, library.Description));
            }
        }
        else
        {
            // Don't add another copy of a missing library.
            var preExistingMissingLibrary = _primeViewModel.KnownLibraries
                .FirstOrDefault(x => x.Filespec == filespec
                                  && x.IsValid == false);
            if (preExistingMissingLibrary is null)
            {
                _primeViewModel.KnownLibraries.Add(new KnownLibraryViewModel(filespec));
            }
        }
    }

    private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            // When on the Playback tab, auto-next track whenever playback completes naturally
            // (otherwise don't do that, it isn't what is expected, for example, on the Playlist Item dialog)
            _primeViewModel.AutoNextWhenPlaybackFinishes = (e.AddedItems[0] is not null
                && ((TabItem)e.AddedItems[0]!).Name == "PlaybackTabItem");

            // If we're showing the Playback tab, give the UI a refresh in case changes were made
            // in the Playlist tab which could affect the display on the Playback UI.
            if ((e.AddedItems[0] is not null
                && ((TabItem)e.AddedItems[0]!).Name == "PlaybackTabItem"))
            {
                // Is there a selected track?
                if (_primeViewModel.CurrentPlaylistTrack is not null && _primeViewModel.CurrentPlaylist is not null)
                {
                    // TODO: See if there is a way on improving the Playback tab refresh so that drag drop is more intuitive
                    int currentIndex = _primeViewModel.CurrentTrackIndex;
                    if (currentIndex >=0)
                    {
                        // Reselect it so that the playback tab reflects any drag/drop or other changes on the playlist tab
                        _primeViewModel.CurrentPlaylistTrack = null;
                        _primeViewModel.CurrentPlaylistTrack = _primeViewModel.CurrentPlaylist.Tracks[currentIndex];
                    }
                }

                _primeViewModel.RefreshPlaybackUI(PlaybackTabUiParts.FullPlaybackUI);
            }
        }
        catch (Exception ex)
        {
            App.WriteLog(ex);
        }
    }
    
    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Which track had it's preview button pressed?
            Button? clickedButton = sender as Button;
            if (clickedButton is not null)
            {
                // Which preview button was pressed? (Library Entry or Playlist Item?)
                if (clickedButton.Name == "LibraryEntryPreviewButton")
                {
                    // The preview is of a library track.
                    LibraryEntryViewModel? clickedEntry = clickedButton.DataContext as LibraryEntryViewModel;
                    if (clickedEntry is not null && clickedEntry.Filespec is not null)
                    {
                        _preview.PreviewTrack(clickedEntry);
                    }
                }
                else if (clickedButton.Name == "PlaylistTrackPreviewButton")
                {
                    // The preview is of a playlist track.
                    TrackViewModel? clickedTrack = clickedButton.DataContext as TrackViewModel;
                    if (clickedTrack is not null && clickedTrack.Filespec is not null)
                    {
                        _preview.PreviewTrack(clickedTrack);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            App.WriteLog(ex);
        }
    }

    private void OpenLibraryButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Use a file open dialog to get a library file (Track List)...
            Microsoft.Win32.OpenFileDialog dlg = new()
            {
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".ciblib",
                Filter = "Library Files (*.ciblib)|*.ciblib",
                FilterIndex = 0,
                Multiselect = false,
                Title = "Open a Library File"
            };
            // Get a file...
            if (dlg.ShowDialog(this) == true)
            {
                string loadedFilespec = dlg.FileName;
                string loadedFileName = Path.GetFileNameWithoutExtension(loadedFilespec);

                LoadSingleLibraryFile(dlg.FileName);

                // Select this as the current library...
                foreach (var knownLibrary in _primeViewModel.KnownLibraries)
                {
                    if (Path.GetFileNameWithoutExtension(knownLibrary.Filespec) == loadedFileName)
                    {
                        KnownLibraryListComboBox.SelectedIndex = KnownLibraryListComboBox.Items.IndexOf(knownLibrary);
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            App.WriteLog(ex);
        }
    }

    private void NewLibraryButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Use a file save as dialog to get a filespec for the new library...
            Microsoft.Win32.SaveFileDialog dlg = new()
            {
                AddExtension = true,
                CheckFileExists = false,
                CheckPathExists = true,
                DefaultExt = ".ciblib"
            };
            // If there's an open library when the new one is created, pick up the path as a starting point.
            if (_primeViewModel.CurrentLibrary is not null)
            {
                if (_primeViewModel.CurrentLibrary.Filespec is not null &&
                    _primeViewModel.CurrentLibrary.Filespec.Length > 0)
                {   // Use the existing file name by default...
                    dlg.InitialDirectory = System.IO.Path.GetDirectoryName(_primeViewModel.CurrentLibrary.Filespec);
                }
            }
            // Initialize a new Library...
            LibraryModel newLibraryModel = new()
            {
                Title = "(New Library)"
            };
            // Finish setting up the dialog and 
            dlg.Filter = "Library Files (*.ciblib)|*.ciblib";
            dlg.FilterIndex = 0;
            dlg.OverwritePrompt = true;
            dlg.Title = "Create New Library";
            if (dlg.ShowDialog(this) == true)
            {   // Save the file...
                string newLibraryFilespec = dlg.FileName!;
                // Persist the file...
                LibraryModel.SaveLibraryFile(newLibraryModel, newLibraryFilespec);
                LibraryViewModel libraryViewModel = new(newLibraryModel);
                // Add it to the open libraries list...
                _primeViewModel.OpenLibraries.Add(newLibraryModel.LibraryKey, libraryViewModel);
                // Put it in the combo box and select it.
                string libraryName = Path.GetFileNameWithoutExtension(newLibraryFilespec).Trim();
                KnownLibraryViewModel newKnownLibVM = new(newLibraryModel.LibraryKey, true, newLibraryFilespec, newLibraryModel.Title, newLibraryModel.Description);
                _primeViewModel.KnownLibraries.Add(newKnownLibVM);
                KnownLibraryListComboBox.SelectedItem = newKnownLibVM;

                _primeViewModel.CurrentLibrary = libraryViewModel;
            }
        }
        catch (Exception ex)
        {
            App.WriteLog(ex);
        }
    }

    private void SaveAsLibraryButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Use a file save as dialog to get the library filespec...
            Microsoft.Win32.SaveFileDialog dlg = new()
            {
                Title = "Save Library File As",
                AddExtension = true,
                CheckFileExists = false,
                CheckPathExists = true,
                DefaultExt = ".ciblib"
            };
            if (_primeViewModel.CurrentLibrary is not null)
            {
                if (_primeViewModel.CurrentLibrary.Filespec is not null &&
                    _primeViewModel.CurrentLibrary.Filespec.Length > 0)
                {   // Use the existing file name by default...
                    dlg.InitialDirectory = System.IO.Path.GetDirectoryName(_primeViewModel.CurrentLibrary.Filespec);
                    dlg.FileName = System.IO.Path.GetFileName(_primeViewModel.CurrentLibrary.Filespec);
                }
                else
                {   // Make up a default name...
                    dlg.FileName = _primeViewModel.CurrentLibrary.Title;
                }
                dlg.Filter = "Library Files (*.ciblib)|*.ciblib";
                dlg.FilterIndex = 0;
                dlg.OverwritePrompt = false;
                if (dlg.ShowDialog(this) == true)
                {   // Save the file...
                    string newLibraryFilespec = dlg.FileName!;
                    _primeViewModel.CurrentLibrary.SaveAs(newLibraryFilespec);
                    _primeViewModel.CanSaveLibrary = true;

                    // Now add this to the list of known libraries and open libraries...
                    var library = LibraryModel.OpenLibraryFile(newLibraryFilespec);
                    if (library is not null)
                    {
                        // Add it to the open libraries list...
                        LibraryViewModel libraryVM = new(library);
                        _primeViewModel.OpenLibraries.Add(library.LibraryKey, libraryVM);

                        string libraryName = Path.GetFileNameWithoutExtension(newLibraryFilespec).Trim();
                        KnownLibraryViewModel newKnownLibVM = new(library.LibraryKey, true, newLibraryFilespec, library.Title, library.Description);
                        _primeViewModel.KnownLibraries.Add(newKnownLibVM);
                        KnownLibraryListComboBox.SelectedItem = newKnownLibVM;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            App.WriteLog(ex);
        }
    }

    private void AddLibraryTrackButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Use a file open dialog to get an MP3 file to play...
            Microsoft.Win32.OpenFileDialog dlg = new()
            {
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".mp3",
                // TODO: Add WMA support to the Add to Playlist file open dialog.
                //dlg.Filter = "WMA Files (*.wma)|*.wma|;
                Filter = "Supported Sound Files|*.mp3;*.wav|MP3 Files (*.mp3)|*.mp3|Wave Files (*.wav)|*.wav|All files (*.*)|*.*",
                FilterIndex = 0,
                Multiselect = false,
                Title = "Add an Entry to the Library"
            };
            // Get a file...
            if (dlg.ShowDialog(this) == true)
            {   // What file was selected?
                if (dlg.FileName.Length > 0 && _primeViewModel.CurrentLibrary is not null)
                {
                    // Gather up the track info...
                    string newFilespec = dlg.FileName;
                    string newTitle = Path.GetFileNameWithoutExtension(newFilespec);
                    // It would be better if we can get the title from the MP3 tags...
                    // See if we can glean the title from the track itself...
                    TagLib.File tagFile = TagLib.File.Create(newFilespec);
                    if (tagFile is not null)
                    {   // Is there a title?
                        if (!string.IsNullOrEmpty(tagFile.Tag.Title))
                        {
                            newTitle = tagFile.Tag.Title;
                        }
                    }
                    int lengthInSeconds = (int)Math.Round(Toolbox.GetMediaDuration(newFilespec));

                    // Create a Library Entry for this file.
                    LibraryEntryModel newEntry = new()
                    {
                        Filespec = newFilespec,
                        Title = newTitle,
                        NominalLength = lengthInSeconds,
                    };
                    LibraryEntryViewModel newEntryVM = new(newEntry);

                    // Add it to the open libraries list and refresh the current library view model...
                    Guid currentLibraryKey = (Guid)_primeViewModel.CurrentLibrary.LibraryKey!;
                    _primeViewModel.OpenLibraries[currentLibraryKey].Entries.Add(newEntryVM);

                    // Refresh the current library view model.
                    // DWR: For some reason, if this isn't done, then the new added track isn't editable in the GUI?!?!?
                    _primeViewModel.CurrentLibrary = null;
                    _primeViewModel.CurrentLibrary = _primeViewModel.OpenLibraries[currentLibraryKey];

                    // Refresh the CurrentItem view model...
                    _primeViewModel.CurrentLibraryEntry = null;
                    if (_primeViewModel.CurrentLibrary.Entries.Count > 0)
                    {
                        // Pick the last item as the current item...
                        _primeViewModel.CurrentLibraryEntry = _primeViewModel.CurrentLibrary.Entries[^1];
                        // Now select the item and edit it...
                        LibraryEntryPropertiesContainerGrid.Visibility = Visibility.Visible;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            App.WriteLog(ex);
        }
    }

    private void RemoveLibraryTrackButton_Click(object sender, RoutedEventArgs e)
    {
        // Bail out if we don't have a library loaded.
        if (_primeViewModel.CurrentLibrary is null)
        {
            return;
        }

        // Prepare a confirmation message for the dialog (overlay)
        // and also make a list of the items to be removed.
        int itemCount = 0;
        StringBuilder confirmList = new();
        foreach (var entry in _primeViewModel.CurrentLibrary.Entries)
        {
            if (entry.Selected)
            {
                itemCount++;
                confirmList.Append("- ");
                if (entry.Title is not null)
                {
                    confirmList.Append(entry.Title);
                    if (entry.Rendition is not null)
                    {
                        confirmList.Append(" (" + entry.Rendition + ")");
                    }
                    confirmList.AppendLine();
                }
                else
                {
                    confirmList.AppendLine(entry.Filespec);
                }
            }
        }
        string confirmMessage = "Are you sure you want to permanently delete ";
        if (itemCount == 1)
        {
            confirmMessage += "this track?";
        }
        else {
            confirmMessage += "these " + itemCount.ToString() + " entries?";
        }
        _primeViewModel.LibraryEntryConfirmDeleteMessage = confirmMessage;
        _primeViewModel.LibraryEntryConfirmDeleteList = confirmList.ToString();

        // Show the dialog
        LibraryEntryConfirmationContainerGrid.Visibility = Visibility.Visible;
    }

    private void IncludeTracksButton_Click(object sender, RoutedEventArgs e)
    {
        // Bail out if we don't have a library and playlist loaded.
        if (_primeViewModel.CurrentLibrary is null 
            || _primeViewModel.CurrentLibrary.LibraryKey is null
            || _primeViewModel.CurrentPlaylist is null)
        {
            return;
        }

        // Look at the entries and create a track for any that are selected...
        foreach (var entry in _primeViewModel.CurrentLibrary.Entries)
        {
            if (entry.Selected && entry.LibraryEntryKey is not null)
            {
                _primeViewModel.AppendLibraryTrackToCurrentPlaylist(_primeViewModel.CurrentLibrary.LibraryKey, entry.LibraryEntryKey);
                
                entry.Selected = false;
            }
        }
    }

    private void KnownLibraryListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedLibrary = ((KnownLibraryViewModel)((System.Windows.Controls.Primitives.Selector)e.Source).SelectedValue);
        if (selectedLibrary is not null && selectedLibrary.IsValid && selectedLibrary.LibraryKey is not null)
        {
            Guid selectedLibraryKey = (Guid)selectedLibrary.LibraryKey;
            _primeViewModel.CurrentLibrary = _primeViewModel.OpenLibraries[selectedLibraryKey];
            _primeViewModel.CurrentLibraryEntry = null;
            if (_primeViewModel.CurrentLibrary.Entries.Count > 0)
            {
                _primeViewModel.CurrentLibraryEntry=_primeViewModel.CurrentLibrary.Entries[0];
            }
        }
        // Remember to mark the event handled so that it doesn't bubble up further.
        e.Handled = true;
    }

    private void LibraryEntryDetailsCloseButton_Click(object sender, RoutedEventArgs e)
    {
        LibraryEntryPropertiesContainerGrid.Visibility = Visibility.Hidden;
    }

    private void EditLibraryEntryButton_Click(object sender, RoutedEventArgs e)
    {
        // Set the current library track item
        if (sender is not null)
        {
            Button? clickedButton = sender as Button;
            if (clickedButton is not null)
            {
                LibraryEntryViewModel? clickedEntry = clickedButton.DataContext as LibraryEntryViewModel;
                if (clickedEntry is not null)
                {
                    _primeViewModel.CurrentLibraryEntry = clickedEntry;
                }
            }
        }
        LibraryEntryPropertiesContainerGrid.Visibility = Visibility.Visible;
    }

    private void LibraryEntrySelectionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        _primeViewModel.RefreshHasSelectedLibraryEntries();
    }

    private void CancelLibraryEntryDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        LibraryEntryConfirmationContainerGrid.Visibility = Visibility.Hidden;
    }

    private void ConfirmLibraryEntryDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        // Bail out if we don't have a library loaded.
        if (_primeViewModel.CurrentLibrary is null)
        {
            return;
        }

        // Reset the current track in case this gets in the way of removing it, if applicable
        _primeViewModel.CurrentLibraryEntry = null;

        // Make a list of the items to be removed...
        List<LibraryEntryViewModel> gonners = [];

        // Enumerate each of the selected items...
        foreach (var entry in _primeViewModel.CurrentLibrary.Entries)
        {
            if (entry.Selected)
            {
                gonners.Add(entry);
            }
        }
        // Now remove them...
        foreach (var gonner in gonners)
        {
            _primeViewModel.CurrentLibrary.Entries.Remove(gonner);
        }

        // Hide the dialog
        LibraryEntryConfirmationContainerGrid.Visibility = Visibility.Hidden;
    }

    private void OpenPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Use a file open dialog to get a playlist file...
            Microsoft.Win32.OpenFileDialog dlg = new()
            {
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".cibplf",
                Filter = "Playlist Files (*.cibplf)|*.cibplf",
                FilterIndex = 0,
                Multiselect = false,
                Title = "Open a Playlist File"
            };
            // Get a file...
            if (dlg.ShowDialog(this) == true)
            {
                string loadedFilespec = dlg.FileName;
                string loadedFileName = Path.GetFileNameWithoutExtension(loadedFilespec);

                // Load the file and select it as the current one.
                _primeViewModel.OpenPlaylistFile(loadedFilespec);
            }
        }
        catch (Exception ex)
        {
            App.WriteLog(ex);
        }
    }

    private void NewPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Use a file save as dialog to get a filespec for the new playlist...
            Microsoft.Win32.SaveFileDialog dlg = new()
            {
                AddExtension = true,
                CheckFileExists = false,
                CheckPathExists = true,
                DefaultExt = ".cibplf"
            };
            // If there's an open playlist when the new one is created, pick up the path as a starting point.
            if (_primeViewModel.CurrentPlaylist is not null)
            {
                if (_primeViewModel.CurrentPlaylist.Filespec is not null &&
                    _primeViewModel.CurrentPlaylist.Filespec.Length > 0)
                {   // Use the existing file name by default...
                    dlg.InitialDirectory = System.IO.Path.GetDirectoryName(_primeViewModel.CurrentPlaylist.Filespec);
                }
            }
            // Initialize a new Playlist...
            PlaylistModel newPlaylistModel = new()
            {
                Title = "(New Playlist)"
            };
            // Finish setting up the dialog and 
            dlg.Filter = "Playlist Files (*.cibplf)|*.cibplf";
            dlg.FilterIndex = 0;
            dlg.OverwritePrompt = true;
            dlg.Title = "Create New Playlist";
            if (dlg.ShowDialog(this) == true)
            {   // Save the file...
                string newPlaylistFilespec = dlg.FileName!;
                // Persist the file...
                PlaylistModel.SavePlaylistFile(newPlaylistModel, newPlaylistFilespec);
                PlaylistViewModel playlistViewModel = new(newPlaylistModel,_primeViewModel);
                // Make it the current playlist...
                _primeViewModel.CurrentPlaylist = playlistViewModel;
            }
        }
        catch (Exception ex)
        {
            App.WriteLog(ex);
        }
    }

    private void SaveAsPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Use a file save as dialog to get the library filespec...
            Microsoft.Win32.SaveFileDialog dlg = new()
            {
                Title = "Save Playlist File As",
                AddExtension = true,
                CheckFileExists = false,
                CheckPathExists = true,
                DefaultExt = ".cibplf"
            };
            if (_primeViewModel.CurrentPlaylist is not null)
            {
                if (_primeViewModel.CurrentPlaylist.Filespec is not null &&
                    _primeViewModel.CurrentPlaylist.Filespec.Length > 0)
                {   // Use the existing file name by default...
                    dlg.InitialDirectory = System.IO.Path.GetDirectoryName(_primeViewModel.CurrentPlaylist.Filespec);
                    dlg.FileName = System.IO.Path.GetFileName(_primeViewModel.CurrentPlaylist.Filespec);
                }
                else
                {   // Make up a default name...
                    dlg.FileName = _primeViewModel.CurrentPlaylist.Title;
                }
                dlg.Filter = "Playlist Files (*.cibplf)|*.cibplf";
                dlg.FilterIndex = 0;
                dlg.OverwritePrompt = false;
                if (dlg.ShowDialog(this) == true)
                {   // Save the file...
                    string newPlaylistFilespec = dlg.FileName!;
                    _primeViewModel.CurrentPlaylist.SaveAs(newPlaylistFilespec);
                    _primeViewModel.CanSavePlaylist = true;

                    // Now flip the current playlist over to the newly saved one...
                    _primeViewModel.OpenPlaylistFile(newPlaylistFilespec);
                }
            }
        }
        catch (Exception ex)
        {
            App.WriteLog(ex);
        }
    }

    private void RemovePlaylistTrackButton_Click(object sender, RoutedEventArgs e)
    {
        // Bail out if we don't have a library loaded.
        if (_primeViewModel.CurrentPlaylist is null)
        {
            return;
        }

        // Prepare a confirmation message for the dialog (overlay)
        // and also make a list of the items to be removed.
        int itemCount = 0;
        StringBuilder confirmList = new();
        foreach (var track in _primeViewModel.CurrentPlaylist.Tracks)
        {
            if (track.Selected)
            {
                itemCount++;
                confirmList.Append("- ");
                if (track.Title is not null)
                {
                    confirmList.Append(track.Title);
                    if (track.Rendition is not null)
                    {

                        confirmList.Append(" (" + track.Rendition + ")");
                    }
                    confirmList.AppendLine();
                }
                else
                {
                    confirmList.AppendLine(track.Filespec);
                }
            }
        }
        string confirmMessage = "Are you sure you want to remove ";
        if (itemCount == 1)
        {
            confirmMessage += "this track";
        }
        else
        {
            confirmMessage += "these " + itemCount.ToString() + " tracks";
        }
        confirmMessage += " from the playlist?";
        // Set up the variable dialog contents...
        _primeViewModel.PlaylistTrackConfirmDeleteMessage = confirmMessage;
        _primeViewModel.PlaylistTrackConfirmDeleteList = confirmList.ToString();

        // Show the dialog...
        PlaylistTrackConfirmationContainerGrid.Visibility = Visibility.Visible;
    }

    private void PlaylistDefaultBookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int selectedBook = (int)(((System.Windows.Controls.Primitives.Selector)e.Source).SelectedValue);
        if (_primeViewModel.CurrentPlaylist is not null)
        {
            _primeViewModel.CurrentPlaylist.DefaultBook = selectedBook;
        }

        // Don't let this bubble up to the tab control
        e.Handled = true;
    }

    private void PlaylistTrackSelectionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        _primeViewModel.RefreshHasSelectedPlaylistTracks();
    }

    private void EditPlaylistTrackButton_Click(object sender, RoutedEventArgs e)
    {
        // Set the current playlist track item
        if (sender is not null)
        {
            Button? clickedButton = sender as Button;
            if (clickedButton is not null)
            {
                TrackViewModel? clickedEntry = clickedButton.DataContext as TrackViewModel;
                if (clickedEntry is not null)
                {
                    _primeViewModel.CurrentPlaylistTrack = clickedEntry;
                }
            }
        }
        PlaylistTrackPropertiesContainerGrid.Visibility = Visibility.Visible;
    }

    private void ConfirmPlaylistTrackDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        // Bail out if we don't have a playlist loaded.
        if (_primeViewModel.CurrentPlaylist is null)
        {
            return;
        }

        // Reset the current track in case this gets in the way of removing it, if applicable
        _primeViewModel.CurrentPlaylistTrack = null;

        // Make a list of the items to be removed...
        List<TrackViewModel> gonners = [];

        // Enumerate each of the selected items...
        foreach (var entry in _primeViewModel.CurrentPlaylist.Tracks)
        {
            if (entry.Selected)
            {
                gonners.Add(entry);
            }
        }
        // Now remove them...
        foreach (var gonner in gonners)
        {
            _primeViewModel.CurrentPlaylist.ExcludeTrack(gonner);
        }

        // Hide the dialog
        PlaylistTrackConfirmationContainerGrid.Visibility = Visibility.Hidden;
    }

    private void CancelPlaylistTrackDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        PlaylistTrackConfirmationContainerGrid.Visibility = Visibility.Hidden;
    }

    private void PlaylistTrackPropertiesDialogCloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Before closing, if there is a playback running stop it.
        if (_primeViewModel.CurrentPlaylistTrack is not null)
        {
            if (_primeViewModel.CurrentPlaylistTrack.CanStop)
            {
                _primeViewModel.CurrentPlaylistTrack.StopCurrentTrack();
            }
        }

        PlaylistTrackPropertiesContainerGrid.Visibility = Visibility.Hidden;
    }

    #region Workaround for XAML TextBox auto-selection of contents

    // Adapted from https://stackoverflow.com/a/16328482/659653 to make textboxes select contents
    // when used with:
    //      MouseDoubleClick="SelectAddress",
    //      GotKeyboardFocus="SelectAddress",
    //      PreviewMouseLeftButtonDown="SelectivelyIgnoreMouseButton"
    //
    // in the TextBox XAML.
    private void SelectTextboxContents(object sender, RoutedEventArgs e)
    {
        if (sender is not null && sender.GetType() == typeof(TextBox))
        {
            if (sender is TextBox tb)
            {
                tb?.SelectAll();
            }
        }
    }

    /// <summary>
    /// Selectively ignores mouse button clicks in text boxes when focused via the keyboard
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
    {
        if (sender is not null && sender.GetType() == typeof(TextBox))
        {
            if (sender is TextBox tb)
            {
                if (!tb.IsKeyboardFocusWithin)
                {
                    e.Handled = true;
                    tb.Focus();
                }
            }
        }
    }
    #endregion

    /// <summary>
    /// Temporarily displays a message on the UI before fading it out.
    /// Used to indicate when a background action (like an API command) takes place
    /// </summary>
    /// <param name="message">The message to be shown</param>
    private void FlashPlaybackMessage(string message)
    {
        LowerRightCornerStatusMessages.Text = message;

        // Animate the PlaybackTabStatusMessages TextBlock opacity
        SineEase easing = new()
        {
            EasingMode = EasingMode.EaseOut
        };
        // NOTE: Want to go from 1.0 to 0.0 but start at 4.0 so the first 3/4 isn't effectively animating (yet)
        DoubleAnimation fadeOutAnimation = new(4.0, 0.0, TimeSpan.FromMilliseconds(2000))
        {
            EasingFunction = easing
        };

        LowerRightCornerStatusMessages.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation, HandoffBehavior.SnapshotAndReplace);
    }

    /// <summary>
    /// The full path and filespec of the temporary file where the UI thread and the HTTP API thread exchange status information
    /// </summary>
    private string _statusApiDetailsFilespec = string.Empty;

    /// <summary>
    /// The Mutex name to use for the status file. DWR: this must also be set in HomeController.cs to an identical value!
    /// </summary>
    private const string _statusApiFilespecMutex = "CantorInABoxStatusFile";

    /// <summary>
    /// Writes status information into a temporary file to be shared from the UI thread with the HTTP API background timer thread
    /// </summary>
    /// <param name="statusApiViewModel">An object containing the pertinent status data points</param>
    internal void WriteStatusApiDetailsFile(StatusApiViewModel statusApiViewModel)
    {
        // Make sure we have the file defined
        if (String.IsNullOrEmpty(_statusApiDetailsFilespec))
        {
            // Set the API details transfer file name DWR: this must be the same as in HomeController.cs
            _statusApiDetailsFilespec = Path.GetTempPath() + "CantorInABoxStatusAPI.json";
        }
        using var mutex = new Mutex(false, _statusApiFilespecMutex);
        var hasHandle = false;
        try
        {
            hasHandle = mutex.WaitOne(Timeout.Infinite, false);
            string content = JsonSerializer.Serialize(statusApiViewModel);
            File.WriteAllText(_statusApiDetailsFilespec, content);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            if (hasHandle)
            {
                mutex.ReleaseMutex();
            }
        }
    }
}