using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Windows;

namespace Mooseware.CantorInABox;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Playlist file spec captured from the command line arguments, if present
    /// </summary>
    private string _playlistToLoad = string.Empty;

    public static IHost? AppHost { get; private set; }

    public App()
    {
        // Establish both a main window singleton and a concurrent queue singleton to be used for the HTTP API
        AppHost = (IHost?)Host.CreateDefaultBuilder()
        .ConfigureServices((hostContext, services) =>
        {
            services.Configure<Configuration.AppSettings>(hostContext.Configuration.GetSection("ApplicationSettings"));
            services.AddSingleton<MainWindow>();
            services.AddSingleton<ConcurrentQueue<ApiMessage>>();
        })
        .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost!.StartAsync();

        // Capture any startup arguments from the command line or shell
        string filespec = string.Empty;
        try
        {
            if (e.Args.Length > 0)
            {
                filespec = string.Empty;
                for (int i = 0; i < e.Args.Length; i++)
                {
                    filespec += e.Args[i] + " ";
                }
                filespec = filespec.Trim();
                _playlistToLoad = filespec;
            }
            if (filespec.Length == 0)
            {
                string[] parts = Environment.GetCommandLineArgs();
                for (int i = 0; i < parts.Length; i++)
                {
                    filespec += parts[i] + " ";
                }
                _playlistToLoad = filespec.Trim(); ;
            }
        }
        catch (Exception ex)
        {   // For debugging only...
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }

        // Launch the MainWindow
        var startupForm = AppHost.Services.GetRequiredService<MainWindow>();
        startupForm.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        // Wind down the API server...
        await AppHost!.StopAsync();
        base.OnExit(e);
    }

    /// <summary>
    /// Logs a caught error using a more readable format.
    /// </summary>
    /// <param name="ex">The caught error</param>
    internal static void WriteLog(Exception ex)
    {
        // TODO: Consider replacing this debugging code with actual file logging
        System.Windows.Forms.MessageBox.Show(CompactExMessage(ex), "CantorInABox was minding its own business when this happened...", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
    }

    /// <summary>
    /// Reformats an Exception into a more compact and easy to understand format
    /// </summary>
    /// <param name="ex">The Exception to be formatted</param>
    /// <returns>A more readable string representation of the Exception message</returns>
    private static string CompactExMessage(Exception ex)
    {
        // This functions takes an ex.ToString() and trims out the argument and path information from the 
        // stack trace and makes a variety of other formatting changes (such as indenting inner exceptions)
        // to make the overall message more readable.
        string message = string.Empty;
        string[] lines;
        string thisLine;
        string newLine;
        int startPoint;
        int endPoint;
        int lineNo;
        int indent = 0;
        string thisProc;
        string lastProc = "";
        string origMsg = "";
        bool gottem;
        string backslash = "\\";
        char openParen = '(';
        char closeParen = ')';
        string[] hardReturn = ["\r\n"];
        try
        {
            origMsg = ex.ToString();
            // Split the message into lines...
            lines = origMsg.Split(hardReturn, StringSplitOptions.RemoveEmptyEntries);
            // Check each line...
            for (lineNo = lines.GetLowerBound(0); (lineNo <= lines.GetUpperBound(0)); lineNo++)
            {
                thisLine = lines[lineNo];
                newLine = "";
                gottem = false;

                // Is this a stack trace line?
                if ((thisLine.Trim()[..3] == "at "))
                {
                    // This is a stack trace line.
                    // First, remove the path information...
                    startPoint = thisLine.IndexOf(" in ");
                    endPoint = thisLine.LastIndexOf(backslash);
                    gottem = true;
                    if (startPoint >= 0 && endPoint >= startPoint)
                    {
                        newLine = thisLine[..(startPoint + 4)];
                        thisLine = thisLine[(endPoint + 1)..];
                        newLine = newLine.Trim();
                        // Next, remove the parameter information...
                        startPoint = newLine.IndexOf(openParen);
                        endPoint = newLine.LastIndexOf(closeParen);
                        newLine = (string.Concat(newLine.AsSpan(0, (startPoint + 1)), newLine.AsSpan(endPoint)));
                        // Now, determine the procedure name...
                        endPoint = newLine.IndexOf(openParen);
                        thisProc = newLine[3..endPoint];
                        if ((thisProc == lastProc))
                        {
                            // This is another reference to the same procedure.
                            // This is likely to be the throw line in the catch block and is therefore of subsidiary interest.
                            // Back up to the prior line and append the current line parenthetically.
                            startPoint = (newLine.IndexOf(":line ") + 1);
                            newLine = string.Concat(" (", newLine.AsSpan(startPoint), ")");
                            message += newLine;
                            newLine = "";
                        }
                        else
                        {
                            if (indent == 0)
                            {
                                newLine = "=>" + newLine;
                            }
                            else
                            {
                                newLine = (new string(' ', (indent * 2)) + "+-" + newLine);
                            }
                            indent++;
                        }
                        lastProc = thisProc;
                        newLine = newLine + " " + thisLine;
                    }
                    else
                    {   // This is probably a .NET internal location so just repeat it as-is...
                        newLine = thisLine.Trim();
                    }
                }
                if (!gottem)
                {
                    // This is not a stack trace line or one of the other special types handled above...
                    newLine = (new string(' ', (indent * 2)) + thisLine);
                }
                if (((lineNo > (lines.GetLowerBound(0))) && (newLine != "")))
                {
                    message += Environment.NewLine.ToString();
                }
                message += newLine;
            }
        }
        catch (Exception localex)
        {
            System.Diagnostics.Debug.WriteLine(localex.ToString());
            // Ignore any error here.
            message = origMsg;
        }
        return message;
    }
}
