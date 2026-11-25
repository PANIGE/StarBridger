using Eto.Drawing;
using Eto.Forms;
using StarBridger.SDK;
using StarBridger.SDK.Exceptions;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace StarBridger.UI;

public partial class MainForm : Form
{
  private readonly StarBridgerClient _client = new();
  private readonly UITimer _statusTimer = new() { Interval = 2 };

  private readonly Label _statusLight = new() { Text = "●", TextColor = Colors.Gray };
  private readonly Label _statusLabel = new() { Text = "Checking Host..." };

  private readonly Button _startButton = new() { Text = "START", Size = new Size(154, 52), Enabled = false };
  private readonly Button _stopButton = new() { Text = "STOP", Size = new Size(154, 52), Enabled = false };
  private readonly Button _shellButton = new() { Text = "Open Shell", Size = new Size(108, 26) };

  private bool _checkingStatus;
  private bool _closed;

  public MainForm()
  {
    Title = "StarBridger";
    ClientSize = new Size(350, 158);
    Resizable = false;
    Icon = Icon.FromResource(@"StarBridger.UI.ressources.icon.ico");

    Content = new StackLayout
    {
      Padding = 16,
      Spacing = 10,
      HorizontalContentAlignment = HorizontalAlignment.Stretch,
      Items =
      {
        new StackLayout
        {
          Orientation = Orientation.Horizontal,
          Spacing = 8,
          Items = { _statusLight, _statusLabel }
        },
        new StackLayout
        {
          Orientation = Orientation.Horizontal,
          Spacing = 10,
          Items = { _startButton, _stopButton }
        },
        new StackLayout
        {
          Orientation = Orientation.Horizontal,
          Items =
          {
            new StackLayoutItem(new Panel(), true),
            _shellButton
          }
        }
      }
    };

    _startButton.Click += (_, _) => StartHost();
    _stopButton.Click += async (_, _) => await StopHostAsync();
    _shellButton.Click += (_, _) => OpenShell();

    _statusTimer.Elapsed += async (_, _) => await RefreshStatusAsync();

    Shown += async (_, _) =>
    {
      _statusTimer.Start();
      await RefreshStatusAsync();
    };

    Closed += async (_, _) =>
    {
      _closed = true;
      _statusTimer.Stop();
      _statusTimer.Dispose();
      await _client.DisposeAsync();
    };
  }

  private void StartHost()
  {
    try
    {
      string hostPath = GetExecutablePath("StarBridger.Host.exe");
      EnsureExists(hostPath);

      using Process? process = Process.Start(new ProcessStartInfo
      {
        FileName = hostPath,
        WorkingDirectory = AppContext.BaseDirectory,
        UseShellExecute = false,
        CreateNoWindow = true
      }) 
        ?? throw new InvalidOperationException("Could not launch the Host process.");

      SetStatus("Starting Host...", Colors.Orange, null);
    }
    catch (Exception ex)
    {
      MessageBox.Show(this, $"Failed to start Host:\n{ex.Message}");
    }
  }

  private async Task StopHostAsync()
  {
    SetStatus("Stopping Host...", Colors.Orange, null);

    try
    {
      // The Shell uses a separate pipe per command; RunAsync() is not required.
      await _client.Shell.ShutdownHostAsync();
    }
    catch (Exception ex)
    {
      MessageBox.Show(this, $"Failed to stop Host:\n{ex.Message}");
    }

    await RefreshStatusAsync();
  }

  private void OpenShell()
  {
    try
    {
      string shellPath = GetExecutablePath("StarBridger.Shell.exe");
      EnsureExists(shellPath);

      using Process? process = Process.Start(new ProcessStartInfo
      {
        FileName = shellPath,
        WorkingDirectory = AppContext.BaseDirectory,
        UseShellExecute = true
      }) 
        ?? throw new InvalidOperationException("Could not launch the Shell process.");
    }
    catch (Exception ex)
    {
      MessageBox.Show(this, $"Failed to open Shell:\n{ex.Message}");
    }
  }

  private async Task RefreshStatusAsync()
  {
    if (_checkingStatus || _closed)
      return;

    _checkingStatus = true;

    try
    {
      // A successful Shell response means the Host is reachable.
      await _client.Shell.GetHostStatusAsync();
      SetStatus("Host running", Colors.Green, true);
    }
    catch (TimeoutException)
    {
      SetStatus("Host stopped", Colors.Red, false);
    }
    catch (ShellCommandException)
    {
      SetStatus("Host status unavailable", Colors.Orange, null);
    }
    catch (IOException)
    {
      SetStatus("Host stopped", Colors.Red, false);
    }
    catch (Exception ex)
    {
      SetStatus("Host status error", Colors.Orange, null);
    }
    finally
    {
      _checkingStatus = false;
    }
  }

  private void SetStatus(string text, Color color, bool? running)
  {
    Application.Instance.AsyncInvoke(() =>
    {
      if (_closed)
        return;

      _statusLight.TextColor = color;
      _statusLabel.Text = text;
      _startButton.Enabled = running == false;
      _stopButton.Enabled = running == true;
    });
  }

  private static string GetExecutablePath(string filename)
  {
    return Path.Combine(AppContext.BaseDirectory, filename);
  }

  private static void EnsureExists(string path)
  {
    if (!File.Exists(path))
      throw new FileNotFoundException("Executable not found in the application directory.", path);
  }
}
