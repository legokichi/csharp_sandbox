using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Linq;

namespace ConsoleApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Chasen chasen = new Chasen("COM8", 20);
            while (true)
            {
                chasen.read();
                Console.WriteLine("");
            }
        }
    }
}

public class Chasen
{
    private SerialPort serial;
    private float[] xs;
    private float[] ys;
    private float[] zs;
    private float[] lwmaxs;
    private float[] lwmays;
    private float[] lwmazs;
    private float[] autocorrxs;
    private float[] autocorrys;
    private float[] autocorrzs;
    private int id = 0;
    public float concentration;
    public float frequency;
    public Chasen(string port, int length)
    {
        serial = new SerialPort(port, 9600);
        serial.Open();
        xs = new float[length];
        ys = new float[length];
        zs = new float[length];
        lwmaxs = new float[length];
        lwmays = new float[length];
        lwmazs = new float[length];
        int i = length;
        while (i-- != 0){
            xs[i] = ys[i] = zs[i] = lwmaxs[i] = lwmays[i] = lwmazs[i] = autocorrxs[i] = autocorrys[i] = autocorrzs[i] = 678;
        }
    }
    public void read()
    {
        string[] ary = serial.ReadLine().Split(',');
        if (ary.Length != 3) return; // error data
        xs[id] = int.Parse(ary[0]);
        ys[id] = int.Parse(ary[1]);
        zs[id] = int.Parse(ary[2]);
        lwmaxs[id] = lwma(xs);
        lwmays[id] = lwma(ys);
        lwmazs[id] = lwma(zs);
        Array.Copy(lwmaxs, 0, autocorrxs, id, id + 1); Array.Copy(lwmaxs, id, autocorrxs, 0, xs.Length - id - 1);
        Array.Copy(lwmays, 0, autocorrys, id, id + 1); Array.Copy(lwmays, id, autocorrys, 0, ys.Length - id - 1);
        Array.Copy(lwmazs, 0, autocorrzs, id, id + 1); Array.Copy(lwmazs, id, autocorrzs, 0, zs.Length - id - 1);
        autocorrxs = autocorr(autocorrxs);
        autocorrys = autocorr(autocorrys);
        autocorrzs = autocorr(autocorrzs);
        if (++id >= xs.Length) id = 0;
    }
    private float lwma(float[] ary)
    {
        float _0 = 0, _1 = 0;
        for (int i = 0, j = ary.Length - 1; i < ary.Length; i++, j--)
        {
            _0 += ary[i] * j;
            _1 += j;
        }
        return _0 / _1;
    }
    private float[] autocorr(float[] v){
        int i, j, N, n;
        n = v.Length;
        float[] r = new float[n];
　　　  float s;
　　　  N = n/2;
　　　  for (j = 0; j <= N; j++) {
　　　　    s = 0;
　　　　　　for (i = 1; i <= N; i++) {
                s = s + v[i] * v[i+j];
            }
            r[j] = s;
        }
        if (r[0] != 0) for (j = 0; j <= N; j++) r[j] = r[j] / r[0];
        return r;
    }
}
