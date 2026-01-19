using Microsoft.AspNetCore.Mvc;
using Mooseware.CantorInABox.ViewModels;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Mooseware.CantorInABox.Controllers;

/// <summary>
/// Controller for HTTP APIs for Cantor In A Box
/// </summary>
public class HomeController : Controller
{
    /// <summary>
    /// Message queue to pass requests received by this controller to the UI thread for handling
    /// </summary>
    private readonly ConcurrentQueue<ApiMessage> _messageQueue;

    public HomeController(ConcurrentQueue<ApiMessage> msgQueue)
    {
        _messageQueue = msgQueue;
    }

    /// <summary>
    /// Status retrieval API. Gets key current status information recorded by the UI thread in a shared temporary file
    /// </summary>
    /// <returns>StatusApiViewModel details in JSON format</returns>
    [HttpGet("/status")]
    public ActionResult<StatusApiViewModel> Status()
    {
        // Read the status transfer file and return the contents as JSON.
        StatusApiViewModel status = ReadStatusApiDetailsFile();
        return Ok(status);
    }

    /// <summary>
    /// Full path and file spec of the temporary file used to transfer status information from the UI thread to the API thread
    /// </summary>
    private string _statusApiDetailsFilespec = string.Empty;

    /// <summary>
    /// The Mutex name to use for the status file. DWR: this must also be set in MainWindow.xaml.cs to an identical value!
    /// </summary>
    private const string _statusApiFilespecMutex = "CantorInABoxStatusFile";

    /// <summary>
    /// Retrieve the details from the temporary status transmission file
    /// </summary>
    /// <returns>The status information in a POCO</returns>
    internal StatusApiViewModel ReadStatusApiDetailsFile()
    {
        StatusApiViewModel? result = new();
        if (String.IsNullOrEmpty(_statusApiDetailsFilespec))
        {
            // Set the API details transfer file name DWR: this must be the same as in MainWindow.xaml.cs
            _statusApiDetailsFilespec = System.IO.Path.GetTempPath() + "CantorInABoxStatusAPI.json";
        }
        if (System.IO.File.Exists(_statusApiDetailsFilespec))
        {
            using var mutex = new Mutex(false, _statusApiFilespecMutex);
            var hasHandle = false;
            try
            {
                hasHandle = mutex.WaitOne(Timeout.Infinite, false);
                string content = System.IO.File.ReadAllText(_statusApiDetailsFilespec);
                var statusContent = JsonSerializer.Deserialize<StatusApiViewModel>(content);
                result = new(statusContent);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (hasHandle)
                {
                    mutex.ReleaseMutex();
                }
            }
        }
        return result;
    }

    /// <summary>
    /// HTTP API controller for transport actions (play|pause|stop)
    /// </summary>
    /// <param name="cmd">A string indicating the type of action being requested</param>
    /// <returns>HTTP response code (200 if OK)</returns>
    [Route("/transport")]
    [HttpPost()]
    public IActionResult Transport(string cmd="?")
    {
        // Verify that the command is acceptable
        string command = cmd.ToLower().Trim();
        if (command == ApiMessage.TransportPlay 
         || command == ApiMessage.TransportPause 
         || command == ApiMessage.TransportStop)
        {
            ApiMessage message = new()
            {
                Verb = ApiMessageVerb.Transport,
                Parameters = cmd
            };
            _messageQueue.Enqueue(message);
            return Ok();
        }
        else
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// HTTP API controller for playlist action (next track|previous track|from the top)
    /// </summary>
    /// <param name="cmd">A string indicating the type of action being requested</param>
    /// <returns>HTTP response code (200 if OK)</returns>
    [Route("/playlist")]
    [HttpPost()]
    public IActionResult Playlist(string cmd = "?")
    {
        // Verify that the command is acceptable
        string command = cmd.ToLower().Trim();
        if (command == ApiMessage.PlaylistPrevious 
         || command == ApiMessage.PlaylistNext
         || command == ApiMessage.PlaylistRestart)
        {
            ApiMessage message = new()
            {
                Verb = ApiMessageVerb.Playlist,
                Parameters = cmd
            };
            _messageQueue.Enqueue(message);
            return Ok();
        }
        else
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// HTTP API controller for volume action (up|down|reset)
    /// </summary>
    /// <param name="cmd">A string indicating the type of action being requested</param>
    /// <returns>HTTP response code (200 if OK)</returns>
    [Route("/volume")]
    [HttpPost()]
    public IActionResult Volume(string cmd = "?")
    {
        // Verify that the command is acceptable
        string command = cmd.ToLower().Trim();
        if (command == ApiMessage.VolumeLouder 
         || command == ApiMessage.VolumeQuieter 
         || command == ApiMessage.VolumeReset)
        {
            ApiMessage message = new()
            {
                Verb = ApiMessageVerb.Volume,
                Parameters = cmd
            };
            _messageQueue.Enqueue(message);
            return Ok();
        }
        else
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// HTTP API controller for pan action (left|right|reset)
    /// </summary>
    /// <param name="cmd">A string indicating the type of action being requested</param>
    /// <returns>HTTP response code (200 if OK)</returns>
    [Route("/pan")]
    [HttpPost()]
    public IActionResult Pan(string cmd = "?")
    {
        // Verify that the command is acceptable
        string command = cmd.ToLower().Trim();
        if (command == ApiMessage.PanMoreVoice 
         || command == ApiMessage.PanMoreInstrument
         || command == ApiMessage.PanReset)
        {
            ApiMessage message = new()
            {
                Verb = ApiMessageVerb.Pan,
                Parameters = cmd
            };
            _messageQueue.Enqueue(message);
            return Ok();
        }
        else
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// HTTP API controller for tempo action (up|down|reset)
    /// </summary>
    /// <param name="cmd">A string indicating the type of action being requested</param>
    /// <returns>HTTP response code (200 if OK)</returns>
    [Route("/tempo")]
    [HttpPost()]
    public IActionResult Tempo(string cmd = "?")
    {
        // Verify that the command is acceptable
        string command = cmd.ToLower().Trim();
        if (command == ApiMessage.TempoSlower 
         || command == ApiMessage.TempoFaster 
         || command == ApiMessage.TempoReset)
        {
            ApiMessage message = new()
            {
                Verb = ApiMessageVerb.Tempo,
                Parameters = cmd
            };
            _messageQueue.Enqueue(message);
            return Ok();
        }
        else
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// HTTP API controller for pitch action (up|down|reset)
    /// </summary>
    /// <param name="cmd">A string indicating the type of action being requested</param>
    /// <returns>HTTP response code (200 if OK)</returns>
    [Route("/pitch")]
    [HttpPost()]
    public IActionResult Pitch(string cmd = "?")
    {
        // Verify that the command is acceptable
        string command = cmd.ToLower().Trim();
        if (command == ApiMessage.PitchUp 
         || command == ApiMessage.PitchDown 
         || command == ApiMessage.PitchReset)
        {
            ApiMessage message = new()
            {
                Verb = ApiMessageVerb.Pitch,
                Parameters = cmd
            };
            _messageQueue.Enqueue(message);
            return Ok();
        }
        else
        {
            return BadRequest();
        }
    }
}
