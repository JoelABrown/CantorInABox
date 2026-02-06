// License :
//
// SoundTouch audio processing library
// Copyright (c) Olli Parviainen
// C# port Copyright (c) Olaf Woudenberg
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

// ********************************************************************
// Multiple changes by JB to code posted by Olaf Woudenberg to adapt
// to the needs of CantorInABox v2.0, including applying the MVVM toolkit.
// ********************************************************************

using SoundTouch;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mooseware.CantorInABox;

public class AudioPlayback
{
    private readonly SoundProcessor _processor;

    private double _tempo = 100.0;  // Original speed by default until we know otherwise.
    private int _pitch;
    private string? _status;
    private string? _filename;

    public event EventHandler? TransportModeChanged;

    public event EventHandler? PlaybackFinished;

    protected virtual void OnTransportModeChanged(EventArgs e)
    {
        TransportModeChanged?.Invoke(this, e);
    }

    public const int PitchHardFloor = -12;
    public const int PitchDefault = 0;
    public const int PitchHardCeiling = 12;
    public const double TempoHardFloor = 50.0;
    public const double TempoDefault = 100.0;
    public const double TempoHardCeiling = 200.0;
    public const float PanHardFloor = -1.0f;
    public const float PanDefaultFallback = 0.0f;
    public const float PanHardCeiling = 1.0f;
    public const float VolumeHardFloor = 0.0f;
    public const float VolumeDefaultFallback = 50.0f;
    public const float VolumeHardCeiling = 1.0f;

    public AudioPlayback()
    {
        _processor = new SoundProcessor();
        _processor.PlaybackStopped += OnPlaybackStopped;

        SetPlaybackMode(PlaybackMode.Unloaded);
    }

    // This event is part of the SoundTouch audio library but it is not needed by Cantor In A Box
    //public event PropertyChangedEventHandler? PropertyChanged;

    public enum PlaybackMode
    {
        Unloaded,
        Stopped,
        Playing,
        Paused,
    }

    private PlaybackMode _transportMode;

    public PlaybackMode TransportMode { get { return _transportMode; } private set { _transportMode = value; } }

    public long Position
    {
        get
        {
            long result = 0;
            if (_processor.ProcessorStream is not null)
            {
                result = _processor.ProcessorStream.Position;
            }
            return result;
        }
        private set
        {
            try
            {
                if (_processor.ProcessorStream is not null)
                {
                    _processor.ProcessorStream.Position = value;
                }
            }
            catch (Exception ex)
            {
                // Probably set an invalid value. Just eat the error.
                Console.WriteLine(ex.ToString());
            }
        }
    }

    public double PositionInSeconds
    {
        get
        {
            double result = 0;
            long lengthLong = Length;
            if (lengthLong > 0)
            {
                double lengthAsDouble = (double)lengthLong;
                double positionAsDouble = (double)Position;
                double totalLength = LengthInSeconds;

                result = (positionAsDouble / lengthAsDouble) * totalLength;
            }
            return result;
        }
    }

    /// <summary>
    /// Position in seconds, taking into consideration the current Tempo
    /// </summary>
    public double EffectivePositionInSeconds
    {
        get
        {
            double nominal = PositionInSeconds;
            double effective = nominal / (double)((_tempo / 100));
            return effective;
        }
    }

    /// <summary>
    /// Position in 1/1000ths used for setting the position of a progress bar
    /// This is independent of the current Tempo
    /// </summary>
    public int PositionInThousandths
    {
        get
        {
            int result = 0;
            long length = Length;
            long position = Position;
            if (length > 0)
            {
                double progress = ((double)position * 1000.0) / (double)length;
                result = (int)(Math.Round(progress, 0));
            }
            return result;
        }
        set
        {
            long lengthLong = Length;
            if (lengthLong > 0)
            {
                double lengthAsDouble = (double)lengthLong;
                double newPositionAsDouble = ((double)value / 1000.0) * lengthAsDouble;
                long newPosition = (long)Math.Round(newPositionAsDouble, 0);
                // Make sure rounding error or bad user input doesn't send us out of range
                newPosition = Math.Max(Math.Min(newPosition, lengthLong), 0);
                this.Position = newPosition;
            }
        }
    }

    /// <summary>
    /// Remaining seconds in the playback based on the original Tempo
    /// </summary>
    public double RemainingInSeconds
    {
        get
        {
            double remaining = LengthInSeconds - PositionInSeconds;
            double result = Math.Max(0.0, remaining);
            return result;
        }
    }

    /// <summary>
    /// Remaining in seconds, taking into consideration the current Tempo
    /// </summary>
    public double EffectiveRemainingInSeconds
    {
        get
        {
            double nominal = RemainingInSeconds;
            double remaining = nominal / (double)((_tempo / 100));
            return remaining;
        }
    }

