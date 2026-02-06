using CommunityToolkit.Mvvm.ComponentModel;
using Mooseware.CantorInABox.Configuration;
using Mooseware.CantorInABox.Models;
using System.Collections.ObjectModel;

namespace Mooseware.CantorInABox.ViewModels;

/// <summary>
/// View model for a Playlist (.cibplf) file containing a list of tracks representing audio files to be performed 
/// at a particular place and time in a particular sequence.
/// </summary>
public partial class PlaylistViewModel : ObservableObject
{
    /// <summary>
    /// The file specification of the playlist file (.cibplf)
    /// </summary>
    [ObservableProperty]
    private string? _filespec;
    /// <summary>
    /// Short descriptive name for the playlist
    /// </summary>
    [ObservableProperty]
    private string? _title;
    /// <summary>
    /// Optional more detailed description of the purpose and contents of the playlist
    /// </summary>
    [ObservableProperty]
    private string? _description;
    /// <summary>
    /// Index of the prayerbook to be used with this playlist (1,2,3. 0=>TBD)
    /// </summary>
    [ObservableProperty]
    private int _defaultBook = 0;
    /// <summary>
    /// Pitch setting to use with all tracks in the playlist, unless overridden at the track level or during live playback
    /// </summary>
    [ObservableProperty]
    private int? _pitchDefault;
    /// <summary>
    /// Tempo setting to use with all tracks in the playlist, unless overridden at the track level or during live playback
    /// </summary>
    [ObservableProperty]
    private double? _tempoDefault;
    /// <summary>
    /// Pan setting to use with all tracks in the playlist, unless overridden at the track level or during live playback
    /// </summary>
    [ObservableProperty]
    private double? _panDefault;
    /// <summary>
    /// Volume setting to use with all tracks in the playlist, unless overridden at the track level or during live playback
    /// </summary>
    [ObservableProperty]
    private double? _volumeDefault;
    /// <summary>
    /// Observable calculated description of the Pitch setting for the UI
    /// </summary>
    [ObservableProperty]
    private string? _defaultPitchDescription;
    /// <summary>
    /// Observable calculated description of the Tempo setting for the UI
    /// </summary>
    [ObservableProperty]
    private string? _defaultTempoDescription;
    /// <summary>
    /// Observable calculated description of the Pan setting for the UI
    /// </summary>
    [ObservableProperty]
    private string? _defaultPanDescription;
    /// <summary>
    /// Observable calculated description of the Volume setting for the UI
    /// </summary>
    [ObservableProperty]
    private string? _defaultVolumeDescription;

    /// <summary>
    /// The list of tracks which comprise the playliset
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TrackViewModel> _tracks = [];

    /// <summary>
    /// The underlying model for this view model
    /// </summary>
    private readonly PlaylistModel? _model;

    /// <summary>
    /// Internal reference to the PrimeViewModel which contains this view model 
    /// (particularly when it represents the CurrentPlaylist in the main view model)
    /// </summary>
    private readonly PrimeViewModel? _containingPrimeViewModel = null;

    /// <summary>
    /// Externally accessible reference to the PrimeViewModel which contains this view model 
    /// (particularly when it represents the CurrentPlaylist in the main view model)
    /// </summary>
    public PrimeViewModel? ParentViewModel { get => _containingPrimeViewModel; }

    /// <summary>
    /// Application settings used to configure the app at startup
    /// </summary>
    private readonly AppSettings _appSettings;

    partial void OnFilespecChanged(string? value)
    {
        if (_model is not null)
        {
            _model.Filespec = value ?? string.Empty;
        }
    }

    partial void OnTitleChanged(string? value)
    {
        if (_model is not null)
        {
            _model.Title = value ?? string.Empty;
        }
    }
    
    partial void OnDescriptionChanged(string? value)
    {
        if (_model is not null)
        {
            _model.Description = value ?? string.Empty;
        }
    }

    partial void OnDefaultBookChanged(int value)
    {
        if (_model is not null)
        {
            _model.DefaultBook = value;
            // Adjust the value on all playlist tracks as well...
            foreach (var track in _tracks)
            {
                track.DefaultBookIndex = value;
            }
        }
    }

    partial void OnPitchDefaultChanged(int? value)
    {
        if (_model is not null)
        {
            if (_containingPrimeViewModel is not null)
            {
                _model.PitchDefault = Math.Max(_containingPrimeViewModel.PitchFloor,
                                      Math.Min(_containingPrimeViewModel.PitchCeiling, (value ?? AudioPlayback.PitchDefault)));
            }
            else
            {
                _model.PitchDefault = AudioPlayback.PitchDefault;
            }
            DefaultPitchDescription = ViewModelUtilities.ComposePitchDescription(_model.PitchDefault);
        }
    }

    partial void OnTempoDefaultChanged(double? value)
    {
        if (_model is not null)
        {
            if (_containingPrimeViewModel is not null)
            {
                _model.TempoDefault = Math.Max(_containingPrimeViewModel.TempoFloor,
                                      Math.Min(_containingPrimeViewModel.TempoCeiling, (value ?? AudioPlayback.TempoDefault)));
            }
            else
            {
                _model.TempoDefault = AudioPlayback.TempoDefault;
            }
            DefaultTempoDescription = ViewModelUtilities.ComposeTempoDescription(_model.TempoDefault);
        }
    }

    partial void OnPanDefaultChanged(double? value)
    {
        if (_model is not null)
        {
            if (_containingPrimeViewModel is not null)
            {
                _model.PanDefault = Math.Max(_containingPrimeViewModel.PanFloor,
                                    Math.Min(_containingPrimeViewModel.PanCeiling, (value ?? _appSettings.PanDefault)));
            }
            else
            {
                _model.PanDefault = _appSettings?.PanDefault ?? AudioPlayback.PanDefaultFallback;
            }
            DefaultPanDescription = ViewModelUtilities.ComposePanDescription(_model.PanDefault);
        }
    }

