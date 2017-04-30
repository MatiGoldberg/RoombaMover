using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace ResetRoomba
{
    public class Program
    {
        private const int POLLING_PERIOD_MS = 1000;
        private const int STARTUP_DELAY = 2000;
        private static RoombaSerial roomba;
        private static InterruptPort PushButton;
        private static bool ButtonPressed = false;
        
        public static void Main()
        {
            Thread.Sleep(STARTUP_DELAY);

            // Init //
            roomba = new RoombaSerial(SerialPorts.COM1, Pins.GPIO_PIN_D4);
            PushButton = new InterruptPort(Pins.ONBOARD_BTN, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
            PushButton.OnInterrupt += new NativeEventHandler(OnPushButton);

            while (true)
            {
                Thread.Sleep(POLLING_PERIOD_MS);


                if (ButtonPressed)
                {
                    TRACE("Main", "Handling Button.");
                    ButtonPressed = false;
                    roomba.ResetSerialPort();

                    Thread.Sleep(POLLING_PERIOD_MS);
                    roomba.StartRommbaCommunication();

                    Thread.Sleep(POLLING_PERIOD_MS);
                    TRACE("Main", roomba.ReadSensors(RoombaSerial.SensorData.All));
                }
                else
                    TRACE("Main", "Tick.");
            }

        }

        private static void OnPushButton(uint data1, uint data2, DateTime time)
        {
            ButtonPressed = true;
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
