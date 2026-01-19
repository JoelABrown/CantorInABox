using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mooseware.CantorInABox.Models;
using System.Windows;
using System.Windows.Threading;

namespace Mooseware.CantorInABox.ViewModels;

/// <summary>
/// View model for playlist tracks, representing an individual audio file with playback settings included
/// </summary>
public partial class TrackViewModel : ObservableObject
{
    /// <summary>
    /// Timer which ticks during playback allowing tracking of progress through the audio file
    /// </summary>
    private readonly DispatcherTimer _playbackClockTimer = new();

    /// <summary>
    /// The GUID identifying the library from which the track was included in a playlist
    /// </summary>
    [ObservableProperty]
    private Guid? _libraryKey;
    /// <summary>
    /// The GUID identifying the library entry which provides information about this track
    /// </summary>
    [ObservableProperty]
    private Guid? _libraryEntryKey;
    /// <summary>
    /// Whether or not to use the local Pitch override (as opposed to the playlist default value) for playback
    /// </summary>
    [ObservableProperty]
    private bool _usePitchOverride = false;
    /// <summary>
    /// The locally applicable Pitch setting to use for playback (assuming the use override flag is on)
    /// </summary>
    [ObservableProperty]
    private int? _pitchOverride;
    /// <summary>
    /// Whether or not to use the local Tempo override (as opposed to the playlist default value) for playback
    /// </summary>
    [ObservableProperty]
    private bool _useTempoOverride = false;
    /// <summary>
    /// The locally applicable Tempo setting to use for playback (assuming the use override flag is on)
    /// </summary>
    [ObservableProperty]
    private double? _tempoOverride;
    /// <summary>
    /// Whether or not to use the local Pan override (as opposed to the playlist default value) for playback
    /// </summary>
    [ObservableProperty]
    private bool _usePanOverride = false;
    /// <summary>
    /// The locally applicable Pan setting to use for playback (assuming the use override flag is on)
    /// </summary>
    [ObservableProperty]
    private double? _panOverride;
    /// <summary>
    /// Whether or not to use the local Volume override (as opposed to the playlist default value) for playback
    /// </summary>
    [ObservableProperty]
    private bool _useVolumeOverride = false;
    /// <summary>
    /// The locally applicable Volume setting to use for playback (assuming the use override flag is on)
    /// </summary>
    [ObservableProperty]
    private double? _volumeOverride;
    /// <summary>
    /// Flag indicating whether or not this track is selected on the Playlist tab
    /// </summary>
    [ObservableProperty]
    private bool _selected = false;
    /// <summary>
    /// Flag indicating whether or not this track is currently being previewed on the Playlist tab.
    /// Used to drive visual feedback in the UI
    /// </summary>
    [ObservableProperty]
    private bool _previewing = false;

    // These properties have to be looked up in the library
    // and set by the code that instantiates this object.
    // ----------------------------------------------------

    /// <summary>
    /// Track title for the audio
    /// </summary>
    [ObservableProperty]
    private string? _title;
    /// <summary>
    /// The version (e.g. composer) of the audio track
    /// </summary>
    [ObservableProperty]
    private string? _rendition;
    /// <summary>
    /// Full path and filespec of the audio file
    /// </summary>
    [ObservableProperty]
    private string? _filespec;
    /// <summary>
    /// Default prayer book index in use on the playlist containing this track
    /// Used to select the pages (A, B, or C). Unknown/TBD when = 0.
    /// </summary>
    [ObservableProperty]
    private int _defaultBookIndex = 0;
    /// <summary>
    /// The page(s) that this track appears on in the first prayer book
    /// </summary>
    [ObservableProperty]
    private string? _pagesBookA;
    /// <summary>
    /// The page(s) that this track appears on in the second prayer book
    /// </summary>
    [ObservableProperty]
    private string? _pagesBookB;
    /// <summary>
    /// The page(s) that this track appears on in the third prayer book
    /// </summary>
    [ObservableProperty]
    private string? _pagesBookC;
    /// <summary>
    /// The length of the audio track, at original speed, expressed in minutes and seconds format
    /// </summary>
    [ObservableProperty]
    private string? _formattedLength;
    /// <summary>
    /// Visible when this track has any playback setting overrides active, Hidden when only defaults are in use
    /// Used in the Playlist tab to show an icon on the track listing to provide a quick visual reference
    /// </summary>
    [ObservableProperty]
    private Visibility _trackTweaksVisibility = Visibility.Hidden;

