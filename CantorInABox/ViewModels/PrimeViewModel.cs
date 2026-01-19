using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mooseware.CantorInABox.Models;
using Mooseware.CantorInABox.Themes.Styles;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Mooseware.CantorInABox.ViewModels;

/// <summary>
/// Main view model for the application. Supports top level UI element contents and behaviours.
/// </summary>
public partial class PrimeViewModel : ObservableObject
{
    /// <summary>
    /// The Library which is the focus of actions on the Library tab
    /// </summary>
    [ObservableProperty]
    private LibraryViewModel? _currentLibrary;
    /// <summary>
    /// The Library Entry which is the focus of actions on the Library Entry details tab (on the Library tab)
    /// </summary>
    [ObservableProperty]
    private LibraryEntryViewModel? _currentLibraryEntry;
    /// <summary>
    /// Flag indicating whether there is a library selected as the CurrentLibrary. 
    /// Used to drive UI behaviours.
    /// </summary>
    [ObservableProperty]
    private bool _hasCurrentLibrary = false;
    /// <summary>
    /// Flag indicating whether there is NO library selected as the CurrentLibrary. 
    /// Used to drive UI behaviours.
    /// </summary>
    [ObservableProperty]
    private bool _hasNoCurrentLibrary = true;
    /// <summary>
    /// Flag indicating whether there is a library entry selected as the CurrentLibraryEntry. 
    /// Used to drive UI behaviours.
    /// </summary>
    [ObservableProperty]
    private bool _hasCurrentLibraryEntry = false;
    /// <summary>
    /// Flag indicating whether there is NO library entry selected as the CurrentLibraryEntry. 
    /// Used to drive UI behaviours.
    /// </summary>
    [ObservableProperty]
    private bool _hasNoCurrentLibraryEntry = true;
    /// <summary>
    /// Flag indicating whether it makes sense in the current context to save (the current) library. 
    /// Used to drive UI behaviours.
    /// </summary>
    [ObservableProperty]
    private bool _canSaveLibrary = false;
    /// <summary>
    /// The Playlist which is the focus of actions on the Playlist and Playback tabs
    /// </summary>
    [ObservableProperty]
    private PlaylistViewModel? _currentPlaylist;
    /// <summary>
    /// The Track which is the focus of actions on the Playlist Track Details dialog (Playlist tab) or the Playback tab
    /// </summary>
    [ObservableProperty]
    private TrackViewModel? _currentPlaylistTrack;
    /// <summary>
    /// Flag indicating whether there is a playlist selected as the CurrentPlaylist. 
    /// Used to drive UI behaviours.
    /// </summary>
    [ObservableProperty]
    private bool _hasCurrentPlaylist = false;
    /// <summary>
    /// Flag indicating whether there is NO playlist selected as the CurrentPlaylist. 
    /// Used to drive UI behaviours.
    /// </summary>
    [ObservableProperty]
    private bool _hasNoCurrentPlaylist = false;
    /// <summary>
    /// Flag indicating whether it makes sense in the current context to save (the current) playlist. 
    /// Used to drive UI behaviours.
    /// </summary>
    [ObservableProperty]
    private bool _canSavePlaylist = false;
    /// <summary>
    /// The user-friendly name of the first prayerbook (can be overridden by application settings)
    /// </summary>
    [ObservableProperty]
    private string _pagesLabel1 = "First Book:";
    /// <summary>
    /// The user-friendly name of the second prayerbook (can be overridden by application settings)
    /// </summary>
    [ObservableProperty]
    private string _pagesLabel2 = "Second Book:";
    /// <summary>
    /// The user-friendly name of the third prayerbook (can be overridden by application settings)
    /// </summary>
    [ObservableProperty]
    private string _pagesLabel3 = "Third Book:";
    /// <summary>
    /// The message displayed before a library entry is deleted from the current library.
    /// Used to confirm the action with the user before proceeding.
    /// </summary>
    [ObservableProperty]
    private string _libraryEntryConfirmDeleteMessage = string.Empty;
    /// <summary>
    /// The list of library entries selected for deletion to be included in a confirmation
    /// message on the library tab.
    /// </summary>
    [ObservableProperty]
    private string _libraryEntryConfirmDeleteList = string.Empty;
    /// <summary>
    /// The message displayed before a playlist track is deleted from the current playlist.
    /// Used to confirm the action with the user before proceeding.
    /// </summary>
    [ObservableProperty]
    private string _playlistTrackConfirmDeleteMessage = string.Empty;
    /// <summary>
    /// The list of playlist tracks selected for deletion to be included in a confirmation
    /// message on the playlist tab.
    [ObservableProperty]
    private string _playlistTrackConfirmDeleteList = string.Empty;
    /// <summary>
    /// The brief description of the current playlist as shown on the Playback tab UI
    /// </summary>
    [ObservableProperty]
    private string _currentPlaylistPlaybackTabDescription = string.Empty;
    /// <summary>
    /// The 0-based index of the current track in the current playlist (<0 if n/a)
    /// </summary>
    [ObservableProperty]
    private int _currentTrackIndex = int.MinValue;
    /// <summary>
    /// A description of the current track (headline) for display on the Playback tab
    /// </summary>
    [ObservableProperty]
    private string _currentTrackPlaybackDescription = string.Empty;
    /// <summary>
    /// Details of the current track (byline) for display on the Playback tab
    /// </summary>
    [ObservableProperty]
    private string _currentTrackPlaybackDescriptionDetails = string.Empty;
    /// <summary>
    /// Flag indicating whether the current context allows for scrubbing the position within the current track
    /// Used to control UI behaviour
    /// </summary>
    [ObservableProperty]
    private bool _currentTrackPlaybackCanScrub = false;
    /// <summary>
    /// A summary description of the track prior to the current track (if any) for use on the Playback tab
    /// </summary>
    [ObservableProperty]
    private string _previousTrackDescription = "(none)";
    /// <summary>
    /// A summary description of the track following the current track (if any) for use on the Playback tab
    /// </summary>
    [ObservableProperty]
    private string _nextTrackDescription = "(none)";

