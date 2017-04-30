using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace RoombaDebug
{
    /// <summary>
    /// This is an N3 application for debugging the ROOMBA SCI connection.
    /// It has 2 UART connections:
    /// 1. Pins (0,1,4) are connected to ROOMBA (including DD).
    /// 2. Pins (2,3) are connected to PC Terminal.
    /// </summary>
    public class Program
    {
        private static RoombaSerial roomba;
        private static PCSerial terminal;
        private static Thread uart1;
        private const int POLLING_PERIOD_MS = 500;
        private static int msg_count;
        
        public static void Main()
        {
            TRACE("Main", "--- Starting RoombaDebug ---");
            Init();

            string sensors;
            while (true)
            {
                sensors = roomba.ReadSensors();
                terminal.Write(sensors);
                msg_count++;
                Thread.Sleep(POLLING_PERIOD_MS);
            }

        }

        private static void Init()
        {
            TRACE("Init", "Initializing interfaces");
            roomba = new RoombaSerial(SerialPorts.COM1, Pins.GPIO_PIN_D4);
            terminal = new PCSerial(SerialPorts.COM3);
            uart1 = new Thread(terminal.Run);
            uart1.Start();

            msg_count = 1;
        }

        private static void EchoServer()
        {
            if (terminal.QueueEmpty()) return;

            string msg = terminal.Pop();

            terminal.Write(msg);
        }
        
        
        private static void TRACE(string func, string msg)
        {
#if DEBUG
            TimeSpan ts = Microsoft.SPOT.Hardware.Utility.GetMachineTime();
            Debug.Print("[" + ts.ToString() + "] [Program." + func + "()] " + msg);
#endif
        }

    }
}
