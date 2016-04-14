using System;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using TETCSharpClient;
using TETCSharpClient.Data;
using Newtonsoft.Json.Linq;

public class GazePoint:IGazeListener {
  private TcpClient socket;
  private System.Threading.Thread incomingThread;
  private System.Timers.Timer timerHeartbeat;
  public event EventHandler<ReceivedDataEventArgs> OnData;
  SendKey sendKey = new SendKey();

  public GazePoint() {
    GazeManager.Instance.Activate(1, GazeManager.ClientMode.Push);
    GazeManager.Instance.AddGazeListener(this);
  }

  public void OnGazeUpdate(GazeData gazeData) {
    double gX = gazeData.SmoothedCoordinates.X;
    double gY = gazeData.SmoothedCoordinates.Y;
  }

  public void OnCalibrationStateChanged(bool val) { }
  public void OnScreenIndexChanged(int number) { }

  public bool Connect(string host, int port) {
    try {
      socket = new TcpClient(host, port);
    } catch(Exception ex) {
      Console.Out.WriteLine("Error connecting: " + ex.Message);
      return false;
    }

    // Send the obligatory connect request message
    string REQ_CONNECT = "{\"values\":{\"push\":true,\"version\":1},\"category\":\"tracker\",\"request\":\"set\"}";
    Send(REQ_CONNECT);

    // Lauch a seperate thread to parse incoming data
    incomingThread = new System.Threading.Thread(ListenerLoop);
    incomingThread.Start();

    // Start a timer that sends a heartbeat every 250ms.
    // The minimum interval required by the server can be read out 
    // in the response to the initial connect request.   

    string REQ_HEATBEAT = "{\"category\":\"heartbeat\",\"request\":null}";
    timerHeartbeat = new System.Timers.Timer(300);
    timerHeartbeat.Elapsed += delegate { Send(REQ_HEATBEAT); };
    timerHeartbeat.Start();

    return true;
  }

  private void Send(string message) {
    if(socket != null && socket.Connected) {
      System.IO.StreamWriter writer = new System.IO.StreamWriter(socket.GetStream());
      writer.WriteLine(message);
      writer.Flush();
    }
  }

  private void ListenerLoop() {
    System.IO.StreamReader reader = new System.IO.StreamReader(socket.GetStream());
    bool isRunning = true;

    while(isRunning) {
      string response = string.Empty;

      try {
        response = reader.ReadLine();

        JObject jObject = JObject.Parse(response);

        Packet p = new Packet();
        //p.rawData = json;

        p.category = (string)jObject["category"];
        p.request = (string)jObject["request"];
        p.statusCode = (string)jObject["statuscode"];

        JToken values = jObject.GetValue("values");

        if(values != null) {
          // sanitation
          p.values = values.ToString().Replace("\r\n", "");

          // 視線のx,y座標の取得
          JObject j = JObject.Parse(p.values);
          int x = (int)j["frame"]["avg"]["x"];
          int y = (int)j["frame"]["avg"]["y"];
          //Console.WriteLine("x:" + x + " y:" + y);

          // 左目
          int leftX = (int)j["frame"]["lefteye"]["raw"]["x"];
          // 右目
          int rightX = (int)j["frame"]["righteye"]["raw"]["x"];

          // マウスの移動
          if(x != 0 && y != 0) {
            Cursor.Position = new System.Drawing.Point(x, y);
          }

          if(leftX != 0 && rightX == 0) {
            Console.WriteLine("右目閉じてる");
            sendKey.Send(Keys.Enter, false);
          }
          if(leftX == 0 && rightX != 0) {
            Console.WriteLine("左目閉じてる");
          }
        }

        // Raise event with the data
        if(OnData != null)
          OnData(this, new ReceivedDataEventArgs(p));
      } catch(Exception ex) {
        Console.Out.WriteLine("Error while reading response: " + ex.Message);
      }
    }
  }

  static void Main(string[] args) {
    Console.WriteLine("Start");
    GazePoint gazePoint = new GazePoint();
    bool canConnect = gazePoint.Connect("localhost", 6555);
    if(!canConnect) {
      Console.WriteLine("接続に失敗");
      return;
    }
  }

}


public class Packet {
  public string time = DateTime.UtcNow.Ticks.ToString();
  public string category = string.Empty;
  public string request = string.Empty;
  public string statusCode = string.Empty;
  public string values = string.Empty;
  public string rawData = string.Empty;

  public Packet() { }
}

public class ReceivedDataEventArgs:EventArgs {
  private Packet packet;

  public ReceivedDataEventArgs(Packet _packet) {
    this.packet = _packet;
  }

  public Packet Packet {
    get { return packet; }
  }
}



class SendKey {

  [StructLayout(LayoutKind.Sequential)]
  private struct MOUSEINPUT {
    public int dx;
    public int dy;
    public int mouseData;
    public int dwFlags;
    public int time;
    public int dwExtraInfo;
  };

  [StructLayout(LayoutKind.Sequential)]
  private struct KEYBDINPUT {
    public short wVk;
    public short wScan;
    public int dwFlags;
    public int time;
    public int dwExtraInfo;
  };

  [StructLayout(LayoutKind.Sequential)]
  private struct HARDWAREINPUT {
    public int uMsg;
    public short wParamL;
    public short wParamH;
  };

  [StructLayout(LayoutKind.Explicit)]
  private struct INPUT {
    [FieldOffset(0)]
    public int type;
    [FieldOffset(4)]
    public MOUSEINPUT no;
    [FieldOffset(4)]
    public KEYBDINPUT ki;
    [FieldOffset(4)]
    public HARDWAREINPUT hi;
  };

  [DllImport("user32.dll")]
  private extern static void SendInput(int nInputs, ref INPUT pInputs, int cbsize);
  [DllImport("user32.dll", EntryPoint = "MapVirtualKeyA")]
  private extern static int MapVirtualKey(int wCode, int wMapType);

  private const int INPUT_KEYBOARD = 1;
  private const int KEYEVENTF_KEYDOWN = 0x0;
  private const int KEYEVENTF_KEYUP = 0x2;
  private const int KEYEVENTF_EXTENDEDKEY = 0x1;

  private void Send(Keys key, bool isEXTEND) {
    /*
     * Keyを送る
     * 入力
     *     isEXTEND : 拡張キーかどうか
     */

    INPUT inp = new INPUT();

    // 押す
    inp.type = INPUT_KEYBOARD;
    inp.ki.wVk = (short)key;
    inp.ki.wScan = (short)MapVirtualKey(inp.ki.wVk, 0);
    inp.ki.dwFlags = ((isEXTEND) ? (KEYEVENTF_EXTENDEDKEY) : 0x0) | KEYEVENTF_KEYDOWN;
    inp.ki.time = 0;
    inp.ki.dwExtraInfo = 0;
    SendInput(1, ref inp, Marshal.SizeOf(inp));

    System.Threading.Thread.Sleep(100);

    // 離す
    inp.ki.dwFlags = ((isEXTEND) ? (KEYEVENTF_EXTENDEDKEY) : 0x0) | KEYEVENTF_KEYUP;
    SendInput(1, ref inp, Marshal.SizeOf(inp));
  }

}