    // These properties primarily drive the visual appearance of the UI (particularly Playback tab)
    // --------------------------------------------------------------------------------------------

    /// <summary>
    /// Border brush for the Playback tab's current track indication
    /// </summary>
    [ObservableProperty]
    private Brush _currentTrackBorderBrush = AppResources.DefinedColour(AppResources.StaticResource.NoClipBackgroundBorderBrush);
    /// <summary>
    /// Background brush for the Playback tab's current track indication
    /// </summary>
    [ObservableProperty]
    private Brush _currentTrackBorderBackground = AppResources.DefinedColour(AppResources.StaticResource.NoClipBackgroundBrush);
    /// <summary>
    /// Label content for the transport state of the current track on the Playback tab
    /// </summary>
    [ObservableProperty]
    private string _currentTrackStatusLabel = "(None)";
    /// <summary>
    /// Status colour for the current track on the Playback tab when there is no current track
    /// </summary>
    [ObservableProperty]
    private Brush _currentTrackStatusColour = AppResources.DefinedColour(AppResources.StaticResource.NoClipMainBrush);
    /// <summary>
    /// Foreground colour for the Play button on the Playback tab
    /// </summary>
    [ObservableProperty]
    private Brush _playbackPlayButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.DisabledMainBrush);
    /// <summary>
    /// Background colour for the Play button on the Playback tab
    /// </summary>
    [ObservableProperty]
    private Brush _playbackPlayButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.DisabledContrastBrush);
    /// <summary>
    /// Foreground colour for the Pause button on the Playback tab
    /// </summary>
    [ObservableProperty]
    private Brush _playbackPauseButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.DisabledMainBrush);
    /// <summary>
    /// Background colour for the Pause button on the Playback tab
    /// </summary>
    [ObservableProperty]
    private Brush _playbackPauseButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.DisabledContrastBrush);
    /// <summary>
    /// Foregroung colour for the Stop button on the Playback tab
    /// </summary>
    [ObservableProperty]
    private Brush _playbackStopButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.DisabledMainBrush);
    /// <summary>
    /// Background colour for the Stop button on the Playback tab
    /// </summary>
    [ObservableProperty]
    private Brush _playbackStopButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.DisabledContrastBrush);
    /// <summary>
    /// Flag indicating whether the Previous Track button should be enabled or not
    /// </summary>
    [ObservableProperty]
    private bool _canPreviousTrack = false;
    /// <summary>
    /// Flag indicating whether the Next Track button should be enabled or not
    /// </summary>
    [ObservableProperty]
    private bool _canNextTrack = false;
    /// <summary>
    /// Flag indicating whether the Playlist tab should be enabled or not
    /// </summary>
    [ObservableProperty]
    private bool _canPlaylistTab = true;
    /// <summary>
    /// Flag indicating whether the Library tab should be enabled or not
    /// </summary>
    [ObservableProperty]
    private bool _canLibraryTab = true;

    // These properties are related to the Audio Playback device
    // ---------------------------------------------------------

    /// <summary>
    /// Flag indicating whether or not the Play button should be enabled or disabled
    /// </summary>
    [ObservableProperty]
    private bool _canPlay = false;
    /// <summary>
    /// Flag indicating whether or not the Pause button should be enabled or disabled
    /// </summary>
    [ObservableProperty]
    private bool _canPause = false;
    
    /// <summary>
    /// Flag indicating whether or not the Stop button should be enabled or disabled
    /// </summary>
    [ObservableProperty]
    private bool _canStop = false;
    
    /// <summary>
    /// Current Track playback position formatted as minutes and seconds
    /// </summary>
    [ObservableProperty]
    private string _currentTrackPositionFormatted = "[0:00]";

    /// <summary>
    /// Current Track time remaining from the current playback position formatted as minutes and seconds
    /// </summary>
    [ObservableProperty]
    private string _currentTrackRemainingFormatted = "[0:00]";

    /// <summary>
    /// Current track playback position expressed as a number between 0 and 1000 (the limits of the current track progress slider)
    /// </summary>
    [ObservableProperty]
    private int _currentTrackProgressSliderValue = 0;

    // These properties are application settings related to the Audio Playback Device
    // ------------------------------------------------------------------------------

    /// <summary>
    /// The lowest acceptable value for Pitch set via the AudioPlayback hard floor but overridable from application settings.
    /// </summary>
    [ObservableProperty]
    private int _pitchFloor = AudioPlayback.PitchHardFloor;
    /// <summary>
    /// The highest acceptable value for Pitch set via the AudioPlayback hard ceiling but overridable from application settings.
    /// </summary>
    [ObservableProperty]
    private int _pitchCeiling = AudioPlayback.PitchHardCeiling;
    /// <summary>
    /// The lowest acceptable value for Pan set via the AudioPlayback hard floor but overridable from application settings.
    /// </summary>
    [ObservableProperty]
    private float _panFloor = AudioPlayback.PanHardFloor;
    /// <summary>
    /// The highest acceptable value for Pan set via the AudioPlayback hard ceiling but overridable from application settings.
    /// </summary>
    [ObservableProperty]
    private float _panCeiling = AudioPlayback.PanHardCeiling;
    /// <summary>
    /// The lowest acceptable value for Tempo set via the AudioPlayback hard floor but overridable from application settings.
    /// </summary>
    [ObservableProperty]
    private double _tempoFloor = AudioPlayback.TempoHardFloor;
    /// <summary>
    /// The highest acceptable value for Tempo set via the AudioPlayback hard ceiling but overridable from application settings.
    /// </summary>
    [ObservableProperty]
    private double _tempoCeiling = AudioPlayback.TempoHardCeiling;
    /// <summary>
    /// The lowest acceptable value for Volume set via the AudioPlayback hard floor but overridable from application settings.
    /// Scaled from 0 to 100 instead of the model-native 0 to 1.0
    /// </summary>
    [ObservableProperty]
    private float _volumeFloor = AudioPlayback.VolumeHardFloor * 100f;
    /// <summary>
    /// The highest acceptable value for Volume set via the AudioPlayback hard ceiling but overridable from application settings.
    /// Scaled from 0 to 100 instead of the model-native 0 to 1.0
    /// </summary>
    [ObservableProperty]
    private float _volumeCeiling = AudioPlayback.VolumeHardCeiling * 100f;

    /// <summary>
    /// The list of Library files that have actually been found and are open and available for use.
    /// </summary>
    public readonly Dictionary<Guid, LibraryViewModel> OpenLibraries = [];

    /// <summary>
    /// The collection of prayer books (from application settings)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<BookCodeViewModel> _prayerbooks = [];

    /// <summary>
    /// UI-specific list of known libraries stored in user settings and loaded at startup. 
    /// Used to load the libaries in the MainViewModel and also to provide UI feedback about
    /// which libraries were previously known but seem to have gone missing.
    /// </summary>
    public ObservableCollection<KnownLibraryViewModel> KnownLibraries { get; set; }

    /// <summary>
    /// Sentinel used by the UI to tell the view model whether to enable auto-next when playback completes.
    /// </summary>
    public bool AutoNextWhenPlaybackFinishes { get; set; }

    /// <summary>
    /// Playback device used to play audio tracks with playback settings applied
    /// </summary>
    private readonly AudioPlayback _playbackDevice = new();

    /// <summary>
    /// Establishes the prime view model instance and initializes core objects and properties
    /// </summary>
    public PrimeViewModel()
    {
        _currentLibrary = new();
        _currentLibraryEntry = new();
        KnownLibraries = [];
        Prayerbooks.Clear();

        AutoNextWhenPlaybackFinishes = false;
        _playbackDevice.PlaybackFinished += PlaybackDevice_PlaybackFinished;
    }

    /// <summary>
    /// Event called when playback stops either by being forced to stop or through the audio file reaching the end
    /// </summary>
    private void PlaybackDevice_PlaybackFinished(object? sender, EventArgs e)
    {
        // Force this manually since _transport mode still shows as playing for some reason when this event fires.
        _playbackDevice.Stop();

        // Reset the adjacent track buttons
        AssessAdjacentTrackButtonStatus();

        // When a playback finishes naturally, move to the next track automatically
        if (CurrentPlaylist is not null 
            && CurrentTrackIndex < CurrentPlaylist.Tracks.Count - 1
            && AutoNextWhenPlaybackFinishes)
        {
            NextPlaybackTrack();
        }
    }

    /// <summary>
    /// Flag indicating whether there are selected library entries
    /// Controls UI behaviour
    /// </summary>
    [ObservableProperty]
    private bool _hasSelectedLibraryEntries = false;

    /// <summary>
    /// Flag indicating whether there are selected playlist tracks
    /// Controls UI behaviour
    /// </summary>
    [ObservableProperty]
    private bool _hasSelectedPlaylistTracks = false;

    /// <summary>
    /// Forces refreshing the value of the HasSelectedLibraryEntries sentinel, triggering NotifyPropertyChanged as appropriate
    /// </summary>
    public void RefreshHasSelectedLibraryEntries()
    {
        bool result = false;
        if (CurrentLibrary is not null)
        {
            foreach (var entry in CurrentLibrary.Entries)
            {
                if (entry.Selected == true)
                {
                    result = true;
                    break;
                }
            }
        }
        HasSelectedLibraryEntries = result;
    }

    partial void OnCurrentLibraryChanged(LibraryViewModel? value)
    {
        HasCurrentLibrary = (value is not null && value.LibraryKey is not null);
        HasNoCurrentLibrary = !HasCurrentLibrary;
        CanSaveLibrary = (value is not null && value.Filespec is not null && value.Filespec.Length > 0);
        RefreshHasSelectedLibraryEntries();
    }

    partial void OnCurrentLibraryEntryChanged(LibraryEntryViewModel? value)
    {
        HasCurrentLibraryEntry = (value is not null && value.LibraryEntryKey is not null);
        HasNoCurrentLibraryEntry = !_hasNoCurrentLibraryEntry;
        RefreshHasSelectedLibraryEntries();
    }

    partial void OnCurrentPlaylistChanged(PlaylistViewModel? value)
    {
        RefreshCurrentPlaylist();
        RefreshPlaybackUI(PlaybackTabUiParts.PlaylistDescription);
    }

    partial void OnCurrentPlaylistTrackChanging(TrackViewModel? oldValue, TrackViewModel? newValue)
    {
        if (oldValue is not null)
        {
            _playbackDevice.Stop();     // If it isn't already.
            oldValue.SetCurrentTrackParentReferences(null, null);
        }
        if (newValue is not null)
        {
            if (newValue.Filespec is not null && System.IO.File.Exists(newValue.Filespec))
            {
                _playbackDevice.Filename = newValue.Filespec;
                newValue.SetCurrentTrackParentReferences(_playbackDevice, this);
            }
        }
    }

    partial void OnCurrentPlaylistTrackChanged(TrackViewModel? value)
    {
        // TODO: Do whatever (else) needs doing when the current track is set (e.g. setting prev/next tracks)
        value?.SetPrayerBookList(Prayerbooks.ToList<BookCodeViewModel>());

        // Figure out what the track index is...
        int trackIndex = int.MinValue;
        if (CurrentPlaylist is not null && value is not null)
        {
            trackIndex = CurrentPlaylist.Tracks.IndexOf(value);
        }
        CurrentTrackIndex = trackIndex;
    }

    partial void OnCurrentTrackIndexChanged(int value)
    {
        AssessAdjacentTrackButtonStatus();
    }

    partial void OnCurrentTrackProgressSliderValueChanged(int value)
    {
        if (CurrentTrackPlaybackCanScrub)
        {
            if (_playbackDevice is not null)
            {
                _playbackDevice.PositionInThousandths = value;
            }
            RefreshPlaybackUI(PlaybackTabUiParts.PositionRemainingOnly);
        }
    }

    /// <summary>
    /// Opens a playlist file (.cibplf) and sets it as the current playlist
    /// </summary>
    /// <param name="filespec">Full path and filespec of the .cibplf file to open</param>
    public void OpenPlaylistFile(string? filespec)
    {
        if (filespec is not null && filespec.Length > 0)
        {
            PlaylistModel? playlistModel = PlaylistModel.LoadPlaylistFile(filespec);
            if (playlistModel != null)
            {
                CurrentPlaylist = new(playlistModel, this);       ////, _prayerBooks);
            }
        }
    }

    /// <summary>
    /// Appends a given library entry to the current playlist as a track
    /// </summary>
    /// <param name="libraryKey">GUID identifier of the library containing the entry</param>
    /// <param name="libraryEntryKey">GUID identifier of the library entry to append</param>
    public void AppendLibraryTrackToCurrentPlaylist(Guid? libraryKey, Guid? libraryEntryKey)
    {
        // Bail out if we're missing data...
        if (libraryKey is null || libraryEntryKey is null || CurrentPlaylist is null)
        {
            return;
        }

        TrackModel newTrackModel = new()
        {
            LibraryKey = (Guid)libraryKey,
            LibraryEntryKey = (Guid)libraryEntryKey
        };

        var newTrackViewModel = new TrackViewModel(newTrackModel, CurrentPlaylist);
        CurrentPlaylist.IncludeTrack(newTrackViewModel);

        RefreshCurrentPlaylist();
    }

    /// <summary>
    /// Updates properties that drive UI contents and behaviours related to the current playlist
    /// </summary>
    public void RefreshCurrentPlaylist()
    {
        HasCurrentPlaylist = (CurrentPlaylist is not null && CurrentPlaylist.Filespec is not null);
        HasNoCurrentPlaylist = !HasCurrentPlaylist;
        CanSavePlaylist = (CurrentPlaylist is not null
                        && CurrentPlaylist.Filespec is not null
                        && CurrentPlaylist.Filespec.Length > 0);
        // Decorate each track with its library properties...
        if (CurrentPlaylist is not null)
        {
            foreach (var track in CurrentPlaylist.Tracks)
            {
                DecoratePlaylistTrack(track);
            }
        }
        RefreshHasSelectedPlaylistTracks();
    }

    /// <summary>
    /// Refreshes the HasSelectedPlaylistTracks flag and fires the INotifyPropertyChanges event as appropriate
    /// </summary>
    public void RefreshHasSelectedPlaylistTracks()
    {
        bool result = false;
        if (CurrentPlaylist is not null)
        {
            foreach (var track in CurrentPlaylist.Tracks)
            {
                if (track.Selected == true)
                {
                    result = true;
                    break;
                }
            }
        }
        HasSelectedPlaylistTracks = result;
    }

    /// <summary>
    /// Decorates a PlaylistViewModel with information from the corresponding Libary Entry
    /// </summary>
    /// <param name="track">The PlaylistViewModel instance to be augmented</param>
    private void DecoratePlaylistTrack(TrackViewModel track)
    {
        string title = "(Unknown track)";
        string rendition = string.Empty;
        string filespec = string.Empty;
        track.PagesBookA = "-";     // Until we find out otherwise.
        track.PagesBookB = "-";
        track.PagesBookC = "-";
        string nominalLength = string.Empty;
        int nominalSeconds = 0;
        // Find the library for the track
        if (track.LibraryKey is not null && OpenLibraries.ContainsKey((Guid)track.LibraryKey))
        {
            var library = OpenLibraries[(Guid)track.LibraryKey];
            if (library is not null)
            {
                foreach (var entry in library.Entries)
                {   
                    if (entry.LibraryEntryKey == track.LibraryEntryKey)
                    {
                        title = entry.Title ?? string.Empty;
                        rendition = entry.Rendition ?? string.Empty;
                        filespec = entry.Filespec ?? string.Empty;
                        nominalLength = Utilities.FormattedDurationFromSeconds(entry.NominalLength);
                        nominalSeconds = entry.NominalLength ?? 0;
                        track.PagesBookA = entry.PagesBookA ?? "-";
                        track.PagesBookB = entry.PagesBookB ?? "-";
                        track.PagesBookC = entry.PagesBookC ?? "-";
                        if (CurrentPlaylist is not null)
                        {
                            track.DefaultBookIndex = CurrentPlaylist.DefaultBook;
                            track.RecalculateShownPages();
                        }
                        break;
                    }    
                }
            }
        }
        track.Title = title;
        track.Rendition = rendition;
        track.Filespec = filespec;
        track.FormattedLength = nominalLength;
        track.NominalLength = nominalSeconds;
    }

    /// <summary>
    /// Composes a description for a track adjacent to the current track (for next|previous purposes)
    /// </summary>
    /// <param name="track">The TrackViewModel of the adjacent track to be described</param>
    /// <returns>a one-line track description in the form: Title (Rendition) page N [n of m] | (none)</returns>
    private static string ComposeAdjacentTrackDescription(TrackViewModel? track)
    {
        string result = "(none)";
        if (track is not null)
        {
            // Compose the parts of the one-line description
            string title = track.Title ?? "(unknown track)";
            string rendition = track.Rendition ?? string.Empty;
            if (rendition.Length > 0)
            {
                rendition = "(" + rendition + ")";
            }
            string page = track.PageAndBookForDisplay;
            string trackPosition = string.Empty;
            if (track.ParentPlaylist is not null)
            {
                int trackNumber = track.ParentPlaylist.Tracks.IndexOf(track) + 1;
                int trackCount = track.ParentPlaylist.Tracks.Count;
                trackPosition = $"[{trackNumber} of {trackCount}]";
            }
            // Assemble the parts...
            result = title
                   + (rendition.Length > 0 ? " " + rendition: string.Empty)
                   + (page.Length > 0 ? " " + page : string.Empty)
                   + (trackPosition.Length > 0 ? " " + trackPosition : string.Empty);
        }
        return result;
    }

    /// <summary>
    /// Reviews the current playlist state and sets the flags that control the appearance and behaviour of various UI elements
    /// especially the ones that control playlist current track index position
    /// </summary>
    internal void AssessAdjacentTrackButtonStatus()
    {
        bool canNext = false;
        bool canPrevious = false;
        if (CurrentPlaylist is not null)
        {
            if (CurrentTrackIndex >= 0)
            {
                CurrentPlaylistTrack = CurrentPlaylist.Tracks[CurrentTrackIndex];
            }
            else
            {
                CurrentPlaylistTrack = null;
            }
            AudioPlayback.PlaybackMode transportMode = AudioPlayback.PlaybackMode.Unloaded;
            if (_playbackDevice is not null)
            {
                transportMode = _playbackDevice.TransportMode;
            }
            canNext = ((CurrentTrackIndex < CurrentPlaylist.Tracks.Count - 1)
                    && (transportMode == AudioPlayback.PlaybackMode.Stopped ||
                        transportMode == AudioPlayback.PlaybackMode.Unloaded));
            canPrevious = ((CurrentTrackIndex > 0)
                    && (transportMode == AudioPlayback.PlaybackMode.Stopped ||
                        transportMode == AudioPlayback.PlaybackMode.Unloaded));
        }
        CanNextTrack = canNext;
        CanPreviousTrack = canPrevious;
        RefreshPlaybackUI(PlaybackTabUiParts.FullPlaybackUI);
    }

    /// <summary>
    /// Refresh the Observable Properties that drive the contents and appearance of the Playback Tab
    /// </summary>
    /// <param name="uiParts">Bit flag enumeration indicating the parts of the UI to refresh</param>
    internal void RefreshPlaybackUI(PlaybackTabUiParts uiParts)
    {
        if (uiParts.HasFlag(PlaybackTabUiParts.PlaylistDescription))
        {
            // Update the current playlist description for the playback tab.
            // What flavour are we getting?
            string playlistDescription = string.Empty;
            if (CurrentPlaylist is null)
            {
                playlistDescription = "(no playlst selected)";
            }
            else
            {
                // Is there a title?
                if (CurrentPlaylist.Title is not null)
                {
                    playlistDescription = CurrentPlaylist.Title;
                }
                // Is there (also?) a filespec?
                if (CurrentPlaylist.Filespec is not null)
                {
                    bool appendParenthetically = CurrentPlaylist.Title is not null;
                    if (appendParenthetically)
                    {
                        playlistDescription += " (";
                    }
                    playlistDescription += System.IO.Path.GetFileName(CurrentPlaylist.Filespec);
                    if (appendParenthetically)
                    {
                        playlistDescription += ")";
                    }
                }
            }
            CurrentPlaylistPlaybackTabDescription = playlistDescription;
        }
        if (uiParts.HasFlag(PlaybackTabUiParts.CurrentTrackDescription))
        {
            string description = "[No track selected]";
            if (CurrentTrackIndex >= 0 && CurrentPlaylist is not null)
            {
                description = CurrentPlaylistTrack?.Title ?? "(Unknown Track)";
                if (CurrentPlaylistTrack is not null)
                {
                    if (CurrentPlaylistTrack.Rendition is not null)
                    {
                        description += " ("
                            + CurrentPlaylistTrack.Rendition + ")";
                    }
                }
            }
            CurrentTrackPlaybackDescription = description;

            string details = "Load a playlist and select a track.";
            if (CurrentTrackIndex >= 0 && CurrentPlaylist is not null && CurrentPlaylistTrack is not null)
            {
                details = string.Empty;
                if (CurrentPlaylistTrack.ShownPages.Length > 0)
                {
                    details += "Page: " + CurrentPlaylistTrack.ShownPages;
                }

                if (CurrentPlaylistTrack.ParentheticalBookForDisplay.Length > 0)
                {
                    details += " " + CurrentPlaylistTrack.ParentheticalBookForDisplay;
                }
                details += "   Duration: " + CurrentPlaylistTrack.FormattedLength
                        + "   [ Track " + (CurrentTrackIndex + 1).ToString()
                            + " of " + CurrentPlaylist.Tracks.Count.ToString() + " ]";
            }
            CurrentTrackPlaybackDescriptionDetails = details;
        }
        if (uiParts.HasFlag(PlaybackTabUiParts.CurrentTrackStatusVisuals))
        {
            AudioPlayback.PlaybackMode transportMode = AudioPlayback.PlaybackMode.Unloaded;
            if (_playbackDevice is not null)
            {
                transportMode = _playbackDevice.TransportMode;
            }
            switch (transportMode)
            {
                case AudioPlayback.PlaybackMode.Unloaded:
                    CurrentTrackBorderBrush = AppResources.DefinedColour(AppResources.StaticResource.NoClipBackgroundBorderBrush);
                    CurrentTrackBorderBackground = AppResources.DefinedColour(AppResources.StaticResource.NoClipBackgroundBorderBrush);
                    CurrentTrackStatusLabel = "(None)";
                    CurrentTrackStatusColour = AppResources.DefinedColour(AppResources.StaticResource.NoClipMainBrush);
                    break;
                case AudioPlayback.PlaybackMode.Stopped:
                    CurrentTrackBorderBrush = AppResources.DefinedColour(AppResources.StaticResource.CuedBackgroundBorderBrush);
                    CurrentTrackBorderBackground = AppResources.DefinedColour(AppResources.StaticResource.CuedBackgroundBrush);
                    CurrentTrackStatusLabel = "Cued:";
                    CurrentTrackStatusColour = AppResources.DefinedColour(AppResources.StaticResource.CuedMainBrush);
                    break;
                case AudioPlayback.PlaybackMode.Playing:
                    CurrentTrackBorderBrush = AppResources.DefinedColour(AppResources.StaticResource.PlayingBackgroundBorderBrush);
                    CurrentTrackBorderBackground = AppResources.DefinedColour(AppResources.StaticResource.PlayingBackgroundBrush);
                    CurrentTrackStatusLabel = "Playing:";
                    CurrentTrackStatusColour = AppResources.DefinedColour(AppResources.StaticResource.PlayingMainBrush);
                    break;
                case AudioPlayback.PlaybackMode.Paused:
                    CurrentTrackBorderBrush = AppResources.DefinedColour(AppResources.StaticResource.CuedBackgroundBorderBrush);
                    CurrentTrackBorderBackground = AppResources.DefinedColour(AppResources.StaticResource.CuedBackgroundBrush);
                    CurrentTrackStatusLabel = "Paused:";
                    CurrentTrackStatusColour = AppResources.DefinedColour(AppResources.StaticResource.CuedMainBrush);
                    break;
                default:
                    break;
            }
        }
        if (uiParts.HasFlag(PlaybackTabUiParts.TransportButtons))
        {
            bool canPlay = false;
            bool canPause = false;
            bool canStop = false;
            AudioPlayback.PlaybackMode transportMode = AudioPlayback.PlaybackMode.Unloaded;

            if (_playbackDevice is not null)
            {
                canPlay = _playbackDevice.CanPlay;
                canPause = _playbackDevice.CanPause;
                canStop = _playbackDevice.CanStop;
                transportMode = _playbackDevice.TransportMode;
            }
            CanPlay = canPlay;
            CanPause = canPause;
            CanStop = canStop;
            CurrentTrackPlaybackCanScrub = (transportMode == AudioPlayback.PlaybackMode.Paused);
            CanPlaylistTab = (transportMode == AudioPlayback.PlaybackMode.Stopped || transportMode == AudioPlayback.PlaybackMode.Unloaded);
            CanLibraryTab = (transportMode == AudioPlayback.PlaybackMode.Stopped || transportMode == AudioPlayback.PlaybackMode.Unloaded);

            // Style the PLAY, PAUSE and STOP buttons appropriately based on their state.
            // When playing, the PLAY button is disabled and the PAUSE/STOP buttons are "playing"
            // When not playing, the PAUSE/STOP buttons are disabled and the PLAY button is "cued"
            switch (transportMode)
            {
                case AudioPlayback.PlaybackMode.Unloaded:
                    PlaybackPlayButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.DisabledMainBrush);
                    PlaybackPlayButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.DisabledContrastBrush);
                    PlaybackPauseButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.DisabledMainBrush);
                    PlaybackPauseButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.DisabledContrastBrush);
                    PlaybackStopButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.DisabledMainBrush);
                    PlaybackStopButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.DisabledContrastBrush);
                    break;
                case AudioPlayback.PlaybackMode.Stopped:
                    PlaybackPlayButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.CuedMainBrush);
                    PlaybackPlayButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.CuedBackgroundBrush);
                    PlaybackPauseButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.DisabledMainBrush);
                    PlaybackPauseButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.DisabledContrastBrush);
                    PlaybackStopButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.DisabledMainBrush);
                    PlaybackStopButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.DisabledContrastBrush);
                    break;
                case AudioPlayback.PlaybackMode.Playing:
                    PlaybackPlayButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.DisabledMainBrush);
                    PlaybackPlayButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.DisabledContrastBrush);
                    PlaybackPauseButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.PlayingMainBrush);
                    PlaybackPauseButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.PlayingBackgroundBrush);
                    PlaybackStopButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.PlayingMainBrush);
                    PlaybackStopButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.PlayingBackgroundBrush);
                    break;
                case AudioPlayback.PlaybackMode.Paused:
                    PlaybackPlayButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.CuedMainBrush);
                    PlaybackPlayButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.CuedBackgroundBrush);
                    PlaybackPauseButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.DisabledMainBrush);
                    PlaybackPauseButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.DisabledContrastBrush);
                    PlaybackStopButtonForeground = AppResources.DefinedColour(AppResources.StaticResource.DisabledMainBrush);
                    PlaybackStopButtonBackground = AppResources.DefinedColour(AppResources.StaticResource.DisabledContrastBrush);
                    break;
                default:
                    break;
            }
        }
        if (uiParts.HasFlag(PlaybackTabUiParts.PlaybackProgress) || uiParts.HasFlag(PlaybackTabUiParts.PositionRemainingOnly))
        {
            if (_playbackDevice is not null &&
                (_playbackDevice.TransportMode == AudioPlayback.PlaybackMode.Playing
                || _playbackDevice.TransportMode == AudioPlayback.PlaybackMode.Paused))
            {
                CurrentTrackPositionFormatted = Utilities.FormattedDurationFromSeconds((int)Math.Round(_playbackDevice.EffectivePositionInSeconds,0));
                CurrentTrackRemainingFormatted = Utilities.FormattedDurationFromSeconds((int)Math.Round(_playbackDevice.EffectiveRemainingInSeconds, 0));
                // Only set the slider if we're not manually scrubbing.
                if (uiParts.HasFlag(PlaybackTabUiParts.PlaybackProgress))
                {
                    CurrentTrackProgressSliderValue = _playbackDevice.PositionInThousandths;
                }
            }
            else
            {
                CurrentTrackPositionFormatted = "0:00";
                if (CurrentPlaylistTrack is not null)
                {
                    CurrentTrackRemainingFormatted = Utilities.FormattedDurationFromSeconds((int)Math.Round(CurrentPlaylistTrack.EffectiveNominalLength, 0)); ;
                }
                else
                {
                    CurrentTrackRemainingFormatted = "0:00";
                }
                CurrentTrackProgressSliderValue = 0;
            }
        }
        if (uiParts.HasFlag(PlaybackTabUiParts.EffectiveSettings))
        {   
            // TODO: Manage the refresh of effective settings UI if something is actuallly needed
        }
        if (uiParts.HasFlag(PlaybackTabUiParts.AdjacentTracks))
        {
            const string noTrack = "(none)";
            if (CurrentPlaylist is null || CurrentTrackIndex < 0)
            {
                PreviousTrackDescription = noTrack;
                NextTrackDescription = noTrack;
            }
            else
            {
                if (CurrentTrackIndex == 0)
                {
                    PreviousTrackDescription = noTrack;
                }
                else
                {
                    PreviousTrackDescription = ComposeAdjacentTrackDescription(CurrentPlaylist.Tracks[CurrentTrackIndex-1]);
                }
                if (CurrentTrackIndex == CurrentPlaylist.Tracks.Count - 1)
                {
                    NextTrackDescription = noTrack;
                }
                else
                {
                    NextTrackDescription = ComposeAdjacentTrackDescription(CurrentPlaylist.Tracks[CurrentTrackIndex + 1]);
                }
            }
        }
    }

    /// <summary>
    /// Retrieves the current transport mode (playing|stopped|paused, etc.) of the main playback device
    /// </summary>
    public AudioPlayback.PlaybackMode TransportMode
    {
        get
        {
            var result = AudioPlayback.PlaybackMode.Unloaded;
            if (_playbackDevice is not null)
            {
                result = _playbackDevice.TransportMode;
            }
            return result;
        }
    }

    /// <summary>
    /// Persists the current library (.ciblib)
    /// </summary>
    [RelayCommand]
    public void SaveCurrentLibrary()
    {
        if (CurrentLibrary is not null && CurrentLibrary.Filespec is not null && CurrentLibrary.Filespec.Length > 0)
        {
            CurrentLibrary.Save();
            CanSaveLibrary = true;
        }
    }

    /// <summary>
    /// Persists the current playlist (.cibplf)
    /// </summary>
    [RelayCommand]
    public void SaveCurrentPlaylist()
    {
        if (CurrentPlaylist is not null && CurrentPlaylist.Filespec is not null && CurrentPlaylist.Filespec.Length > 0)
        {
            CurrentPlaylist.Save();
            CanSavePlaylist = true;
            // Ensure the libary-inherited properties are all repopulated...
            foreach (var track in CurrentPlaylist.Tracks)
            {
                DecoratePlaylistTrack(track);
            }
        }
    }

    /// <summary>
    /// Resets the default Pitch of the Current Playlist to its nominal value
    /// </summary>
    [RelayCommand]
    public void ResetPlaylistDefaultPitch()
    {
        CurrentPlaylist?.ResetDefaultPitch();
    }

    /// <summary>
    /// Resets the default Pan of the Current Playlist to its nominal value
    /// </summary>
    [RelayCommand]
    public void ResetPlaylistDefaultPan()
    {
        CurrentPlaylist?.ResetDefaultPan();
    }


    /// <summary>
    /// Resets the default Tempo of the Current Playlist to its nominal value
    /// </summary>
    [RelayCommand]
    public void ResetPlaylistDefaultTempo()
    {
        CurrentPlaylist?.ResetDefaultTempo();
    }


    /// <summary>
    /// Resets the default Volume of the Current Playlist to its nominal value
    /// </summary>
    [RelayCommand]
    public void ResetPlaylistDefaultVolume()
    {
        CurrentPlaylist?.ResetDefaultVolume();
    }


    /// <summary>
    /// Resets the override Pitch of the Current Playlist Track to its nominal value
    /// </summary>
    [RelayCommand]
    public void ResetPlaylistOverridePitch()
    {
        CurrentPlaylistTrack?.ResetOverridePitch();
    }


    /// <summary>
    /// Resets the override Pan of the Current Playlist Track to its nominal value
    /// </summary>
    [RelayCommand]
    public void ResetPlaylistOverridePan()
    {
        CurrentPlaylistTrack?.ResetOverridePan();
    }


    /// <summary>
    /// Resets the override Tempo of the Current Playlist Track to its nominal value
    /// </summary>
    [RelayCommand]
    public void ResetPlaylistOverrideTempo()
    {
        CurrentPlaylistTrack?.ResetOverrideTempo();
    }


    /// <summary>
    /// Resets the override Volume of the Current Playlist Track to its nominal value
    /// </summary>
    [RelayCommand]
    public void ResetPlaylistOverrideVolume()
    {
        CurrentPlaylistTrack?.ResetOverrideVolume();
    }

    /// <summary>
    /// Navigates the playlist from the current track to the next track in the playlist, making it the new current track
    /// </summary>
    [RelayCommand]
    public void NextPlaybackTrack()
    {
        if (CurrentPlaylist is not null)
        {
            if (CurrentTrackIndex < CurrentPlaylist.Tracks.Count - 1)
            {
                CurrentTrackIndex++;
            }
        }
    }

    /// <summary>
    /// Navigates the playlist from the current track to the previous track in the playlist, making it the new current track
    /// </summary>
    [RelayCommand]
    public void PreviousPlaybackTrack()
    {
        if (CurrentPlaylist is not null)
        {
            if (CurrentTrackIndex > 0)
            {
                CurrentTrackIndex--;
            }
        }
    }
}
