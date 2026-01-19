using CommunityToolkit.Mvvm.ComponentModel;
using Mooseware.CantorInABox.Models;

namespace Mooseware.CantorInABox.ViewModels;

/// <summary>
/// The View Model for a libray entry (single track in a library)
/// </summary>
public partial class LibraryEntryViewModel : ObservableObject
{
    /// <summary>
    /// The GUID which identifies the individual library entry (track)
    /// </summary>
    [ObservableProperty]
    private Guid? _libraryEntryKey;
    /// <summary>
    /// Title of the audio track
    /// </summary>
    [ObservableProperty]
    private string? _title;
    /// <summary>
    /// Version of the audio track (e.g. composer)
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PunctuatedRendition))]
    private string? _rendition;
    /// <summary>
    /// Relative or absolute filespec of the audio file for this track
    /// </summary>
    [ObservableProperty]
    private string? _filespec;
    /// <summary>
    /// The length of the track in seconds when played at original speed
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedLength))] 
    private int? _nominalLength;
    /// <summary>
    /// The page number(s) for this track in the containing playlist's default prayer book
    /// </summary>
    [ObservableProperty]
    private string? _pages;
    /// <summary>
    /// Flag indicating whether or not this entry is currently selected in the list of entries in the Library tab
    /// </summary>
    [ObservableProperty]
    private bool _selected = false;
    /// <summary>
    /// The page number(s) for this track in the first prayer book
    /// </summary>
    [ObservableProperty]
    private string? _pagesBookA;
    /// <summary>
    /// The page number(s) for this track in the second prayer book
    /// </summary>
    [ObservableProperty]
    private string? _pagesBookB;
    /// <summary>
    /// The page number(s) for this track in the third prayer book
    /// </summary>
    [ObservableProperty]
    private string? _pagesBookC;
    /// <summary>
    /// Flag indicating whether or not the entry is currently being previewed in the UI
    /// </summary>
    [ObservableProperty]
    private bool _previewing = false;

    /// <summary>
    /// The model on which this view model is based.
    /// </summary>
    private readonly LibraryEntryModel? _model;

    public LibraryEntryViewModel()
    {
        SyncFromModel();
    }

    public LibraryEntryViewModel(LibraryEntryModel model)
    {
        _model = model;
        SyncFromModel();
    }

    /// <summary>
    /// The rendition prepended with a psuedo bullet (hyphen) for display purposes
    /// </summary>
    public string PunctuatedRendition
    {
        get
        {
            string result = string.Empty;
            if (Rendition is not null &&  Rendition.Length > 0)
            {
                result = "- " + Rendition;
            }
            return result;
        }
    }

    /// <summary>
    /// The nominal length in seconds formatted as minutes and seconds for display purposes
    /// </summary>
    public string FormattedLength
    {
        get
        {
            return Utilities.FormattedDurationFromSeconds(NominalLength);
        }
    }

    /// <summary>
    /// Returns a reference to the underlying model for this view model
    /// </summary>
    internal LibraryEntryModel? Model { get { return _model; } }

    /// <summary>
    /// Sets the values in the view model based on the underlying model 
    /// (triggering updates to observable properties in the process)
    /// </summary>
    private void SyncFromModel()
    {
        this.LibraryEntryKey = _model?.LibraryEntryKey;
        this.Title = _model?.Title;
        this.Rendition = _model?.Rendition;
        this.Filespec = _model?.Filespec;
        this.NominalLength = _model?.NominalLength;
        this.PagesBookA = _model?.PagesA;
        this.PagesBookB = _model?.PagesB;
        this.PagesBookC = _model?.PagesC;
    }

    partial void OnLibraryEntryKeyChanged(Guid? value)
    {
        if (_model is not null)
        {
            _model.LibraryEntryKey = value ?? new Guid();
        }
    }

    partial void OnTitleChanged(string? value)
    {
        if (_model is not null)
        {
            _model.Title = value ?? string.Empty;
        }
    }

    partial void OnRenditionChanged(string? value)
    {
        if (_model is not null)
        {
            _model.Rendition = value ?? string.Empty;
        }
    }

    partial void OnFilespecChanged(string? value)
    {
        if (_model is not null)
        {
            _model.Filespec = value ?? string.Empty;
        }
    }

    partial void OnNominalLengthChanged(int? value)
    {
        if (_model is not null)
        {
            _model.NominalLength = value ?? 0;
        }
    }

    partial void OnPagesBookAChanged(string? value)
    {
        if (_model is not null)
        {
           _model.PagesA = value ?? "-";
        }
    }

    partial void OnPagesBookBChanged(string? value)
    {
        if (_model is not null)
        {
            _model.PagesB = value ?? "-";
        }
    }

    partial void OnPagesBookCChanged(string? value)
    {
        if (_model is not null)
        {
            _model.PagesC = value ?? "-";
        }
    }
}
