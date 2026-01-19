using System.IO;

namespace Mooseware.CantorInABox.ViewModels;

/// <summary>
/// A library file (.ciblib) which has been previously loaded (and is therefore known to the app)
/// </summary>
public class KnownLibraryViewModel
{
    /// <summary>
    /// The GUID which identifies the library file
    /// </summary>
    public Guid? LibraryKey { get; set; }
    /// <summary>
    /// The full path and file spec to the library (.ciblib) file 
    /// </summary>
    public string Filespec { get; set; } = string.Empty;
    /// <summary>
    /// Short descriptive title of the track library
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Longer description of the purpose and contents of the library (optional)
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Flag indicating whether the library can be found in the indicated location. When false the library has not been loaded
    /// It may have been deleted or moved since the last known library record was established.
    /// </summary>
    public bool IsValid { get; set; } = false;
    
    /// <summary>
    /// Create a blank default KnownLibraryViewModel
    /// </summary>
    public KnownLibraryViewModel()
    {
        
    }

    /// <summary>
    /// Create a KnownLibraryViewModel and populate its properties
    /// </summary>
    /// <param name="libraryKey">Library Key GUID</param>
    /// <param name="isValid">Whether or not the file has been found as defined by the filespec</param>
    /// <param name="filespec">Full path and filespec where the .ciblib file is stored</param>
    /// <param name="title">Short descriptive title of the library</param>
    /// <param name="description">Fuller description of the content and purpose of the library (optional)</param>
    public KnownLibraryViewModel(Guid libraryKey, bool isValid, string filespec, string title, string description)
    {
        LibraryKey = libraryKey;
        IsValid = isValid;
        Filespec = filespec;
        Title = title;
        Description = description;
    }

    /// <summary>
    /// Create a KnownLibraryViewModel for a filespec that cannot be located.
    /// </summary>
    /// <param name="missingFilespec">Full path and file spec for the missing file</param>
    public KnownLibraryViewModel(string missingFilespec)
    {
        LibraryKey = null;
        IsValid = false;
        Filespec = missingFilespec;
        Title = missingFilespec + " (missing)";
        Description = string.Empty;
    }

    public override string ToString()
    {
        string result = "(new Library)";
        if (Filespec is not null && Filespec.Length > 0)
        {
            result = Path.GetFileNameWithoutExtension(this.Filespec);
        }
        return result;
    }
}
