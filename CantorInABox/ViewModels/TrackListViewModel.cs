using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.ObjectModel;

namespace Mooseware.CantorInABox.ViewModels;

[Serializable]
public partial class TrackListViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private string? _filespec;

    public ObservableCollection<OldTrackViewModel> Tracks { get; set; }

    public TrackListViewModel()
    {
        Title = "New Track List";
        Filespec = string.Empty;
        Tracks = [];
    }

    public void Save()
    {
        if (!string.IsNullOrEmpty(Filespec))
        {
            SaveAs(this.Filespec);
        }
    }

    public void SaveAs(string filespec)
    {
        try
        {
            this.Filespec = filespec;

            // Restate the track file specifications in relative terms to the track list file specification...
            foreach (OldTrackViewModel oTrack in Tracks)
            {
                string sRelativeFile = Toolbox.GetRelativePath(this.Filespec, oTrack.Filespec!);
                oTrack.Filespec = sRelativeFile;
            }

            // Record the file...
            System.Xml.XmlTextWriter oXml = new(filespec, System.Text.Encoding.Default)
            {
                Formatting = System.Xml.Formatting.Indented,
                Indentation = 4,
                IndentChar = ' '
            };
            oXml.WriteStartDocument();
            oXml.WriteStartElement("TrackList");
            oXml.WriteAttributeString("xmlns:xsi", @"http://www.w3.org/2001/XMLSchema-instance");
            oXml.WriteAttributeString("xmlns:xsd", @"http://www.w3.org/2001/XMLSchema");
            oXml.WriteElementString("Title", this.Title);
            oXml.WriteElementString("Filespec", this.Filespec);
            oXml.WriteStartElement("Tracks");
            for (int i = 0; i < Tracks.Count; i++)
            {
                oXml.WriteStartElement("Track");
                oXml.WriteElementString("Title", Tracks[i].Title);
                oXml.WriteElementString("Filespec", Tracks[i].Filespec);
                oXml.WriteElementString("Length", Tracks[i].TrackLength);
                oXml.WriteElementString("Selected", Tracks[i].Selected.ToString());
                oXml.WriteEndElement();     // Track
            }
            oXml.WriteEndElement();     // Tracks
            oXml.WriteEndElement();     // TrackList
            oXml.WriteEndDocument();
            oXml.Flush();
            oXml.Close();

            // Reconstitute the full absolute path for the current tracks based on where the playlist file is right now...
            foreach (OldTrackViewModel oTrack in Tracks)
            {
                oTrack.Filespec = Path.GetFullPath(Path.Combine(filespec, oTrack.Filespec!));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());
            throw;
        }
    }

    public void Open(string filespec)
    {
        FileStream plfFile;
        OldTrackViewModel? track = null;
        string element = string.Empty;
        bool trackPending = false;

        try
        {
            // See if the file is there...
            if (File.Exists(filespec))
            {   // Reset this object...
                this.Title = string.Empty;
                this.Tracks.Clear();
                this.Filespec = filespec;

                plfFile = new FileStream(filespec, FileMode.Open, FileAccess.Read, FileShare.None);
                System.Xml.XmlTextReader xmlReader = new(plfFile);
                while (xmlReader.Read())
                {
                    switch (xmlReader.NodeType)
                    {
                        case System.Xml.XmlNodeType.Element:
                            // Make a note of what element we've just entered.
                            // (The text will be in the next read...)
                            element = xmlReader.Name;
                            if (xmlReader.Name == "Track")
                            {   // Start the track section...
                                trackPending = true;
                                track = new OldTrackViewModel();
                            }
                            break;
                        case System.Xml.XmlNodeType.EndElement:
                            // Is this the end of a track?
                            if (xmlReader.Name == "Track")
                            {   // Add this rule to the list...
                                Tracks.Add(track!);
                                trackPending = false;
                            }
                            break;
                        case System.Xml.XmlNodeType.Text:
                            // Pick up the setting value and assign it to the internal buffer...
                            switch (element)
                            {
                                case "Tracks":
                                    // Start of the track list.
                                    break;
                                case "Title":
                                    if (trackPending)
                                    {   // Track title...
                                        track!.Title = xmlReader.Value;
                                    }
                                    else
                                    {   // Playlist title...
                                        this.Title = xmlReader.Value;
                                    }
                                    break;
                                case "Filespec":
                                    if (trackPending)
                                    {
                                        track!.Filespec = Path.GetFullPath(Path.Combine(filespec, xmlReader.Value));
                                    }
                                    break;
                                case "Length":
                                    int value = 0;
                                    _ = int.TryParse(xmlReader.Value, out value);
                                    TimeSpan trackLength = TimeSpan.FromSeconds((double)value);
                                    track!.Length = trackLength;
                                    break;
                                case "Selected":
                                    bool boolValue = false;
                                    _ = bool.TryParse(xmlReader.Value, out boolValue);
                                    track!.Selected = boolValue;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case System.Xml.XmlNodeType.Whitespace:
                            // ignore these.
                            break;
                        default:
                            element = "";
                            break;
                    }
                }
                // NOTE: Do NOT close the Xml reader because this also disposes the underlying file stream!
                xmlReader.Close();
                plfFile.Close();
                plfFile.Dispose();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());
            throw;
        }
    }
}
