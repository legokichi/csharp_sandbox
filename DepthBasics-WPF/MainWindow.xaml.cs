namespace DepthBasics {
  using System;
  using System.IO;
  using System.Windows;
  using System.Windows.Media;
  using System.Windows.Media.Imaging;
  using Microsoft.Kinect;
  public partial class MainWindow:Window {
    private KinectSensor sensor;
    private WriteableBitmap colorBitmap;
    private short[] depthPixels;
    private short[] previousPixels;
    private byte[] colorPixels;
    public bool isBow = false;
    public MainWindow() {
      InitializeComponent();
    }
    private void WindowLoaded(object sender, RoutedEventArgs e) {
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
        this.colorPixels = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];
        this.colorBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
        this.Image.Source = this.colorBitmap;
        this.sensor.DepthFrameReady += this.SensorDepthFrameReady;
        try {
          this.sensor.Start();
        } catch(IOException) {
          this.sensor = null;
        }
      }
    }
    private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
      if(null != this.sensor) {
        this.sensor.Stop();
      }
    }
    private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e) {
      using(DepthImageFrame depthFrame = e.OpenDepthImageFrame()) {
        if(depthFrame != null) {
          depthFrame.CopyPixelDataTo(this.depthPixels);
          int colorPixelIndex = 120;
          int previousIndex = 0;
          int downforthCount = 0;
          for(int i = 30; i < this.depthPixels.Length; i += 60) {
            for(int j = 0; j < 20; j++) {
              short depth = (short)(this.depthPixels[i] >> DepthImageFrame.PlayerIndexBitmaskWidth);
              byte intensity = (byte)((depth + 1) & byte.MaxValue);
              this.colorPixels[colorPixelIndex] = intensity;
              this.colorPixels[colorPixelIndex + 1] = intensity;
              this.colorPixels[colorPixelIndex + 2] = intensity;
              if(1100 < depth && depth < 1500) {
                if(this.previousPixels[previousIndex] < depth) {
                  this.colorPixels[colorPixelIndex] = 0;
                  this.colorPixels[colorPixelIndex + 1] = 127;
                  this.colorPixels[colorPixelIndex + 2] = 0;
                  downforthCount++;
                }
              }
              this.previousPixels[previousIndex++] = depth;
              colorPixelIndex += 4;
              i++;
            }
            colorPixelIndex += 240;
          }
          if(100 * downforthCount / this.previousPixels.Length > 9) {
            Console.WriteLine("Bow.");
            this.isBow = true;
          } else {
            Console.WriteLine(downforthCount);
            this.isBow = false;
          }
          this.colorBitmap.WritePixels(
              new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
              this.colorPixels,
              this.colorBitmap.PixelWidth * sizeof(int),
              0);
        }
      }
    }
  }
}