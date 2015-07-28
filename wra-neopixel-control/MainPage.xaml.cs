using Microsoft.Maker.Firmata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Gpio;
using System.Threading;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace wra_neopixel_control
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int NEOPIXEL_SET_COMMAND = 0x42;
        private const int NEOPIXEL_SHOW_COMMAND = 0x44;
        private const int NUMBER_OF_PIXELS = 30;
        private int count = 0;
        private int count2 = 0;

        private DispatcherTimer timer;
        private DispatcherTimer timer2;
        private DispatcherTimer timer3;
        private DispatcherTimer timer4;

      
        private const int LED_PIN = 6;
        private const int BUTTON_PIN = 5;
        private GpioPin ledPin; 
        private GpioPin buttonPin;
        private GpioPinValue ledPinValue = GpioPinValue.High;


        private AudioGraph graph;
        private AudioFileInputNode fileInput;
        private AudioDeviceOutputNode deviceOutput;

        private MediaElement media;

        int[,] blendedRainbow;
        byte[] rainbowRed = { 255, 255, 255, 128, 0, 0, 0, 0, 0, 127 };
        byte[] rainbowGreen = { 0, 128, 255, 255, 255, 255, 255, 255, 0, 0 };
        byte[] rainbowBlue = { 0, 0, 0, 0, 0, 128, 255, 255, 255, 255 };
        private UwpFirmata firmata;

        /// <summary>
        /// This page uses advanced features of the Windows Remote Arduino library to carry out custom commands which are
        /// defined in the NeoPixel_StandardFirmata.ino sketch. This is a customization of the StandardFirmata sketch which
        /// implements the Firmata protocol. The customization defines the behaviors of the custom commands invoked by this page.
        /// 
        /// To learn more about Windows Remote Arduino, refer to the GitHub page at: https://github.com/ms-iot/remote-wiring/
        /// To learn more about advanced behaviors of WRA and how to define your own custom commands, refer to the
        /// advanced documentation here: https://github.com/ms-iot/remote-wiring/blob/develop/advanced.md
        /// </summary>
        public MainPage()
        {
            
            this.InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(80);
            timer.Tick += SetAllPixelsRandomly;

            timer2 = new DispatcherTimer();
            timer2.Interval = TimeSpan.FromMilliseconds(100);
            timer2.Tick += CyclePixels;

            timer3 = new DispatcherTimer();
            timer3.Interval = TimeSpan.FromMilliseconds(100);
            timer3.Tick += BlendPixels;

            timer4 = new DispatcherTimer();
            timer4.Interval = TimeSpan.FromMilliseconds(50);
            timer4.Tick += SetPixelsByVolume;

            //blendedRainbow =  initBlend(100);
            //initGraph();
            initMedia();
            InitGPIO();
            firmata = App.Firmata;
        }

        /// <summary>
        /// This button callback is invoked when the buttons are pressed on the UI. It determines which
        /// button is pressed and sets the LEDs appropriately
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Color_Click( object sender, RoutedEventArgs e )
        {
            timer.Stop();
            timer2.Stop();
            timer3.Stop();
            timer4.Stop();
            var button = sender as Button;
            switch( button.Name )
            {
                case "Red":
                    timer4.Start();
                    break;
                
                case "Green":
                    SetAllPixelsAndUpdate( 0, 255, 0 );
                    break;

                case "Blue":
                    SetAllPixelsAndUpdate( 0, 0, 255 );
                    break;

                case "Yellow":
                    SetAllPixelsAndUpdate( 255, 255, 0 );
                    break;

                case "Cyan":
                    SetAllPixelsAndUpdate( 0, 255, 255 );
                    break;

                case "Magenta":
                    SetAllPixelsAndUpdate( 255, 0, 255 );
                    break;
                case "Rainbow":
                    timer.Start();
                    break;
                case "MovingRainbow":
                    timer2.Start();
                    break;
                case "BlendingRainbow":
                    timer3.Start();
                    break;

            }
        }

        /// <summary>
        /// Sets all the pixels to the given color values and calls UpdateStrip() to tell the NeoPixel library to show the set colors.
        /// </summary>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        private void SetAllPixelsAndUpdate( byte red, byte green, byte blue )
        {
            SetAllPixels( red, green, blue );
            UpdateStrip();
        }

        /// <summary>
        /// Sets all the pixels to the given color values
        /// </summary>
        /// <param name="red">The amount of red to set</param>
        /// <param name="green">The amount of green to set</param>
        /// <param name="blue">The amount of blue to set</param>
        private void SetAllPixelsRandomly(object sender, object e)
        {
            Random r = new Random();
            for (byte i = 0; i < NUMBER_OF_PIXELS; ++i)
            {
                byte red = (byte) r.Next(255);
                byte green = (byte) r.Next(255);
                byte blue = (byte) r.Next(255);
                SetPixel(i, red, green, blue);
            }
            UpdateStrip();
        }

        private void SetPixelsByVolume(object sender, object e)
        {
            double vol = media.Volume;
            System.Diagnostics.Debug.WriteLine(media.Volume);
            int end = (int) (NUMBER_OF_PIXELS * vol);
            for (byte i = 0; i < end; i++)
            {
                SetPixel(i, rainbowRed[i % 10], rainbowGreen[i % 10], rainbowBlue[i % 10]);
            }
            UpdateStrip();
        }

        private void CyclePixels(object sender, object e)
        {
            count += 1; 
            for (byte i = 0; i < NUMBER_OF_PIXELS; ++i)
            {
                SetPixel(i, rainbowRed[(count+ i) % 10], rainbowGreen[(count+ i) % 10], rainbowBlue[(count+ i) % 10]);
            }
            UpdateStrip();
        }

        /// <summary>
        /// Sets all the pixels to the given color values
        /// </summary>
        /// <param name="red">The amount of red to set</param>
        /// <param name="green">The amount of green to set</param>
        /// <param name="blue">The amount of blue to set</param>
        private void SetAllPixels( byte red, byte green, byte blue )
        {
            for( byte i = 0; i < NUMBER_OF_PIXELS; ++i )
            {
                SetPixel( i, red, green, blue );
            }
        }

        /// <summary>
        /// Sets a single pixel to the given color values
        /// </summary>
        /// <param name="red">The amount of red to set</param>
        /// <param name="green">The amount of green to set</param>
        /// <param name="blue">The amount of blue to set</param>
        private void SetPixel( byte pixel, byte red, byte green, byte blue )
        {
            firmata.beginSysex( NEOPIXEL_SET_COMMAND );
            firmata.appendSysex( pixel );
            firmata.appendSysex( red );
            firmata.appendSysex( green );
            firmata.appendSysex( blue );
            firmata.endSysex();
        }

        private void BlendPixels(object sender, object e)
        {
            count += 1;
            byte r = (byte) blendedRainbow[count % blendedRainbow.Length, 0];
            byte g = (byte)blendedRainbow[count % blendedRainbow.Length, 1];
            byte b = (byte)blendedRainbow[count % blendedRainbow.Length, 2];
            SetAllPixelsAndUpdate(r, g, b);
        }

        /// <summary>
        /// Tells the NeoPixel strip to update its displayed colors.
        /// This function must be called before any colors set to pixels will be displayed.
        /// </summary>
        /// <param name="red">The amount of red to set</param>
        /// <param name="green">The amount of green to set</param>
        /// <param name="blue">The amount of blue to set</param>
        private void UpdateStrip()
        {
            firmata.beginSysex( NEOPIXEL_SHOW_COMMAND );
            firmata.endSysex();
        }

        private int[,] initBlend(int numIntervals)
        {
            int total_len = (rainbowRed.Length-1) * numIntervals;
            int[,] fullRainbow = new int[total_len, 3];
            for (int i = 0; i < rainbowRed.Length - 1; i++)
            {
                int[] val1 = { rainbowRed[i], rainbowGreen[i], rainbowBlue[i] };
                int[] val2 = { rainbowRed[i + 1], rainbowGreen[i + 1], rainbowBlue[i + 1] };
                int[,] interval = getInterval(val1, val2, numIntervals);
                for (int j = 0; j < numIntervals; j++)
                {
                    fullRainbow[j + (i * rainbowRed.Length), 0] = interval[j, 0];
                    fullRainbow[j + (i * rainbowRed.Length), 1] = interval[j, 1];
                    fullRainbow[j + (i * rainbowRed.Length), 2] = interval[j, 2];
                }
            }
            return fullRainbow;
        }

        private int[,] getInterval(int[] start, int[] end, int numIntervals)
        {
            var interval_R = (end[0] - start[0]) / numIntervals;
            var interval_G = (end[1] - start[1]) / numIntervals;
            var interval_B = (end[2] - start[2]) / numIntervals;

            var R = start[0];
            var G = start[1];
            var B = start[2];
            int[,] colors = new int[numIntervals,3];
            
            for (var i = 0; i <= numIntervals; i++)
            {
                colors[0, 0] = R; colors[0, 1] = G; colors[0, 2] = B;
                R += interval_R;
                G += interval_G;
                B += interval_B;
            }
            return colors;
        }
        
        private  System.Collections.IEnumerable yieldRGB()
        {
            int[,] rainBow = blendedRainbow;
            for (int i = 0; i < rainBow.Length; i++)
            {
                int[] container = { rainBow[i, 0], rainBow[i, 1], rainBow[i, 2] };
                yield return container;
            }
        }

        private async Task<StorageFile> GetPackagedFile(string folderName, string fileName)
        {
            StorageFolder installFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            if (folderName != null)
            {
                StorageFolder subFolder = await installFolder.GetFolderAsync(folderName);
                return await subFolder.GetFileAsync(fileName);
            }
            else
            {
                return await installFolder.GetFileAsync(fileName);
            }
        }

        private async void initGraph() {
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Media);
            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            graph = result.Graph;
            // Create a device output node
            CreateAudioDeviceOutputNodeResult deviceOutputNodeResult = await graph.CreateDeviceOutputNodeAsync();
            deviceOutput = deviceOutputNodeResult.DeviceOutputNode;


            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            filePicker.FileTypeFilter.Add(".mp3");
            filePicker.FileTypeFilter.Add(".wav");
            StorageFile file = await GetPackagedFile(null, "audio.mp3");
            CreateAudioFileInputNodeResult fileInputResult = await graph.CreateFileInputNodeAsync(file);
            fileInput = fileInputResult.FileInputNode;
            fileInput.AddOutgoingConnection(deviceOutput);
            graph.Start();
        }

        private void initMedia() {
            IRandomAccessStream stream = new FileStream(@"audio.mp3", FileMode.Open, FileAccess.Read).AsRandomAccessStream();
            media = new MediaElement();
            media.AutoPlay = true;
            media.SetSource(stream, "audio/mpeg3");
            //media.Play()
        }

        private void buttonPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
           
            // toggle the state of the LED every time the button is pressed
            string[] colors = new string[8] { "red", "blue", "green", "yellow","cyan","magenta","rainbow","moving rainbow" };
           
            if (e.Edge == GpioPinEdge.FallingEdge)
            {
                if (colors[count2%8] == "red")
                {
                    SetAllPixelsAndUpdate(255, 0, 0);
                }

                if (colors[count2%8] == "blue")
                {
                    SetAllPixelsAndUpdate(0, 255, 0);
                }
                if (colors[count2%8] == "green")
                {
                    SetAllPixelsAndUpdate(0, 0, 255);
                }
                if (colors[count2%8] == "yellow")
                {
                    SetAllPixelsAndUpdate(255, 255, 0);
                }
                if (colors[count2 % 8] == "cyan")
                {
                    SetAllPixelsAndUpdate(0, 255, 255);
                }
                if (colors[count2 % 8] == "magenta")
                {
                    SetAllPixelsAndUpdate(255, 0, 255);
                }
                if (colors[count2 % 8] == "rainbow")
                {
                   
                        SetAllPixelsRandomly(null, null);
                    
                }
                if (colors[count2 % 8] == "moving rainbow")
                {
                        CyclePixels(null, null);
                }
                count2++;
                count2 = count2 % 8;
            }
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();            
            buttonPin = gpio.OpenPin(BUTTON_PIN);
            ledPin = gpio.OpenPin(LED_PIN);

            // Check if input pull-up resistors are supported
            if (buttonPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
                buttonPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            else
                buttonPin.SetDriveMode(GpioPinDriveMode.Input);

            // Set a debounce timeout to filter out switch bounce noise from a button press
            buttonPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);

            // Register for the ValueChanged event so our buttonPin_ValueChanged 
            // function is called when the button is pressed
            buttonPin.ValueChanged += buttonPin_ValueChanged;
        }



    }
}
