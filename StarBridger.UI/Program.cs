using Eto.Drawing;
using Eto.Forms;
using System;

namespace StarBridger.UI;

class Program
{
  [STAThread]
  static void Main(string[] args)
  {
    Application app = new Application(Eto.Platform.Detect);
    // Wpf doesn't follow the system light/dark theme unless asked to
    if (app.Platform.IsWpf)
      app.Theme = Themes.System;
    app.Run(new MainForm());
  }
}