    /// <summary>
    /// Abstract length of the audio clip
    /// </summary>
    public long Length
    {
        get
        {
            long result = 0;
            if (_processor.ProcessorStream is not null)
            {
                result = (_processor.ProcessorStream.Length);
            }
            return result;
        }
    }

    /// <summary>
    /// Length of the audio clip in seconds at the original Tempo
    /// </summary>
    public double LengthInSeconds
    {
        get
        {
            double result = 0;
            if (_processor.ProcessorStream is not null)
            {
                result = (_processor.ProcessorStream.TotalTime.TotalSeconds);
            }
            return result;
        }
    }

    public void Play()
    {
        if (_processor.Play())
        {
            SetPlaybackMode(PlaybackMode.Playing);
        }
    }

    public void Pause()
    {
        if (_processor.Pause())
        {
            SetPlaybackMode(PlaybackMode.Paused);
        }
    }

    public void Stop()
    {
        if (_processor.Stop())
        {
            SetPlaybackMode(PlaybackMode.Stopped);
        }
    }

    public string? Status
    {
        get => _status;
        private set => _status = value;
    }

    public double Tempo
    {
        get => _tempo;
        set
        {
            _tempo = Math.Max(TempoHardFloor, Math.Min(value, TempoHardCeiling));

            if (_processor.ProcessorStream != null)
                _processor.ProcessorStream.TempoChange = value;
        }
    }

    public int Pitch
    {
        get => _pitch;
        set
        {
            _pitch = Math.Max(PitchHardFloor, Math.Min(value, PitchHardCeiling));
            _pitch = value;
            if (_processor.ProcessorStream != null)
                _processor.ProcessorStream.PitchSemiTones = value;
        }
    }

    public float Pan
    {
        get
        {
            float result = 0.0f;
            if (_processor.ProcessorStream is not null)
            {
                result = _processor.ProcessorStream.Pan;
            }
            return result;
        }

        set
        {
            if (_processor.ProcessorStream is not null)
            {
                _processor.ProcessorStream.Pan = Math.Max(PanHardFloor, Math.Min(value, PanHardCeiling));
            }
        }
    }

    public float Volume
    {
        get
        {
            float result = 0.0f;
            if (_processor.ProcessorStream is not null)
            {
                result = _processor.ProcessorStream.Volume;
            }
            return result;
        }

        set
        {
            if (_processor.ProcessorStream is not null)
            {
                _processor.ProcessorStream.Volume = Math.Max(VolumeHardFloor, Math.Min(value, VolumeHardCeiling));
            }
        }
    }

    // NOTE: This feature of SoundTouch is not desirable for CantorInABox
    //public int Rate
    //{
    //    get => _rate;
    //    set
    //    {
    //        Set(ref _rate, value);
    //        if (_processor.ProcessorStream != null)
    //            _processor.ProcessorStream.RateChange = value;
    //    }
    //}

    public string? Filename
    {
        get => _filename;
        set
        {
            _filename = value;
            if (System.IO.File.Exists(_filename))
            {
                OpenFile(_filename);
            }
        }
    }

    public void Dispose()
    {
        _processor.Dispose();
    }

    private void OpenFile(string filename)
    {
        Stop();
        if (_processor.OpenFile(filename))
        {
            _filename = filename;
            SetPlaybackMode(PlaybackMode.Stopped);
        }
        else
        {
            _filename = string.Empty;
            SetPlaybackMode(PlaybackMode.Unloaded);
        }
    }

    /// <summary>
    /// Flag indicating whether it makes sense to enable a Play feature based on current context
    /// </summary>
    public bool CanPlay { get => (_transportMode == PlaybackMode.Stopped || _transportMode == PlaybackMode.Paused); }

    /// <summary>
    /// Flag indicating whether it makes sense to enable a Pause feature based on current context
    /// </summary>
    public bool CanPause { get => (_transportMode == PlaybackMode.Playing); }

    /// <summary>
    /// Flag indicating whether it makes sense to enable a Stop feature based on current context
    /// </summary>
    public bool CanStop { get => (_transportMode == PlaybackMode.Playing || _transportMode == PlaybackMode.Paused); }

    /// <summary>
    /// Set the current PlaybackMode and raise an event which the UI can respond to
    /// </summary>
    /// <param name="mode">The new PlaybackMode to set</param>
    private void SetPlaybackMode(PlaybackMode mode)
    {
        _transportMode = mode;

        OnTransportModeChanged(new EventArgs());
    }

    private void OnPlaybackStopped(object? sender, bool endReached)
    {
        if (endReached)
        {
            this.PlaybackFinished?.Invoke(this, EventArgs.Empty);

            SetPlaybackMode(PlaybackMode.Stopped);
        }
    }
}
