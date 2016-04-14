namespace hoge {
  using System;
  using System.IO;
  public class Usage {
    static void Main(string[] args) {
      UnkoChang.Unko.create();
      UnkoChang.Unko.start();
      while(!UnkoChang.Unko.detectBow())
        ;
      Console.WriteLine("detect bowing!");
      UnkoChang.Unko.stop();
    }
  }
}


namespace UnkoChang {
  using System;
  using System.IO;
  using Microsoft.Kinect;
  public sealed class Unko {
    private static Unko unko = new Unko();
    private KinectSensor sensor;
    private short[] depthPixels;
    private short[] previousPixels;
    public bool isBow = false;
    public static Unko create() {
      return unko;
    }
    public static void start() {
      try {
        unko.sensor.Start();
      } catch(IOException) {
        unko.sensor = null;
      }
      Console.WriteLine("kinect started.");
    }
    public static void stop() {
      if(null != unko.sensor) {
        unko.sensor.Stop();
      }
      Console.WriteLine("kinect stoped.");
    }
    public static bool detectBow() {
      return unko.isBow;
    }
    private Unko() {
      foreach(var potentialSensor in KinectSensor.KinectSensors) {
        if(potentialSensor.Status == KinectStatus.Connected) {
          this.sensor = potentialSensor;
          break;
        }
      }
      if(null != this.sensor) {
        this.sensor.DepthStream.Enable(DepthImageFormat.Resolution80x60Fps30);
        this.sensor.DepthStream.Range = DepthRange.Near;
        this.depthPixels = new short[this.sensor.DepthStream.FramePixelDataLength];
        this.previousPixels = new short[this.sensor.DepthStream.FramePixelDataLength];
        this.sensor.DepthFrameReady += this.SensorDepthFrameReady;
      }
    }
    private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e) {
      using(DepthImageFrame depthFrame = e.OpenDepthImageFrame()) {
        if(depthFrame != null) {
          depthFrame.CopyPixelDataTo(this.depthPixels);
          int previousIndex = 0;
          int downforthCount = 0;
          for(int i = 30; i < this.depthPixels.Length; i += 60) {
            for(int j = 0; j < 20; j++) {
              short depth = (short)(this.depthPixels[i] >> DepthImageFrame.PlayerIndexBitmaskWidth);
              if(1100 < depth && depth < 1500) {
                if(this.previousPixels[previousIndex] < depth) {
                  downforthCount++;
                }
              }
              this.previousPixels[previousIndex++] = depth;
              i++;
            }
          }
          if(100 * downforthCount / this.previousPixels.Length > 9) {
            this.isBow = true;
          } else {
            this.isBow = false;
          }
        }
      }
    }
  }
}
