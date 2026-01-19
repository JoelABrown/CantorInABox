using System.Xml.Serialization;

namespace Mooseware.CantorInABox.Models;

/// <summary>
/// A single media file entry in a Library
/// </summary>
[Serializable]
[XmlType("Entry")]
public class LibraryEntryModel
{
    /// <summary>
    /// Unique identifier of the media content when used in the owning Library
    /// </summary>
    public Guid LibraryEntryKey { get; set; }

    /// <summary>
    /// Short descriptive name for the media content
    /// </summary>
    public string Title { get; set; }

    public string Rendition { get; set; }

    /// <summary>
    /// Relative path and file name (with respect to the Libary file) of the media content
    /// </summary>
    public string Filespec { get; set; }

    /// <summary>
    /// The duration of the content's play time in seconds when played at 1.0x playback speed
    /// </summary>
    public int NominalLength { get; set; }

    /// <summary>
    /// Page number(s) for the track in the first configured prayerbook
    /// </summary>
    public string PagesA { get; set; } = "-";

    /// <summary>
    /// Page number(s) for the track in the second configured prayerbook
    /// </summary>
    public string PagesB { get; set; } = "-";

    /// <summary>
    /// Page number(s) for the track in the third configured prayerbook
    /// </summary>
    public string PagesC { get; set; } = "-";

    /// <summary>
    /// Creates a default, empty LibraryEntryModel
    /// </summary>
    public LibraryEntryModel()
    {
        LibraryEntryKey = Guid.NewGuid();
        Title = string.Empty;
        Rendition = string.Empty;
        Filespec = string.Empty;
        NominalLength = 0;
        PagesA = "-";
        PagesB = "-";
        PagesC = "-";
    }

    /// <summary>
    /// Creates a LibraryEntryModel and sets its properties
    /// </summary>
    /// <param name="entryKey">The GUID identifying the LibaryEntry</param>
    /// <param name="title">Title of the library entry</param>
    /// <param name="rendition">Rendition (version) of the library entry</param>
    /// <param name="filespec">Full path and file spec of the audio file</param>
    /// <param name="lengthInSeconds">Length of the audio file in seconds</param>
    /// <param name="pagesA">Page number(s) in the first prayer book</param>
    /// <param name="pagesB">Page number(s) in the second prayer book</param>
    /// <param name="pagesC">Page number(s) in the thir prayer book</param>
    public LibraryEntryModel(Guid entryKey, string title, string rendition, string filespec, int lengthInSeconds, string pagesA, string pagesB, string pagesC) 
    {
        LibraryEntryKey = entryKey;
        Title = title;
        Rendition= rendition;
        Filespec = filespec;
        NominalLength = lengthInSeconds;
        PagesA = pagesA;
        PagesB = pagesB;
        PagesC = pagesC;
    }
}