    // These properties are inherited from the parent PlaylistViewModel
    // or are calculated based on parent and native properties
    // ----------------------------------------------------------------

    /// <summary>
    /// Reference to the parent Playlist View Model used to get applicable 
    /// information tracked at that level
    /// </summary>
    public PlaylistViewModel? ParentPlaylist { get; set; }

    /// <summary>
    /// The effective Pitch playback setting, either the local override or the playlist default as applicable
    /// </summary>
    [ObservableProperty]
    private int? _effectivePitch;
    /// <summary>
    /// The effective Tempo playback setting, either the local override or the playlist default as applicable
    /// </summary>
    [ObservableProperty]
    private double? _effectiveTempo;
    /// <summary>
    /// The effective Pan playback setting, either the local override or the playlist default as applicable
    /// </summary>
    [ObservableProperty]
    private double? _effectivePan;
    /// <summary>
    /// The effective Volume playback setting, either the local override or the playlist default as applicable
    /// </summary>
    [ObservableProperty]
    private double? _effectiveVolume;
    /// <summary>
    /// The active page number entry, based on the playlist's default prayer book choice
    /// </summary>
    [ObservableProperty]
    private string _pageAndBookForDisplay = string.Empty;
    /// <summary>
    /// The effective Pitch playback setting value description to show next to the slider in the UI
    /// </summary>
    [ObservableProperty]
    private string _effectivePitchDescription = string.Empty;
    /// <summary>
    /// The effective Tempo playback setting value description to show next to the slider in the UI
    /// </summary>
    [ObservableProperty]
    private string _effectiveTempoDescription = string.Empty;
    /// <summary>
    /// The effective Pan playback setting value description to show next to the slider in the UI
    /// </summary>
    [ObservableProperty]
    private string _effectivePanDescription = string.Empty;
    /// <summary>
    /// The effective Volume playback setting value description to show next to the slider in the UI
    /// </summary>
    [ObservableProperty]
    private string _effectiveVolumeDescription = string.Empty;


    // These properties are derived from other properties
    // --------------------------------------------------

    /// <summary>
    /// The page(s) from the selected prayer book without any other adornment
    /// </summary>
    [ObservableProperty]
    private string _shownPages = "-";
    /// <summary>
    /// The name of the selected prayer book enclosed in parentheses for display
    /// </summary>
    [ObservableProperty]
    private string _parentheticalBookForDisplay = string.Empty;
    /// <summary>
    /// Flag indicating whether the track has any playback setting overrides in effect
    /// </summary>
    private bool _hasTrackTweaks = false;
    /// <summary>
    /// The underlying model of the Track
    /// </summary>
    private readonly TrackModel? _model;
    /// <summary>
    /// The audio playback device used to play the audio file with playback settings applied
    /// </summary>
    private AudioPlayback? _playbackDevice = null;
    /// <summary>
    /// A reference to the Prime View Model in order to get information determined at that level
    /// </summary>
    private PrimeViewModel? _primeViewModel = null;

    // These properties are related to the Audio Playback device's current state
    // -------------------------------------------------------------------------

    /// <summary>
    /// Flag indicating whether the track can currently be played
    /// </summary>
    [ObservableProperty]
    private bool _canPlay = false;
    /// <summary>
    /// Flag indicating whether the track can currently be paused
    /// </summary>
    [ObservableProperty]
    private bool _canPause = false;
    /// <summary>
    /// Flag indicating whether the track can currently be stopped
    /// </summary>
    [ObservableProperty]
    private bool _canStop = false;

    /// <summary>
    /// The Rendition of the track prepended with a hyphen and a space - used for display
    /// </summary>
    public string PunctuatedRendition
    {
        get
        {
            string result = string.Empty;
            if (Rendition is not null && Rendition.Length > 0)
            {
                result = "- " + Rendition;
            }
            return result;
        }
    }
    /// <summary>
    /// The list of prayer books in order to get names for display
    /// </summary>
    private List<BookCodeViewModel> _prayerBooks = [];

