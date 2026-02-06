using CommunityToolkit.Mvvm.ComponentModel;
using Mooseware.CantorInABox.Models;
using System.Collections.ObjectModel;

namespace Mooseware.CantorInABox.ViewModels;

/// <summary>
/// View model of a Library (.ciblib) file containing a list of entries representing audio tracks
/// </summary>
public partial class LibraryViewModel : ObservableObject
{
    /// <summary>
    /// The GUID which identifies the library
    /// </summary>
    [ObservableProperty]
    private Guid? _libraryKey;

    /// <summary>
    /// The short descriptive title of the library
    /// </summary>
    [ObservableProperty]
    private string? _title;

    /// <summary>
    /// A longer (optional) description of the purpose and contents of the library
    /// </summary>
    [ObservableProperty] 
    private string? _description;

    /// <summary>
    /// The full path and file spec of the library file (.ciblib) as of the last recording.
    /// This can be useful for rebasing the folder location of contained entries if the file
    /// is moved between the time it was saved and the time it is next opened.
    /// </summary>
    [ObservableProperty]
    private string? _filespec;

    /// <summary>
    /// The list of entries which comprise the library
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<LibraryEntryViewModel> _entries = [];

    /// <summary>
    /// The underlying model for this view model
    /// </summary>
    private LibraryModel? _model;

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

    partial void OnFilespecChanged(string? value)
    {
        if (_model is not null)
        {
            _model.Filespec = value ?? string.Empty;
        }
    }

    /// <summary>
    /// Persists the underlying model using the current value of the Filespec property
    /// </summary>
    public void Save()
    {
        if (_model is not null)
        {
            _model.Entries.Clear();
            foreach (var entry in Entries)
            {
                _model.Entries.Add(entry.Model!);
            }
            LibraryModel.SaveLibraryFile(_model, _model.Filespec);
            SyncFromModel();
        }
    }

    /// <summary>
    /// Persists the underlying model using a provided full path and filespec
    /// </summary>
    /// <param name="filespec">The full path and file spec to use for the .ciblib file</param>
    public void SaveAs(string filespec)
    {
        if (_model is not null)
        {
            LibraryModel.SaveLibraryFile(_model, filespec);
            SyncFromModel();
        }
    }

    /// <summary>
    /// Create a new default instance
    /// </summary>
    public LibraryViewModel()
    {
        
    }

    /// <summary>
    /// Create a new view model instance based on an underlying LibraryModel
    /// </summary>
    /// <param name="model">The model to use as the basis for this new view model</param>
    public LibraryViewModel(LibraryModel model)
    {
        _model = model;
        SyncFromModel();
    }

    /// <summary>
    /// Sets the values in the view model based on the underlying model 
    /// (triggering updates to observable properties in the process)
    /// </summary>
    private void SyncFromModel()
    {
        this.LibraryKey = _model?.LibraryKey;
        OnPropertyChanged(nameof(LibraryKey));
        this.Title = _model?.Title;
        OnPropertyChanged(nameof(Title));
        this.Description = _model?.Description;
        OnPropertyChanged(nameof(Description));
        this.Filespec = _model?.Filespec;
        this.Entries.Clear();
        if (_model is not null)
        {
            foreach (var entry in _model.Entries)
            {
                LibraryEntryViewModel viewModel = new(entry);
                this.Entries.Add(viewModel);
            }
        }
    }
}
