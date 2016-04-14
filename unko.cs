using System;
using TETCSharpClient;
using TETCSharpClient.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WebSocketSharp;

public class Unko:IGazeListener {
  public bool Enabled { get; set; }
  public bool Smooth { get; set; }
  public Screen ActiveScreen { get; set; }
  public WebSocket ws { get; set; }

  public Unko() : this(Screen.PrimaryScreen, false, false) { }
  public Unko(Screen screen, bool enabled, bool smooth) {
    GazeManager.Instance.AddGazeListener(this);
    ActiveScreen = screen;
    Enabled = enabled;
    Smooth = smooth;
  }

  public void OnGazeUpdate(GazeData gazeData) {
    if(!Enabled) {
      return;
    }
    // start or stop tracking lost animation
    if((gazeData.State & GazeData.STATE_TRACKING_GAZE) == 0 &&
       (gazeData.State & GazeData.STATE_TRACKING_PRESENCE) == 0) {
      Console.WriteLine("start or stop tracking lost animation");
      return;
    }
    var x = ActiveScreen.Bounds.X;
    var y = ActiveScreen.Bounds.Y;
    var gX = Smooth ? gazeData.SmoothedCoordinates.X : gazeData.RawCoordinates.X;
    var gY = Smooth ? gazeData.SmoothedCoordinates.Y : gazeData.RawCoordinates.Y;
    var screenX = (int)Math.Round(x + gX, 0);
    var screenY = (int)Math.Round(y + gY, 0);
    Console.WriteLine(screenX + "\t" + screenY);
    ws.Send("{screenX:"+screenX+",screenY:"+scrennY+"}");
  }

  static void Main(string[] args) {
    GazeManager.Instance.Activate(GazeManager.ApiVersion.VERSION_1_0, GazeManager.ClientMode.Push);
    if(!GazeManager.Instance.IsConnected) {
      Console.WriteLine("EyeTribe Server has not been started");
    } else if(GazeManager.Instance.IsCalibrated) {
      Console.WriteLine(GazeManager.Instance.LastCalibrationResult);
      Console.WriteLine("Re-Calibrate");
    } else {
      Console.WriteLine("Start");
      ws = new WebSocket ("ws://dragonsnest.far/Laputa");
      ws.Connect ();
    }
    Unko unko = new Unko(Screen.PrimaryScreen, true, true);
  }
}