    partial void OnVolumeDefaultChanged(double? value)
    {
        if (_model is not null)
        {
            if (_containingPrimeViewModel is not null)
            {
                _model.VolumeDefault = Math.Max(_containingPrimeViewModel.VolumeFloor,
                                       Math.Min(_containingPrimeViewModel.VolumeCeiling, value ?? (_appSettings.VolumeDefault)));
            }
            else
            {
                _model.VolumeDefault = _appSettings?.VolumeDefault ?? AudioPlayback.VolumeDefaultFallback;
            }
            DefaultVolumeDescription = ViewModelUtilities.ComposeVolumeDescription(_model.VolumeDefault);
        }
    }

    /// <summary>
    /// Includes an individual TrackViewModel in this playlist
    /// </summary>
    /// <param name="trackViewModel">TrackViewModel containing the track to be included</param>
    public void IncludeTrack(TrackViewModel trackViewModel)
    {
        if (_model is not null && trackViewModel.Model is not null)
        {
            _model.Tracks.Add(trackViewModel.Model);
            this.Tracks.Add(trackViewModel);
        }
    }

    /// <summary>
    /// Excludes a TrackViewModel from this playlist
    /// </summary>
    /// <param name="trackViewModel">TrackViewModel containing the track to be excluded from the playlist</param>
    public void ExcludeTrack(TrackViewModel trackViewModel)
    {
        if (_model is not null && trackViewModel.Model is not null)
        {
            _model.Tracks.Remove(trackViewModel.Model);
            this.Tracks.Remove(trackViewModel);
        }
    }

    /// <summary>
    /// Physically persists the playlist in a playlist file (.cibplf) based on the current value of the Filespec property
    /// </summary>
    public void Save()
    {
        if (_model is not null)
        {
            _model.Tracks.Clear();
            foreach (var track in Tracks)
            {
                _model.Tracks.Add(track.Model!);
            }
            PlaylistModel.SavePlaylistFile(_model, _model.Filespec);
            SyncFromModel();
        }
    }

    /// <summary>
    /// Physically persists the playlist in a .plf file with a given full path and filespec
    /// </summary>
    /// <param name="filespec">The full path and filespec into which the playlist is to be saved</param>
    public void SaveAs(string filespec)
    {
        if (_model is not null)
        {
            PlaylistModel.SavePlaylistFile(_model, filespec);
            SyncFromModel();
        }

    }

    /// <summary>
    /// Create a new default instance of the PlaylistViewModel
    /// </summary>
    public PlaylistViewModel(AppSettings appSettings)
    {
        _appSettings = appSettings;

    }

    /// <summary>
    /// Create a new instance of the PlaylistViewModel, setting the underlying model and the reference to the containing PrimeViewModel
    /// </summary>
    public PlaylistViewModel(PlaylistModel model, PrimeViewModel? containingPrimeViewModel, AppSettings appSettings)
    {
        _model = model;
        _model.SetAppSettings(appSettings);
        _appSettings = appSettings;
        _containingPrimeViewModel = containingPrimeViewModel;
        SyncFromModel();
    }

    /// <summary>
    /// Sets the values in the view model based on the underlying model 
    /// (triggering updates to observable properties in the process)
    /// </summary>
    private void SyncFromModel()
    {
        this.Filespec = _model?.Filespec;
        OnPropertyChanged(nameof(Filespec));
        this.Title = _model?.Title;
        OnPropertyChanged(nameof(Title));
        this.Description = _model?.Description;
        OnPropertyChanged(nameof(Description));
        this.PitchDefault = _model?.PitchDefault;
        OnPropertyChanged(nameof(PitchDefault));
        this.VolumeDefault = _model?.VolumeDefault;
        OnPropertyChanged(nameof(VolumeDefault));
        this.PanDefault = _model?.PanDefault;
        OnPropertyChanged(nameof(PanDefault));
        this.TempoDefault = _model?.TempoDefault;
        OnPropertyChanged(nameof(TempoDefault));
        
        this.Tracks.Clear();
        if (_model is not null)
        {
            foreach (var track in _model.Tracks)
            {
                TrackViewModel viewModel = new(track, this, _appSettings);
                this.Tracks.Add(viewModel);
            }
        }

        // Important: Do this after the tracks are loaded because it affects track properties...
        this.DefaultBook = _model?.DefaultBook ?? 0;
        OnPropertyChanged(nameof(DefaultBook));
    }

    /// <summary>
    /// Resets the Pitch to the default value provided by the model.
    /// </summary>
    public void ResetDefaultPitch()
    {
        _model?.ResetDefaultPitch();
        this.PitchDefault = _model?.PitchDefault;
        OnPropertyChanged(nameof(PitchDefault));
    }

    /// <summary>
    /// Resets the Pan to the default value provided by the model.
    /// </summary>
    public void ResetDefaultPan()
    {
        _model?.ResetDefaultPan();
        this.PanDefault = _model?.PanDefault;
        OnPropertyChanged(nameof(PanDefault));
    }

    /// <summary>
    /// Resets the Tempo to the default value provided by the model.
    /// </summary>
    public void ResetDefaultTempo()
    {
        _model?.ResetDefaultTempo();
        this.TempoDefault = _model?.TempoDefault;
        OnPropertyChanged(nameof(TempoDefault));
    }

    /// <summary>
    /// Resets the Volume to the default value provided by the model.
    /// </summary>
    public void ResetDefaultVolume()
    {
        _model?.ResetDefaultVolume();
        this.VolumeDefault = _model?.VolumeDefault;
        OnPropertyChanged(nameof(VolumeDefault));
    }
}
