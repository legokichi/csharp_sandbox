using System;
using System.Windows.Forms;
using TETCSharpClient;
using TETCSharpClient.Data;

class Program {
  static void Main(string[] args) {
    Application.ApplicationExit += new EventHandler(Exit);
    Console.WriteLine("start");
    REPL();
  }

  static void Exit(object sender, EventArgs e) {
    Console.WriteLine("exit.");
  }

  static void REPL() {
    while(true) {
      string input = Console.ReadLine();
      if(input == "exit") {
        break;
      }
      string output = Eval(input);
      Console.WriteLine(output);
    }
  }

  static string Eval(string input) {
    string[] op = input.Replace("  ", " ").Split(' ');
    if(op[0] == "activate") {
      return GazeManager.Instance.Activate(GazeManager.ApiVersion.VERSION_1_0, GazeManager.ClientMode.Push).ToString();
    } else if(op[0] == "deactivate") {
      GazeManager.Instance.Deactivate();
      return "";
    } else if(op[0] == "Calibration") { 
      GazeManager.Instance.CalibrationPointStart(int.Parse(op[1]), int.Parse(op[2]));
      return "";
    } else {
      return input;
    }
  }
}

