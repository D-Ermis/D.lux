using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace dlux
{
    static class Program
    {
        static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                try
                {
                    // D.lux is not running, show the main window.
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Form1());
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
            else
                MessageBox.Show("D.lux is already running you stoobid.");

        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct RAMP
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public UInt16[] Red;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public UInt16[] Green;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public UInt16[] Blue;
        }

        public static void SetGamma(double red, double green, double blue)
        {
            if (red < 0.0 || red > 1.0 ||
                green < 0.0 || green > 1.0 ||
                blue < 0.0 || blue > 1.0)
                throw new Exception("Multiplier out of range");

            var ramp = new RAMP()
            {
                Red = new ushort[256],
                Green = new ushort[256],
                Blue = new ushort[256]
            };

            for (int i = 0; i <= 255; i++)
            {
                int value = i;

                ramp.Red[i] = (ushort)(Convert.ToByte(value * red) << 8); // bitwise shift left
                ramp.Green[i] = (ushort)(Convert.ToByte(value * green) << 8); // by 8 
                ramp.Blue[i] = (ushort)(Convert.ToByte(value * blue) << 8); // same as multiplying by 256
            }

            var screenDC = GetDC(IntPtr.Zero);
            var result = SetDeviceGammaRamp(screenDC, ref ramp);
            ReleaseDC(IntPtr.Zero, screenDC); // required otherwise will leak GDI objects

            if (result == false)
                // Can't go below 0.50 (3400K) unless flux is installed
                // and "Expand range" feature activated (flux.exe /unlockwingamma)
                throw new Exception("Failed to set gamma ramp");
        }

        public static void Method5(double intensity, out double Red, out double Green, out double Blue)
        {
            // "intensity" ranges from 0.0 (nighttime) to 1.0 (daytime)
            // Calculated using polynomial trendlines (order=2) 
            Red = 1.0;
            Green = (-0.6409 * Math.Pow(intensity, 2)) + (1.3624 * intensity) + 0.278; // changed from 0.2778 to 0.278 to ensure 1.0 --> 1.000
            Blue = (-0.6 * Math.Pow(intensity, 2)) + (1.9439 * intensity) - 0.3349;

            // Clamp values and convert from 0-1 to 0-255
            if (Green > 1) Green = 1; else if (Green < 0) Green = 0;
            if (Blue > 1) Blue = 1; else if (Blue < 0) Blue = 0;
            Red *= 255;
            Green *= 255;
            Blue *= 255;
        }

        public class TimeValue
        {
            public double Offset { get; set; }
            public int Temp { get; set; }
        }

        public static Dictionary<int, int> BuildTimeOfDayLookup(string tsv)
        {
            var data = tsv
             .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
             .Select(z => z.Split('\t'))
             .Select(z => new TimeValue()
             {
                 Offset = TimeSpan.Parse(z[0]).TotalSeconds,
                 Temp = Convert.ToInt32(z[1])
             }).ToList();


            data.Add(new TimeValue() // ensure it wraps around to midnight correctly
            {
                Offset = TimeSpan.Parse("23:59:59").TotalSeconds + 1, // +1 to include 23:59:59 itself
                Temp = data[0].Temp
            });

            var lookup = new Dictionary<int, int>();
            for (int i = 0; i < data.Count - 1; i++)
            {
                double temp = data[i].Temp; // starting temp for this time period
                double seconds = data[i + 1].Offset - data[i].Offset; // total number of seconds
                double increment = (data[i + 1].Temp - data[i].Temp) / seconds; // how much to increment temp each second

                for (var t = data[i].Offset; t < data[i + 1].Offset; t++)
                {
                    // for each second between the two times,
                    // calculate the temp at that point

                    temp += increment;

                    lookup.Add((int)t, (int)temp);
                }

            }
            return lookup;
        }
    }
}
