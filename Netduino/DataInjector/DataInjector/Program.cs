using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace DataInjector
{
    public class Program
    {
        private const int POLLING_PERIOD_MS = 25;
        private const int STARTUP_DELAY = 4000;
        private const string VERSION = "1.0B";
        // ------------------------------------ //
        private static WiFiClient WiFi;
        private static Thread WiFiThread;
        private static Logger SDCard;

        public static void Main()
        {
            Thread.Sleep(STARTUP_DELAY);

            TRACE("Main", "-- DataInjector v" + VERSION + "--");
            string sensors;
            Init();

            while(true)
            {
                Thread.Sleep(POLLING_PERIOD_MS);

                sensors = SDCard.ReadLine();

                if (null == sensors)
                    continue;

                if (!FifoBuffer.Push(sensors))
                    TRACE("FifoBuffer.Push", "Buffer is full.");

            }


        }

        private static void Init()
        {
            // WiFi //
            WiFi = new WiFiClient(Defines.HOST_IP, Defines.HOST_PORT);
            WiFiThread = new Thread(WiFi.Run);
            WiFiThread.Start();

            // SDCard //
            SDCard = new Logger(Defines.LOG_FILENAME, true);

            // Session ID //
            string new_session = "--- NEW SESSION {INJECTED_SCRIPT} VERSION {" + VERSION + "}---";
            FifoBuffer.Push(new_session);
        }


        private static void TRACE(string func, string msg)
        {
            #if DEBUG
            TimeSpan ts = Microsoft.SPOT.Hardware.Utility.GetMachineTime();
            Debug.Print("[" + ts.ToString() + "] [Program." + func + "()] " + msg);
            #endif
        }

    }

    public class Defines
    {
        public const string HOST_IP = "10.0.0.16";
        public const int HOST_PORT = 8888;
        public const string LOG_FILENAME = "ScriptedData.txt";
    }
}