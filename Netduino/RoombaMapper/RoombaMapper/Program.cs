using System;
//using System.Net;
//using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
//using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace RoombaMapper
{
    public class Program
    {
        #region [---Variables and Constants--------------------]
        private const int POLLING_PERIOD_MS = 390;
        private const int STARTUP_DELAY = 5000;
        private const string VERSION = "0.2.0";
        // ------------------------------------ //

        private static InterruptPort PushButton;
        private static RoombaSerial roomba;
        private static WiFiClient WiFi;
        private static Thread WiFiThread;
        private static Logger SDCard;
        private static bool ProgramHalted = false;
        private static bool DriveAround = false;
        #endregion

        #region [---Main---------------------------------------]
        public static void Main()
        {
            Thread.Sleep(STARTUP_DELAY);

            TRACE("Main", "-- RoombaMapper v" + VERSION + "--");
            string sensors;
            Init();

            while (true)
            {
                Thread.Sleep(POLLING_PERIOD_MS);

                if (ProgramHalted) continue;

                if (DriveAround)
                {
                    DriveAround = false;
                    roomba.DriveAround();
                }

                sensors = roomba.ReadSensors(RoombaSerial.SensorData.All);

                if (null == sensors)
                    continue;

                if (!FifoBuffer.Push(sensors))      // Log to Server
                    TRACE("FifoBuffer.Push", "Buffer is Full");

                
                if (!SDCard.Log(sensors,true))   // Log to Storage
                    TRACE("Main", "Logging to SD card failed.");
                
            }

        }

        private static void Init()
        {
            // WiFi //
            WiFi = new WiFiClient(Defines.HOST_IP, Defines.HOST_PORT);
            WiFiThread = new Thread(WiFi.Run);
            WiFiThread.Start();

            // Roomba //
            roomba = new RoombaSerial(SerialPorts.COM1, Pins.GPIO_PIN_D4);

            // SDCard //
            SDCard = new Logger(Defines.LOG_FILENAME);

            // Session ID //
            int sid = GetRandomNumber();
            string SessionID = sid.ToString("X4");
            string new_session = "--- NEW SESSION {" + SessionID + "} VERSION {" + VERSION + "}---";
            FifoBuffer.Push(new_session);
            SDCard.Log(new_session);

            // Pushbutton //
            PushButton = new InterruptPort(Pins.ONBOARD_BTN, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
            PushButton.OnInterrupt += new NativeEventHandler(OnPushButton);
        }

        private static void OnPushButton(uint data1, uint data2, DateTime time)
        {
            DriveAround = true;
            return;
            
            if (ProgramHalted)
            {
                TRACE("OnPushButton", "Program resumed.");
                ProgramHalted = false;
                WiFiThread.Resume();
            }
            else
            {
                TRACE("OnPushButton", "Program halted.");
                ProgramHalted = true;
                WiFiThread.Suspend();
            }
        }
        #endregion

        #region [---Auxilliary---------------------------------]
        private static int GetRandomNumber()
        {
            var R0 = new AnalogInput(AnalogChannels.ANALOG_PIN_A0);
            var R1 = new AnalogInput(AnalogChannels.ANALOG_PIN_A1);
            var R2 = new AnalogInput(AnalogChannels.ANALOG_PIN_A2);
            var R3 = new AnalogInput(AnalogChannels.ANALOG_PIN_A3);
            var R4 = new AnalogInput(AnalogChannels.ANALOG_PIN_A4);

            int num = (int)((R0.ReadRaw() + 1) * (R1.ReadRaw() + 1) * (R2.ReadRaw() + 1) * (R3.ReadRaw() + 1) * (R4.ReadRaw() + 1));
            num = num & (0xFFFF);
            return num;
        }
        
        private static void TRACE(string func, string msg)
        {
            #if DEBUG
            TimeSpan ts = Microsoft.SPOT.Hardware.Utility.GetMachineTime();
            Debug.Print("[" + ts.ToString() + "] [Program." + func + "()] " + msg);
            #endif
        }
        #endregion
    }

    public class Defines
    {
        public const string HOST_IP = "10.0.0.16";
        public const int    HOST_PORT = 8888;
        public const string LOG_FILENAME = "RoombaMapperLog.txt";
    }
}
