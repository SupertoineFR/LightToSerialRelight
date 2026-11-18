using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using BS_Utils.Utilities;
using Zenject;

namespace LightOut
{
    class EventListener : IInitializable, IDisposable
    {
        private readonly BeatmapCallbacksController Ec;
        private readonly ColorManager Cm;
        private readonly BeatmapObjectManager _spawnController;
        private readonly IGamePause _gamePause;
        private BeatmapDataCallbackWrapper _eventCallback;
        private bool _noteCutSubscribed;
        private Color C1;
        private Color C2;
        public int redLeft;
        public int greenLeft;
        public int blueLeft;
        public int redRight;
        public int greenRight;
        public int blueRight;
        float lastEventTime;
        float lastNoteCutTime = float.MinValue;
        float maxArduinoDelay = 15; //Time in miliseconds in which arduino goes through one read cycle. You can calculate your own time by
        // printing char each cycle -> going into serial monitor (Arduino IDE) -> enabling timestamps => Subtract earlier time from newer time.

        bool right = true;

        public EventListener(BeatmapCallbacksController callbacksController, ColorManager colorManager, BeatmapObjectManager beatmapObjectManager, IGamePause gamePause)
        {
            Ec = callbacksController;
            Cm = colorManager;
            _spawnController = beatmapObjectManager;
            _gamePause = gamePause;
        }

        private static bool Active =>
            Settings.instance._isModEnabled && Settings.arduinoPort != null && Settings.arduinoPort.IsOpen;

        public void Initialize()
        {
            if (!Active)
                return;

            Plugin.log.Notice("Initializing..");
            Plugin.log.Notice(Settings.instance.eventChoice);

            _gamePause.didPauseEvent += OnGamePaused;

            if (Settings.instance.eventChoice == "noteCuts")
            {
                Plugin.log.Info("Adding event listner to noteCuts");
                _spawnController.noteWasCutEvent += OnNoteCut;  //Flash on note cuts
                _noteCutSubscribed = true;
            }

            if (Settings.instance.eventChoice == "lightEvents")
            {
                Plugin.log.Info("Adding event listner to lightEvents");
                _eventCallback = Ec.AddBeatmapCallback<BasicBeatmapEventData>(EventHappened); //Flash on map's light events
            }

            C1 = Cm.ColorForType(ColorType.ColorB);
            redLeft = Mathf.RoundToInt(C1.r * 255);
            greenLeft = Mathf.RoundToInt(C1.g * 255);
            blueLeft = Mathf.RoundToInt(C1.b * 255);
            C2 = Cm.ColorForType(ColorType.ColorA);
            redRight = Mathf.RoundToInt(C2.r * 255);
            greenRight = Mathf.RoundToInt(C2.g * 255);
            blueRight = Mathf.RoundToInt(C2.b * 255);

            if (Settings.instance.rainbowMode)
            {
                StartRainbowMode(Settings.arduinoPort);
            }
            else
            {
                Plugin.log.Info("Sending Color to arduino...");
                SendColorToArduino(Settings.arduinoPort);
            }
        }

        public void Dispose()
        {
            _gamePause.didPauseEvent -= OnGamePaused;
            if (_noteCutSubscribed)
                _spawnController.noteWasCutEvent -= OnNoteCut;
            if (_eventCallback != null)
                Ec.RemoveBeatmapCallback(_eventCallback);

            if (Settings.arduinoPort != null && Settings.arduinoPort.IsOpen)
            {
                Settings.arduinoPort.DiscardOutBuffer(); //Drop any effects that haven't been sent yet
                Settings.arduinoPort.Write("#");
                Plugin.log.Info("Removing Eventlistener");
            }
        }

        int lastEventNumber = 0;

        void EventHappened(BasicBeatmapEventData Data)
        {
            int Event;
            int value = Data.value;
            Int32.TryParse(Data.basicBeatmapEventType.ToString().Replace("Event", ""), out Event);
            if (value < 2000000000)
            {
                if(lastEventNumber != value && Data.time > (lastEventTime + maxArduinoDelay*0.001)) //Make sure to never send same signal twice AND do not oversend, otherwise arduino will display beats too late.
                {
                    Plugin.log.Info("Event happened: " + value.ToString());
                    if (Settings.instance.rainbowMode)
                    {
                        Settings.arduinoPort.Write("^");
                    }
                    switch (value)
                    {
                        case 0:
                            Settings.arduinoPort.Write("p"); //Turn Off
                            break;
                        case 1:
                            Settings.arduinoPort.Write("q"); //RightTurnOn
                            break;
                        case 2:
                            Settings.arduinoPort.Write("w"); //RightFlashAndLeaveOn
                            break;
                        case 3:
                            Settings.arduinoPort.Write("e"); //RightFlashAndTurnOff (arduino turns itself off after the flash)
                            break;
                        case 5:
                            Settings.arduinoPort.Write("r"); //LeftTurnOn
                            break;
                        case 6:
                            Settings.arduinoPort.Write("t"); //LeftFlashAndLeaveOn
                            break;
                        case 7:
                            Settings.arduinoPort.Write("y"); //LeftFlashAndTurnOff (arduino turns itself off after the flash)
                            break;
                        default:
                            Settings.arduinoPort.Write("p"); //Turn off on error
                            break;
                    }
                    lastEventNumber = value;
                    lastEventTime = Data.time;
                }
            }
        }
        
