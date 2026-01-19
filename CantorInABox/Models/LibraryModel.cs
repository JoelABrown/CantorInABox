using System.IO;
using System.Xml.Serialization;

namespace Mooseware.CantorInABox.Models;

/// <summary>
/// A collection of sound files that can be used to build a playlist
/// </summary>
[Serializable]
[XmlType("Library")]
public class LibraryModel
{
    /// <summary>
    /// Unique identifier for the Library used to index collections of Libraries
    /// </summary>
    public Guid LibraryKey { get; set; }
    /// <summary>
    /// The full path and file name of the Library when stored in XML format
    /// </summary>
    public string Filespec { get; set; }
    /// <summary>
    /// Short but descriptive name for the Library
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// An optional description of the library to give context for the expected contents
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The individual audio clips that are contained in the library
    /// </summary>
    public List<LibraryEntryModel> Entries { get; set; }

    /// <summary>
    /// Create a new Library model from scratch
    /// </summary>
    public LibraryModel()
    {
        LibraryKey = Guid.NewGuid();
        Filespec = string.Empty;
        Title = "(New track library)";
        Description = string.Empty;
        Entries = [];
    }

    /// <summary>
    /// Persists a given Libary model in XML format using its currently defined full path and filespec.
    /// </summary>
    /// <param name="libraryModel">The instance of a LibraryModel to be persisted.</param>
    /// <param name="libraryFilespec">The desired full path and file spec to be used for the save file.
    /// If the given filespec does not have a ".ciblib" file extension, that will be added automatically.</param>
    public static void SaveLibraryFile(LibraryModel libraryModel, string libraryFilespec)
    {
        const string libFileExt = ".ciblib";

        // If this is an existing library being saved to a new filespec, modify the GUID of the library and its entries...
        if (libraryModel.Filespec is not null && libraryModel.Filespec != libraryFilespec)
        {
            libraryModel.LibraryKey = Guid.NewGuid();
            foreach (var entry in libraryModel.Entries)
            {
                entry.LibraryEntryKey = Guid.NewGuid();
            }
        }

        // Make sure the library knows where it's being saved (important for basing item paths)
        libraryModel.Filespec = libraryFilespec.Trim();
        // Enforce the .ciblib file extension
        if (!libraryModel.Filespec.ToLower().EndsWith(libFileExt))
        {
            libraryModel.Filespec += libFileExt;
        }

        // Restate the track file specifications in relative terms to the track list file specification...
        // This is important in case the library and its entries are lifted and shifted to a new location.
        // They need to stay in the same relative path, but they can be under a new root when re-opened.
        foreach (LibraryEntryModel entry in libraryModel.Entries)
        {
            string relativeFile = Toolbox.GetRelativePath(libraryModel.Filespec, entry.Filespec);
            entry.Filespec = relativeFile;
        }

        try
        {
            // Now persist the Library data structure.
            XmlSerializer xs = new(typeof(LibraryModel));
            TextWriter xmlTextWriter = new StreamWriter(libraryModel.Filespec, false);  // Overwrite any existing file.
            xs.Serialize(xmlTextWriter, libraryModel);
            xmlTextWriter.Flush();
            xmlTextWriter.Close();

            // Reconstitute the full absolute path for the current tracks based on where the playlist file is right now...
            foreach (LibraryEntryModel entry in libraryModel.Entries)
            {
                entry.Filespec = Path.GetFullPath(Path.Combine(libraryModel.Filespec, entry.Filespec));
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Reconstitutes a LibraryModel instance based on the settings saved in a given .ciblib file.
    /// </summary>
    /// <param name="filespec">The full path and filespec of the library save file to be opened.</param>
    /// <returns>A LibraryModel instance or null if the file is not found or another problem occurs.</returns>
    public static LibraryModel? OpenLibraryFile(string filespec)
    {
        LibraryModel? result = null;
        if (File.Exists(filespec))
        {
            XmlSerializer xs = new(typeof(LibraryModel));
            using var fileStream = new FileStream(filespec, FileMode.Open);
            result = (LibraryModel)xs.Deserialize(fileStream)!;

            // Make sure the Library object knows where it actually came from...
            result.Filespec = filespec;

            // Reconstitute the full absolute path for the current tracks based on where the playlist file is right now...
            foreach (LibraryEntryModel entry in result.Entries)
            {
                entry.Filespec = Path.GetFullPath(Path.Combine(result.Filespec, entry.Filespec));
            }
        }
        return result;
    }
}