    /// <summary>
    /// Establish a new instance and set key properties
    /// </summary>
    public TrackViewModel()     
    {
        SyncFromModel();
        UpdateHasTrackTweaks();
        EstablishPlaybackClockTimer();
    }
    /// <summary>
    /// Establish a new instance and set key properties
    /// </summary>
    /// <param name="model">Reference to the TrackModel to bind to this view model</param>
    /// <param name="playlist">Reference to the containing play list view model</param>
    public TrackViewModel(TrackModel model, PlaylistViewModel playlist)
    {
        _model = model;
        ParentPlaylist = playlist;
        SyncFromModel();
        UpdateHasTrackTweaks();
        EstablishPlaybackClockTimer();
    }

    /// <summary>
    /// Constructor used for including a Library Entry as a track on a playlist
    /// </summary>
    /// <param name="libraryKey">The ID of the Library from which the Entry comes</param>
    /// <param name="libraryEntryKey">The ID of the Entry itself</param>
    public TrackViewModel(Guid libraryKey, Guid libraryEntryKey, PlaylistViewModel playlist)
    {
        _model = new();
        ParentPlaylist = playlist;
        LibraryKey = libraryKey;
        LibraryEntryKey = libraryEntryKey;
        EstablishPlaybackClockTimer();
    }

    /// <summary>
    /// Sets a reference to the list of prayer books in order to describe page(s) for the track
    /// </summary>
    /// <param name="prayerBooks"></param>
    public void SetPrayerBookList(List<BookCodeViewModel> prayerBooks)
    {
        _prayerBooks = prayerBooks;
        RecalculateShownPages();
    }

    /// <summary>
    /// Allows references to the playback device and the prime view model, if these are not established earlier
    /// </summary>
    /// <param name="audioPlayback"></param>
    /// <param name="primeViewModel"></param>
    public void SetCurrentTrackParentReferences(AudioPlayback? audioPlayback, PrimeViewModel? primeViewModel)
    {
        _playbackDevice = audioPlayback;
        _primeViewModel = primeViewModel;
        RefreshTransportFlags();
        ////RefreshCurrentTrackVisuals();
    }
    /// <summary>
    /// Provides access to the underlying model of this track
    /// </summary>
    internal TrackModel? Model { get { return _model; } }

    /// <summary>
    /// Sets the values in the view model based on the underlying model 
    /// (triggering updates to observable properties in the process)
    /// </summary>
    private void SyncFromModel()
    {
        this.LibraryKey = _model?.LibraryKey;
        this.LibraryEntryKey = _model?.LibraryEntryKey;
        this.UsePitchOverride = _model?.UsePitchOverride ?? false;
        this.PitchOverride = _model?.PitchOverride;
        this.UseTempoOverride = _model?.UseTempoOverride ?? false;
        this.TempoOverride = _model?.TempoOverride;
        this.UsePanOverride = _model?.UsePanOverride ?? false;
        this.PanOverride = _model?.PanOverride;
        this.UseVolumeOverride = _model?.UseVolumeOverride ?? false;
        this.VolumeOverride = _model?.VolumeOverride * 100.0;
    }

    /// <summary>
    /// Wires up and configures a timer that is used while the track is being played
    /// </summary>
    private void EstablishPlaybackClockTimer()
    {
        _playbackClockTimer.Tick += new EventHandler(PlaybackClockTimer_Tick);
        _playbackClockTimer.Interval = new TimeSpan(0, 0, 0, 0, 100); // tenth of a second.
        _playbackClockTimer.Stop();
    }