        private void OnNoteCut(NoteController noteController, in NoteCutInfo info)
        {
            //Same throttle as light events, so fast streams don't flood the arduino.
            float now = Time.realtimeSinceStartup;
            if (now < lastNoteCutTime + maxArduinoDelay * 0.001f)
                return;
            lastNoteCutTime = now;

            Plugin.log.Notice("Note Cut Event!");
            if (Settings.instance.rainbowMode)
            {
                Settings.arduinoPort.Write("^");
            }

            if (right)
            {
                Settings.arduinoPort.Write("q"); //RightTurnOn
                right = !right;
            }
            else
            {
                Settings.arduinoPort.Write("r"); //LeftTurnOn
                right = !right;
            }
        }

        private void OnGamePaused()
        {
            if (Settings.arduinoPort == null || !Settings.arduinoPort.IsOpen)
                return;

            //Throw away anything still queued on the PC side and turn the lights off.
            Settings.arduinoPort.DiscardOutBuffer();
            Settings.arduinoPort.Write("p");
        }

        private void StartRainbowMode(SerialPort port)
        {
            port.Write(new char[] { '^' }, 0, 1); //Starts rainbow mode
        }

        void SendColorToArduino(SerialPort port)
        {
            Plugin.log.Debug("Before color boost: " + redLeft + " " + greenLeft + " " + blueLeft);
            Plugin.log.Debug("Before color boost: " + redRight + " " + greenRight + " " + blueRight);

            int r1, g1, b1;
            int r2, g2, b2;

            System.Drawing.Color colorLeft = System.Drawing.Color.FromArgb(redLeft,greenLeft,blueLeft);
            System.Drawing.Color colorRight = System.Drawing.Color.FromArgb(redRight, greenRight, blueRight);

            double h1 = colorLeft.GetHue();
            double h2 = colorRight.GetHue();

            Plugin.log.Debug("double h1 = " + h1);
            Plugin.log.Debug("double h2 = " + h2);

            HsvToRgb(h1, 1, 1, out r1, out g1, out b1);
            HsvToRgb(h2, 1, 1, out r2, out g2, out b2);

            Plugin.log.Debug("After color boost: " + r1 +" "+ g1 +" "+ b1);
            Plugin.log.Debug("After color boost: " + r2 + " " + g2 + " " + b2);

            Color rightColor = new Color(r1,g1,b1);
            Color leftColor = new Color(r2, g2, b2);

            decimal p_r = Convert.ToDecimal(redLeft.ToString()); //Primary Red
            decimal p_g = Convert.ToDecimal(greenLeft.ToString()); //Primary Green
            decimal p_b = Convert.ToDecimal(blueLeft.ToString()); //Primary Blue
            decimal s_r = Convert.ToDecimal(redRight.ToString()); //Secondary Red
            decimal s_g = Convert.ToDecimal(greenRight.ToString()); //Secondary Green
            decimal s_b = Convert.ToDecimal(blueRight.ToString()); //Secondary Blue

            byte[] colorsByte = new byte[8];
            colorsByte[0] = Convert.ToByte('$');
            colorsByte[1] = Convert.ToByte(p_r);
            colorsByte[2] = Convert.ToByte(p_g);
            colorsByte[3] = Convert.ToByte(p_b);
            colorsByte[4] = Convert.ToByte(s_r);
            colorsByte[5] = Convert.ToByte(s_g);
            colorsByte[6] = Convert.ToByte(s_b);
            colorsByte[7] = Convert.ToByte('%');

            for(int x = 0; x < colorsByte.Length; x++)
            {
                try
                {
                    Plugin.log.Info("Writing: " + colorsByte[x]);
                }
                catch (Exception e)
                {
                    Plugin.log.Error(e);
                }

            }
            port.Write(colorsByte, 0, 8); 
        }

        public static void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            // ######################################################################
            // T. Nathan Mundhenk
            // mundhenk@usc.edu
            // C/C++ Macro HSV to RGB

            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        public static int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
    }
}
