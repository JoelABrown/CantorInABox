using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Mooseware.CantorInABox.ViewModels;

[Serializable]
public partial class OldTrackViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _title;
    
    [ObservableProperty]
    private string? _filespec;

    // Don't serialize the time span since it doesn't work anyway.  Use the hidden TrackLength property for serialization instead.
    [XmlIgnore]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackLength))]
    [NotifyPropertyChangedFor(nameof(FormattedLength))]
    private TimeSpan? _length;

    // XmlSerializer does not support TimeSpan, so use this property for serialization instead...
    [Browsable(false)]
    [XmlElement(DataType = "duration", ElementName = "Length")]
    public string TrackLength
    {
        get
        {
            if (Length is not null)
            {
                return XmlConvert.ToString((TimeSpan)Length!);
            }
            else
            {
                return string.Empty;
            }
        }

        set
        {
            TimeSpan newValue = string.IsNullOrEmpty(value) ? TimeSpan.Zero : XmlConvert.ToTimeSpan(value);
            if (!Length.Equals(newValue))
            {
                Length = newValue;
                OnPropertyChanged(nameof(Length));
            }
        }
    }

    public string FormattedLength
    {
        get
        {
            // Build up the minimal result...
            string result = "0:00";
            if (Length is not null)
            {
                TimeSpan length=(TimeSpan)Length;

                double secondsPlusFraction = (double)length.Seconds + ((double)length.Milliseconds / 1000);
                result = string.Format("{0:00}", (int)Math.Round(secondsPlusFraction));
                if ((int)length.Minutes > 0)
                {
                    result = string.Format("{0:0}", (int)length.Minutes) + ":" + result;
                }
                else
                {   // Is this because we are even 0 minutes with more than one hour or because we're shorter than a minute?
                    if ((int)length.TotalMinutes == 0)
                    {   // Always show 0 minutes if the clip is less than 1 minute...
                        result = "0:" + result;
                    }
                    else
                    {   // Even 00 ...
                        result = "00:" + result;
                    }
                }
                if ((int)length.TotalHours > 0)
                {
                    result = string.Format("{0:0}", (int)length.TotalHours) + ":" + result;
                }
            }
            return result;
        }
    }

    [ObservableProperty]
    private bool _selected;

    ////public Guid LibraryKey { get; set; }

    ////[XmlIgnore]
    ////public Dictionary<string, TrackPageNumber> PageNumbers { get; set; }

    ////// Use this to serialize the _pageNumbers collection
    ////[Browsable(false)]
    ////[XmlElement(DataType = "string", ElementName = "PageNumbers")]
    ////public string FlattenedPageNumbers
    ////{
    ////    get
    ////    {
    ////        string result = "|";
    ////        foreach (var page in PageNumbers)
    ////        {
    ////            result += page.Key.ToString() + ":" + page.Value.ToString() + "|";
    ////        }
    ////        return result;
    ////    }
    ////}

    public OldTrackViewModel()
    {
        ////PageNumbers = [];
    }

    public OldTrackViewModel(string filespec)
    {
        Title = string.Empty;
        Filespec = string.Empty;
        ////PageNumbers = [];

        // Initialize the track by reading its information...
        try
        {
            Filespec = filespec;
            // By default...
            Title = Path.GetFileNameWithoutExtension(filespec);
            // It would be better if we can get the title from the MP3 tags...
            if (File.Exists(filespec))
            {   // See if we can glean the title from the track itself...
                TagLib.File tagFile = TagLib.File.Create(filespec);
                if (tagFile != null)
                {   // Is there a title?
                    if (!string.IsNullOrEmpty(tagFile.Tag.Title))
                    {
                        Title = tagFile.Tag.Title;
                    }
                }
            }

            double lengthInSeconds = Toolbox.GetMediaDuration(filespec);
            Length = TimeSpan.FromSeconds(lengthInSeconds);
        }
        catch (Exception ex)
        {   // For debugging only...
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }
}

public class TrackViewModelEqualityComparer : IEqualityComparer<OldTrackViewModel>
{
    bool IEqualityComparer<OldTrackViewModel>.Equals(OldTrackViewModel? x, OldTrackViewModel? y)
    {
        return (x is not null && y is not null && x.Filespec == y.Filespec);
    }

    int IEqualityComparer<OldTrackViewModel>.GetHashCode(OldTrackViewModel obj)
    {
        throw new NotImplementedException();
    }
}