    /// <summary>
    /// Provide visual feedback during playback of the track
    /// </summary>
    private void PlaybackClockTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            RefreshTransportFlags();
            _primeViewModel?.RefreshPlaybackUI
                    ( PlaybackTabUiParts.PlaybackProgress
                    | PlaybackTabUiParts.TransportButtons);
        }
        catch (Exception ex)
        {   // For debugging only...
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }

    partial void OnLibraryKeyChanged(Guid? value)
    {
        if (_model is not null)
        {
            _model.LibraryKey = value ?? new Guid();
        }
    }

    partial void OnLibraryEntryKeyChanged(Guid? value)
    {
        if (_model is not null)
        {
            _model.LibraryEntryKey = value ?? new Guid();
        }
    }

    partial void OnUsePitchOverrideChanged(bool value)
    {
        if (_model is not null)
        {
            _model.UsePitchOverride = value;
        }
        UpdateHasTrackTweaks();
        // Determine the effective value...
        if (value)
        {
            EffectivePitch = PitchOverride;
        }
        else
        {
            EffectivePitch = ParentPlaylist?.PitchDefault ?? AudioPlayback.PitchDefault;
        }
        EffectivePitchDescription = ViewModelUtilities.ComposePitchDescription(EffectivePitch);
    }

    partial void OnPitchOverrideChanged(int? value)
    {
        int vettedValue = AudioPlayback.PitchDefault;
        if (ParentPlaylist is not null && ParentPlaylist.ParentViewModel is not null)
        {
            vettedValue = Math.Max(ParentPlaylist.ParentViewModel.PitchFloor,
                          Math.Min(ParentPlaylist.ParentViewModel.PitchCeiling, (value ?? AudioPlayback.PitchDefault)));
        }
        if (_model is not null)
        {
            _model.PitchOverride = vettedValue;
        }
        // Synchronize the effective value if appropriate...
        if (UsePitchOverride)
        {
            EffectivePitch = vettedValue;
        }
        else
        {
            EffectivePitch = ParentPlaylist?.PitchDefault ?? AudioPlayback.PitchDefault;
        }
        EffectivePitchDescription = ViewModelUtilities.ComposePitchDescription(EffectivePitch);
    }

    partial void OnEffectivePitchChanged(int? value)
    {
        if (UsePitchOverride)
        {
            PitchOverride = value;
        }
        if (_playbackDevice is not null)
        {
            _playbackDevice.Pitch = value ?? AudioPlayback.PitchDefault;
        }
        EffectivePitchDescription = ViewModelUtilities.ComposePitchDescription(value);
    }

    partial void OnUseTempoOverrideChanged(bool value)
    {
        if (_model is not null)
        {
            _model.UseTempoOverride = value;
        }
        UpdateHasTrackTweaks();
        // Determine the effective value...
        if (value)
        {
            EffectiveTempo = TempoOverride;
        }
        else
        {
            EffectiveTempo = ParentPlaylist?.TempoDefault ?? AudioPlayback.TempoDefault;
        }
        EffectiveTempoDescription = ViewModelUtilities.ComposeTempoDescription(EffectiveTempo);
    }

    partial void OnTempoOverrideChanged(double? value)
    {
        double vettedValue = AudioPlayback.TempoDefault;
        if (ParentPlaylist is not null && ParentPlaylist.ParentViewModel is not null)
        {
            vettedValue = Math.Max(ParentPlaylist.ParentViewModel.TempoFloor,
                          Math.Min(ParentPlaylist.ParentViewModel.TempoCeiling, (value ?? AudioPlayback.TempoDefault)));
        }
        if (_model is not null)
        {
            _model.TempoOverride = vettedValue;
        }
        // Synchronize the effective value if appropriate...
        if (UseTempoOverride)
        {
            EffectiveTempo = vettedValue;
        }
        else
        {
            EffectiveTempo = ParentPlaylist?.TempoDefault ?? AudioPlayback.TempoDefault;
        }
        EffectiveTempoDescription = ViewModelUtilities.ComposeTempoDescription(EffectiveTempo);
    }

    partial void OnEffectiveTempoChanged(double? value)
    {
        if (UseTempoOverride)
        {
            TempoOverride = value;
        }
        if (_playbackDevice is not null)
        {
            _playbackDevice.Tempo = value ?? AudioPlayback.TempoDefault;
            _primeViewModel?.RefreshPlaybackUI(PlaybackTabUiParts.PlaybackProgress);
        }
        EffectiveTempoDescription = ViewModelUtilities.ComposeTempoDescription(value);
    }

    partial void OnUsePanOverrideChanged(bool value)
    {
        if (_model is not null)
        {
            _model.UsePanOverride = value;
        }
        UpdateHasTrackTweaks();
        // Determine the effective value...
        if (value)
        {
            EffectivePan = PanOverride;
        }
        else
        {
            EffectivePan = ParentPlaylist?.PanDefault ?? AudioPlayback.PanDefault;
        }
        EffectivePanDescription = ViewModelUtilities.ComposePanDescription(EffectivePan);
    }

    partial void OnPanOverrideChanged(double? value)
    {
        double vettedValue = AudioPlayback.PanDefault;
        if (ParentPlaylist is not null && ParentPlaylist.ParentViewModel is not null)
        {
            vettedValue = Math.Max(ParentPlaylist.ParentViewModel.PanFloor,
                         Math.Min(ParentPlaylist.ParentViewModel.PanCeiling, (value ?? AudioPlayback.PanDefault)));
        }
        if (_model is not null)
        {
            _model.PanOverride = vettedValue;
        }
        // Synchronize the effective value if appropriate...
        if (UsePanOverride)
        {
            EffectivePan = vettedValue;
        }
        else
        {
            EffectivePan = ParentPlaylist?.PanDefault ?? AudioPlayback.PanDefault;
        }
        EffectivePanDescription = ViewModelUtilities.ComposePanDescription(EffectivePan);
    }

    partial void OnEffectivePanChanged(double? value)
    {
        if (UsePanOverride)
        {
            PanOverride = value;
        }
        if (_playbackDevice is not null)
        {
            _playbackDevice.Pan = (float)(value ?? AudioPlayback.PanDefault);
        }
        EffectivePanDescription = ViewModelUtilities.ComposePanDescription(value);
    }

    partial void OnUseVolumeOverrideChanged(bool value)
    {
        if (_model is not null)
        {
            _model.UseVolumeOverride = value;
        }
        UpdateHasTrackTweaks();
        // Determine the effective value...
        if (value)
        {
            EffectiveVolume = VolumeOverride;
        }
        else
        {
            EffectiveVolume = ParentPlaylist?.VolumeDefault ?? AudioPlayback.VolumeDefault * 100.0;
        }
        EffectiveVolumeDescription = ViewModelUtilities.ComposeVolumeDescription(EffectiveVolume);
    }

    partial void OnVolumeOverrideChanged(double? value)
    {
        double vettedValue = AudioPlayback.VolumeDefault * 100.0;
        if (ParentPlaylist is not null && ParentPlaylist.ParentViewModel is not null)
        {
            vettedValue = Math.Max(ParentPlaylist.ParentViewModel.VolumeFloor,
                          Math.Min(ParentPlaylist.ParentViewModel.VolumeCeiling, (value ?? AudioPlayback.VolumeDefault * 100.0)));
        }
        if (_model is not null)
        {
            if (value is not null)
            {
                _model.VolumeOverride = vettedValue / 100.0;
            }
            else
            {
                _model.VolumeOverride = AudioPlayback.VolumeDefault * 100.0;
            }
        }
        // Synchronize the effective value if appropriate...
        if (UseVolumeOverride)
        {
            EffectiveVolume = vettedValue;
        }
        else
        {
            EffectiveVolume = ParentPlaylist?.VolumeDefault ?? AudioPlayback.VolumeDefault * 100.0;
        }
        EffectiveVolumeDescription = ViewModelUtilities.ComposeVolumeDescription(EffectiveVolume);
    }

    partial void OnEffectiveVolumeChanged(double? value)
    {
        if (UseVolumeOverride)
        {
            VolumeOverride = value;
        }
        if (_playbackDevice is not null)
        {
            _playbackDevice.Volume = (float)((value ?? AudioPlayback.VolumeDefault * 100.0) / 100.0);
        }
        EffectiveVolumeDescription = ViewModelUtilities.ComposeVolumeDescription(value);
    }

    /// <summary>
    /// Examine the current track settings and set or reset the flag that indicates any overrides are in use accordingly
    /// </summary>
    private void UpdateHasTrackTweaks()
    {
        bool originalValue = _hasTrackTweaks;
        bool newValue = UsePitchOverride || UsePitchOverride || UseTempoOverride || UseVolumeOverride;

        if (originalValue != newValue)
        {
            _hasTrackTweaks = newValue;
            if (_hasTrackTweaks)
            {
                TrackTweaksVisibility = Visibility.Visible;
            }
            else
            {
                TrackTweaksVisibility = Visibility.Hidden;
            }
        }
    }

    /// <summary>
    /// Determine what page(s) to show based on the selected default prayer book in the containing playlist
    /// </summary>
    public void RecalculateShownPages()
    {
        ShownPages = ParentPlaylist?.DefaultBook switch
        {
            0 => PagesBookA ?? "-",
            1 => PagesBookB ?? "-",
            2 => PagesBookC ?? "-",
            _ => PagesBookA ?? "-",
        };

        if (ShownPages == "-")
        {
            ParentheticalBookForDisplay = string.Empty;
        }
        else
        {
            // Append the book name parenthetically
            string bookName = "(unknown book)";
            if (_prayerBooks.Count == 3 && (ParentPlaylist?.DefaultBook ?? -1) >= 0)
            {
                bookName = "(" + _prayerBooks[ParentPlaylist!.DefaultBook] + ")";
            }
            ParentheticalBookForDisplay = bookName;
        }
    }

    partial void OnDefaultBookIndexChanged(int value)
    {
        switch (value)
        {
            case 0:
                ShownPages = PagesBookA ?? "-"; 
                break;
            case 1:
                ShownPages = PagesBookB ?? "-"; 
                break;
            case 2:
                ShownPages = PagesBookC ?? "-"; 
                break;
            default:
                ShownPages = PagesBookA ?? "-";
                break;
        }
    }

    /// <summary>
    /// The length (in seconds) of the audio track when played at its original speed
    /// </summary>
    public double NominalLength { get; set; } = 0.0;

    /// <summary>
    /// The length (in seconds) of the audio track when played at the EffectiveTempo
    /// </summary>
    public double EffectiveNominalLength
    { 
        get 
        {
            double effective = NominalLength / (double)(((EffectiveTempo ?? 100.0) / 100.0));
            return effective; 
        } 
    }

    /// <summary>
    /// Reset the override pitch to the default value from the model
    /// </summary>
    public void ResetOverridePitch()
    {
        _model?.ResetOverridePitch();
        this.PitchOverride = _model?.PitchOverride;
        OnPropertyChanged(nameof(PitchOverride));
    }

    /// <summary>
    /// Reset the override pan to the default value from the model
    /// </summary>
    public void ResetOverridePan()
    {
        _model?.ResetOverridePan();
        this.PanOverride = _model?.PanOverride;
        OnPropertyChanged(nameof(PanOverride));
    }

    /// <summary>
    /// Reset the override tempo to the default value from the model
    /// </summary>
    public void ResetOverrideTempo()
    {
        _model?.ResetOverrideTempo();
        this.TempoOverride = _model?.TempoOverride;
        OnPropertyChanged(nameof(TempoOverride));
    }

    /// <summary>
    /// Reset the override volume to the default value from the model
    /// </summary>
    public void ResetOverrideVolume()
    {
        _model?.ResetOverrideVolume();
        this.VolumeOverride = _model?.VolumeOverride * 100.0;
        OnPropertyChanged(nameof(VolumeOverride));
    }

    /// <summary>
    /// Reassess the transport status flags based on the current state of the track and the playback device
    /// This will result in calls to the appropriate INotifyPropertyChanged events
    /// </summary>
    private void RefreshTransportFlags()
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
        // If we're not playing, we don't need the playback timer running.
        if (transportMode != AudioPlayback.PlaybackMode.Playing)
        {
            _playbackClockTimer.Stop();
        }
        // Also refresh the visuals in the UI...
        _primeViewModel?.RefreshPlaybackUI(PlaybackTabUiParts.TransportButtons
            | PlaybackTabUiParts.CurrentTrackStatusVisuals);
    }

    /// <summary>
    /// Begin playback of the track
    /// </summary>
    [RelayCommand]
    public void PlayCurrentTrack()
    {
        _playbackDevice?.Play();
        _primeViewModel?.AssessAdjacentTrackButtonStatus();
        // First tick will call RefreshTransportFlags()
        _playbackClockTimer.Start();
    }

    /// <summary>
    /// Stop playback of the track
    /// </summary>
    [RelayCommand]
    public void StopCurrentTrack()
    {
        _playbackDevice?.Stop();
        _playbackClockTimer.Stop();
        _primeViewModel?.AssessAdjacentTrackButtonStatus();
        RefreshTransportFlags();
    }

    /// <summary>
    /// Pause playback of the track
    /// </summary>
    [RelayCommand]
    public void PauseCurrentTrack()
    {
        _playbackDevice?.Pause();
        _playbackClockTimer.Stop();
        _primeViewModel?.AssessAdjacentTrackButtonStatus();
        RefreshTransportFlags();
    }
}
