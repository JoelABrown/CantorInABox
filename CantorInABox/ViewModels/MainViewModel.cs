using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mooseware.CantorInABox.Models;
using System.Collections.ObjectModel;
using System.Windows.Navigation;

namespace Mooseware.CantorInABox.ViewModels;

public partial class MainViewModel : ObservableObject
{


    // *******************************************************************************
    #region Everything below here needs to be vetted and probably rebuilt or discarded
    // *******************************************************************************

    /// <summary>
    /// Index of the selected track (1-based, 0=none)
    /// </summary>
    private int _currentTrackIndex = 0;

    private OldTrackViewModel? _currentTrack;

    public TrackListViewModel? LoadedLibrary { get; set; }

    public TrackListViewModel? LoadedPlaylist { get; set; }

    public int CurrentTrackIndex
    {
        get => _currentTrackIndex;
        private set
        {
            _currentTrackIndex = 0; // Until we sanity check
            if (LoadedPlaylist is not null && value > 0 && LoadedPlaylist.Tracks.Count >= value)
            {
                // This is the new selected track index (1-based)
                _currentTrackIndex = value;
                CurrentTrack = LoadedPlaylist.Tracks[_currentTrackIndex - 1];   // NB: 1-based!

                // TODO: Now set the new previous and next tracks, as applicable...
                // Previous is either n minus 1 of none if current is 1...
                if (_currentTrackIndex > 1)
                {
                    PreviousTrack = LoadedPlaylist.Tracks[_currentTrackIndex - 2];
                }
                else
                {
                    PreviousTrack = null;
                }
                if (_currentTrackIndex < LoadedPlaylist.Tracks.Count)
                {
                    NextTrack = LoadedPlaylist.Tracks[_currentTrackIndex];  // -1+1!
                }
                else
                {
                    NextTrack = null;
                }
            }
            OnPropertyChanged(nameof(CurrentTrack));
            OnPropertyChanged(nameof(PreviousTrack));
            OnPropertyChanged(nameof(NextTrack));
        }
    }

    public OldTrackViewModel? CurrentTrack 
    { 
        get => _currentTrack; 
        set
        {
            _currentTrack = value;
            OnPropertyChanged(nameof(CurrentTrackIndex));
            OnPropertyChanged(nameof(CurrentTrack));
            NextCommand.NotifyCanExecuteChanged();
            PreviousCommand.NotifyCanExecuteChanged();

            RefreshTransportCommands();
        }
    }

    private void RefreshTransportCommands()
    {
        OnPropertyChanged(nameof(CanPlay));
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanStop));

        PlayCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }

    public OldTrackViewModel? PreviousTrack { get; set; }

    public OldTrackViewModel? NextTrack { get; set; }

    [RelayCommand(CanExecute = nameof(CanPlay))]
    public void Play()
    {
        if (CurrentTrack is not null)
        {
            RefreshTransportCommands();
            throw new NotImplementedException();
        }
    }

    [RelayCommand]
    public void Pause()
    {
        if (CurrentTrack is not null)
        {
            RefreshTransportCommands();
            throw new NotImplementedException();
        }
    }

    [RelayCommand]
    public void Stop()
    {
        if (CurrentTrack is not null)
        {
            RefreshTransportCommands();
            throw new NotImplementedException();
        }
    }

    [RelayCommand(CanExecute = nameof(CanNext))]
    public void Next()
    {
        CurrentTrackIndex++;
        RefreshTransportCommands();
    }

    [RelayCommand(CanExecute = nameof(CanPrevious))]
    public void Previous()
    {
        CurrentTrackIndex--;
        RefreshTransportCommands();
    }

    public bool CanPlay
    {
        get
        {
            bool result = false;
            if (_currentTrack is not null)
            {
                // TODO: Figure out how to tell if the current track is already playing (can't play if playing already)
                // This is the short answer for now.
                result = true;
            }
            return result;
        }
    }

    public bool CanPause
    {
        get
        {
            bool result = false;
            if (_currentTrack is not null)
            {
                // TODO: Figure out how to tell if the current track is already playing (can't pause if not playing already)
                // This is the short answer for now.
                result = true;
            }
            return result;
        }
    }

    public bool CanStop
    {
        get
        {
            bool result = false;
            if (_currentTrack is not null)
            {
                // TODO: Figure out how to tell if the current track is already playing (can't pause if not playing already)
                // This is the short answer for now.
                result = true;
            }
            return result;
        }
    }

    public bool CanNext
    {
        get
        {
            bool result = false;
            if (LoadedPlaylist is not null && _currentTrackIndex < LoadedPlaylist.Tracks.Count)
            {
                result = true;
            }
            return result;
        }
    }

    public bool CanPrevious
    {
        get
        {
            bool result = false;
            if (LoadedPlaylist is not null && _currentTrackIndex > 1)
            {
                result = true;
            }
            return result;
        }
    }

    #endregion
}
