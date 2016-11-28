using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
//using System.Drawing.Image;
using System.Drawing.Imaging;
//using System.Drawing.Imaging.ImageFormat;

using Microsoft.Kinect;
//using Coding4Fun.Kinect;

namespace SKD_Kinect3D_1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        Boolean irCam = false;

        KinectSensor _sensor;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (KinectSensor.KinectSensors.Count > 0)
            {
                _sensor = KinectSensor.KinectSensors[0];

                if (_sensor.Status == KinectStatus.Connected)
                {
                    
                   // _sensor.ColorStream.Enable(ColorImageFormat.InfraredResolution640x480Fps30);
                    _sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                    _sensor.SkeletonStream.Enable(); // required for player detection
                    _sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(_sensor_AllFramesReady);
                    try
                    {
                        _sensor.Start();
                    }
                    catch (System.IO.IOException)
                    {
                        //exception caught
                    }
                }
            }
        }

        private void _sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {

           
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null) return;

                if (!irCam) //RGB
                {
                    byte[] pixels1 = new byte[colorFrame.PixelDataLength];
                    colorFrame.CopyPixelDataTo(pixels1);

                    int stride = colorFrame.Width * 4;
                    image1.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels1, stride);
                }

                else //IR
                {
                    byte[] pixels = new byte[colorFrame.PixelDataLength];
                    colorFrame.CopyPixelDataTo(pixels);

                    int stride = colorFrame.Width * 2;
                    image1.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Gray16, null, pixels, stride);
                }
            }

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame == null) return;
                byte[] pixels = GenerateColoredBytes(depthFrame);

                int stride = depthFrame.Width * 4;
                image3.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
            }
        }

        private byte[] GenerateColoredBytes(DepthImageFrame depthFrame)
        {
            short[] rawDepthData = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(rawDepthData);

            Byte[] pixels = new byte[depthFrame.Width * depthFrame.Height * 4];

            const int blueIndex = 0;
            const int greenIndex = 1;
            const int redIndex = 2;

            for (int depthIndex = 0, colorIndex = 0; depthIndex < rawDepthData.Length && colorIndex < pixels.Length; depthIndex++, colorIndex += 4)
            {
                int depth = rawDepthData[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                byte intensity = calculateIntensityFromDepth(depth);
                pixels[colorIndex + blueIndex] = intensity;
                pixels[colorIndex + greenIndex] = intensity;
                pixels[colorIndex + redIndex] = intensity;
            }

            return pixels;
        }

        private byte calculateIntensityFromDepth(int distance)
        {
            //return (byte)(255 - (255 * Math.Max(distance - 850, 0)));
            int lowerLimit = 900;
            int upperLimit = 4000;

            if (distance <= lowerLimit) return 255;
            if (distance > upperLimit) return 0;

            int range = (upperLimit - lowerLimit);
            int levels = (range / 255);

            return (byte)(255 - (distance / levels));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopKinect(_sensor);
        }

        void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.AudioSource.Stop();
            }
        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            label1.Content = "Kinect Tilt: " + (int) slider1.Value; 
        }

        // Still snaps triggers
        private bool takeSnap = false;
        private bool depthShot = false;

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            _sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(_sensor_TakeSnapshot);
            takeSnap = true;
            depthShot = true;
        }

        private void _sensor_TakeSnapshot(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null) return;
                if (!takeSnap) return;
                takeSnap = false;

                byte[] pixels = new byte[colorFrame.PixelDataLength];
                colorFrame.CopyPixelDataTo(pixels);

                BitmapSource image;

                if (irCam)
                {
                    int stride = colorFrame.Width * 2; //IR
                    image = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Gray16, null, pixels, stride); //IR
                    image2.Source = image;
                }
                else {
                    int stride = colorFrame.Width * 4; // RGB
                    image = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride); //RGB
                    image2.Source = image;
                }
                
                
                

                int index = 0;
                label2.Content = pixels.Length;
                label_blue.Content = pixels[index + 0];
                label_green.Content = pixels[index + 1];
                label_red.Content = pixels[index + 2];
                label_empty.Content = pixels[index + 3];

                label_Width.Content = colorFrame.Width;
                label_height.Content = colorFrame.Height;

                String fName = string.Format("{0:HH:mm:ss tt}", DateTime.Now);

                String cfName = "";
                for(int i=0;i<8;i++)
                    if(fName[i]!=':' && fName[i]!=' ') cfName+=fName[i];

                label2.Content = cfName+".txt";

                System.IO.StreamWriter file = new System.IO.StreamWriter(cfName+".txt", true);
                file.Write(colorFrame.Width.ToString()+"#"+colorFrame.Height.ToString()+"#");
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (i % 4 == 3) continue;
                    string s = pixels[i].ToString();
                    file.Write(s+'$');
                }

                file.Close();

                FileStream stream = new FileStream(cfName+".png", FileMode.Create);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                TextBlock myTextBlock = new TextBlock();
                myTextBlock.Text = "Codec Author is: " + encoder.CodecInfo.Author.ToString();
                encoder.Interlace = PngInterlaceOption.On;
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);

                stream.Close();

                //Image image = Image.FromStream(new MemoryStream(pixels));

                /*
                var fs = new BinaryWriter(new FileStream(@"C:\\tmp\\" + filename + ".ico", FileMode.Append, FileAccess.Write));
                fs.Write(imageByteArray);
                fs.Close();
                */
                /*
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                image2.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                byte[] ar = new byte[ms.Length];
                ms.Write(ar, 0, ar.Length);
                 
                */
            }

           
            _sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(_sensor_TakeDepthshot);
                

        }

        private void _sensor_TakeDepthshot(object sender, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame == null) return;
                if (!depthShot) return;
                depthShot = false;

                byte[] pixels = GenerateColoredBytes(depthFrame);

                label_max.Content = depthFrame.Width;
                label_min.Content = depthFrame.Height;

                int stride = depthFrame.Width * 4;
                image4.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);


                //Writing depth to a file
                short[] rawDepthData = new short[depthFrame.PixelDataLength];
                depthFrame.CopyPixelDataTo(rawDepthData);

                Console.WriteLine(string.Format("{0:HH:mm:ss tt}", DateTime.Now));

                System.IO.StreamWriter file = new System.IO.StreamWriter("testDepth.txt", true);
                file.Write(depthFrame.Width.ToString() + "#" + depthFrame.Height.ToString() + "#");
                for (int i = 0; i < rawDepthData.Length; i++)
                {
                    int depth = rawDepthData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                    string s = depth.ToString();
                    file.Write(s + '$');
                }
                file.Close();
  
            }
            
        }



        //kinect tilt adjust
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (_sensor != null) _sensor.ElevationAngle = (int)slider1.Value;
        }

        private void radioColorCam_Checked(object sender, RoutedEventArgs e)
        {
            StopKinect(_sensor);
            _sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            irCam = false;
            try
            {
                _sensor.Start();
            }
            catch (System.IO.IOException)
            {
                //exception caught
            }
             
        }

        private void radioIRCam_Checked(object sender, RoutedEventArgs e)
        {
            StopKinect(_sensor);
            _sensor.ColorStream.Enable(ColorImageFormat.InfraredResolution640x480Fps30);
            irCam = true;
            
            try
            {
                _sensor.Start();
            }
            catch (System.IO.IOException)
            {
                //exception caught
            }
             
        }
    }
}
