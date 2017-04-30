using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System.IO.Ports;

namespace RoombaDebug
{
    class RoombaSerial
    {
        #region [---Variables and Constructors------------------]
        public enum SensorData { All, Obstacles, Movement, Vitals }
        private static SerialPort serialPort;
        private static OutputPort device_detect = null;
        private static int CurrentBufferPosition { get; set; }
        private static byte[] Buffer { get; set; }
        private static int ReadCounter = 0;
        private static bool blocked = false;
        

        public RoombaSerial(string portName, Cpu.Pin pin)
        {
            // Serial Port Configuration
            serialPort = new SerialPort(portName, RoombaDefines.BAUDRATE, Parity.None, 8, StopBits.One);
            serialPort.ReadTimeout = 1000; // Set to 10ms.
            serialPort.DataReceived += new SerialDataReceivedEventHandler(RoombaSerialPort_DataReceived);

            // Device Detect Congifuration
            device_detect = new OutputPort(pin, true);

            EraseBuffer();
            Open();

            StartRommbaCommunication();
            ClearRoombaBuffer();

        }
        #endregion


        #region [---Serial Functions----------------------------]
        private static void Write(byte[] bytesToSend)
        {
            serialPort.Write(bytesToSend, 0, bytesToSend.Length);
            TRACE("Write", "TX>> [" + BytesToString(bytesToSend) + "]");
        }

 
        private static void SendByte(byte byteToSend)
        {
            byte[] buff = new byte[1];
            buff[0] = byteToSend;
            Write(buff);
            Thread.Sleep(RoombaDefines.BRIEFLY);
            TRACE("SendByte", "TX>> [" + ((int)byteToSend).ToString() + "]");
        }


        private static byte[] Read()
        {
            if (IsBufferEmpty())
                return null;
            
            // POSSIBLY ADD MSG RECEIVED FLAG AND ADD TIMER //

            var bytesReceived = new byte[CurrentBufferPosition];
            for (int i = 0; i < CurrentBufferPosition; i++)
            {
                bytesReceived[i] = Buffer[i];
            }

            EraseBuffer();

            TRACE("Read", "RX>> [" + BytesToString(bytesReceived) + "]");
            return bytesReceived;
        }


        private static byte[] WriteAndRead(byte[] bytesToSend)
        {
            Write(bytesToSend);
            Thread.Sleep(RoombaDefines.WRITE_READ_DELAY);
            var answer = Read();
            ReadCounter++;
            return answer;
        }
        #endregion


        #region [---Roomba Internal Functions-------------------]
        private static void StartRommbaCommunication()
        {
            TRACE("WakeUp","Waking Roomba up.");
            
            // Physical
            device_detect.Write(false);
            Thread.Sleep(RoombaDefines.HALF_A_SECOND);
            device_detect.Write(true);
            Thread.Sleep(RoombaDefines.ONE_SECOND);
            
            // Logical
            SendByte(COMMANDS.ENTER_START_MODE);
            
            TRACE("WakeUp", "done.");
        }

        private static void ClearRoombaBuffer()
        {
            for (int i = 0; i < RoombaDefines.RETRIES; i++)
            {
                WriteAndRead(new byte[2] { COMMANDS.READ_SENSORS, (byte)SensorData.All });
            }
            ReadCounter = 0;
            TRACE("ClearRoombaBuffer", "Roomba buffer cleared.");
        }
        #endregion


        #region [---Roomba Command Functions--------------------]
        private enum Move { STRAIGHT, TURN_RIGHT, TURN_LEFT, STOP }
        public void DriveAround()
        {
            TRACE("DriveAround", "Starting...");
            SendByte(COMMANDS.ENTER_CONTROL_MODE);

            for (int i = 0; i < 4; i++)
            {
                SendDriveCommand(Move.STRAIGHT);
                Thread.Sleep(RoombaDefines.TWO_SECONDS);
                TRACE("DriveAround", "Turn.");
                SendDriveCommand(Move.TURN_LEFT);
                Thread.Sleep(RoombaDefines.HALF_A_SECOND);
            }

            SendDriveCommand(Move.STOP);

            SendByte(COMMANDS.ENTER_START_MODE);
            TRACE("DriveAround", "Done.");
        }

        private void SendDriveCommand(Move Command)
        {
            byte[] Bytes;
            switch (Command)
            {
                case Move.STOP:
                    Bytes = new byte[5] { COMMANDS.DRIVE_COMMAND, (byte)'\x00', (byte)'\x00', (byte)'\x00', (byte)'\x00' };
                    break;

                case Move.STRAIGHT:
                    Bytes = new byte[5] { COMMANDS.DRIVE_COMMAND, (byte)'\x00', (byte)'\x00', (byte)'\x80', (byte)'\x00' };
                    break;

                case Move.TURN_LEFT:
                    Bytes = new byte[5] { COMMANDS.DRIVE_COMMAND, (byte)'\x00', (byte)'\x00', (byte)'\x00', (byte)'\x01' };
                    break;

                case Move.TURN_RIGHT:
                    Bytes = new byte[5] { COMMANDS.DRIVE_COMMAND, (byte)'\x00', (byte)'\x00', (byte)'\x00', (byte)'\xFF' };
                    break;

                default:
                    throw new NotImplementedException();
            }

            Write(Bytes);
            Thread.Sleep(RoombaDefines.BRIEFLY);
        }

        // NOT CHECKED YET //
        private void SendCustomDriveCommand(int Velocity, int Radius)
        {
            if (System.Math.Abs(Velocity) > RoombaDefines.MAX_VELOCITY)
            {
                TRACE("SendCustomDriveCommand", "Argument out of range (Velocity)");
                return;
            }

            if (System.Math.Abs(Radius) > RoombaDefines.MAX_RADUIS)
            {
                TRACE("SendCustomDriveCommand", "Argument out of range (Radius)");
                return;
            }

            byte[] bytes = new byte[5] { COMMANDS.DRIVE_COMMAND, HighByte(Velocity), LowByte(Velocity), HighByte(Radius), LowByte(Radius) };
            Write(bytes);
            Thread.Sleep(RoombaDefines.BRIEFLY);
        }
        #endregion


        #region [---Roomba Exported Functions-------------------]
        public void Open()
        {
            if (!serialPort.IsOpen)
            {
                serialPort.Open();
            }
        }


        public void Close()
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }


        public string ReadSensors(SensorData type = SensorData.All)
        {
            if (blocked)
            {
                TRACE("ReadSensors", "Communication Blocked.");
                return null;
            }
            blocked = true;

            var sensorbytes = new byte[RoombaDefines.BUFFER_LENGTH];

            sensorbytes = WriteAndRead(new byte[2] { COMMANDS.READ_SENSORS, (byte)type });

            TRACE("ReadSensors", "ReadCounter = {" + ReadCounter.ToString() + "}");

            if (sensorbytes == null)
            {
                blocked = false;
                return null;
            }
            
            blocked = false;
            return BytesToString(WrapSensorData(sensorbytes, (byte)type));
        }
        #endregion


        #region [---Auxiliary Functions-------------------------]
        private static byte[] WrapSensorData(byte[] data, byte type)
        {
            byte[] bytes = new byte[data.Length + 4];
            // 2 bytes for Counter
            // 1 byte for type
            // 1 byte for Checksum

            bytes[0] = (byte)(ReadCounter >> 8);
            bytes[1] = (byte)(ReadCounter);
            
            data.CopyTo(bytes, 2);

            bytes[data.Length + 2] = type;
            bytes[data.Length + 3] = 0;
            bytes[data.Length + 3] = CheckSum(bytes);
            

            return bytes;
        }


        private static byte CheckSum(byte[] data)
        {
            byte checksum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                checksum += data[i];
            }
            return checksum;
        }
        

        private static string BytesToString(byte[] bytes)
        {
            string txt = "";
            if (null == bytes) return "";

            foreach (byte b in bytes)
            {
                int i = (int)b;
                txt = txt + i.ToString() + RoombaDefines.DELIMITER;
            }

            return txt.Substring(0, txt.Length - 1);
        }


        private byte HighByte(int A)
        {
            if (A < 0)
                A = 0x10000 - A;

            return (byte)((A & 0xFF00) >> 8);
        }


        private byte LowByte(int A)
        {
            if (A < 0)
                A = 0x10000 - A;

            return (byte)(A & 0xFF);
        }


        private static void TRACE(string func, string msg)
        {
            #if DEBUG
            TimeSpan ts = Microsoft.SPOT.Hardware.Utility.GetMachineTime();
            Debug.Print("[" + ts.ToString() + "] [RoombaSerial." + func + "()] " + msg);
            #endif
        }
        #endregion


        #region [---Buffer Functions----------------------------]
        private static bool IsBufferEmpty()
        {
            return (CurrentBufferPosition == 0);
        }
        

        private static void EraseBuffer()
        {
            CurrentBufferPosition = 0;
            Buffer = new byte[RoombaDefines.BUFFER_LENGTH];
        }

        
        private void RoombaSerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (Buffer)
            {
                int incomingBytes = serialPort.BytesToRead;

                // PROTECTION: empty serial buffer
                if (incomingBytes == 0) return;

                // PROTECTION: buffer overflow
                if (incomingBytes >= RoombaDefines.BUFFER_LENGTH)
                {
                    serialPort.Flush();
                    Debug.Print("RoombaSerial buffer flushed.");
                    return;
                }

                // Actual reading
                int incoming_bytes = serialPort.Read(Buffer, CurrentBufferPosition, incomingBytes);

                // PROTECTION: junk data
                if ((incoming_bytes == 1) && (Buffer[CurrentBufferPosition] == 0))  return; 

                // Saving data
                CurrentBufferPosition += incoming_bytes;
            }
        }
        #endregion
    }

    class RoombaDefines
    {
        public const int BAUDRATE = 115200;
        public const int BUFFER_LENGTH = 512;
        public const char DELIMITER = '|';
        public const int HALF_A_SECOND = 500;
        public const int ONE_SECOND = 1000;
        public const int TWO_SECONDS = 2000;
        public const int WRITE_READ_DELAY = 100;
        public const int BRIEFLY = 50;
        public const int RETRIES = 3;

        public const int MAX_VELOCITY = 500;    // mm/s
        public const int MAX_RADUIS = 2000;     // mm
    }

    class COMMANDS
    {
        public const byte ENTER_START_MODE = (byte)('\x80');
        public const byte ENTER_CONTROL_MODE = (byte)('\x82');
        //public const byte ENTER_SAFE_MODE = (byte)('\x83');
        //public const byte ENTER_FULL_MODE = (byte)('\x84');
        
        public const byte READ_SENSORS = (byte)('\x8e');
        public const byte DRIVE_COMMAND = (byte)('\x89');

        // USE IN SAFE/FULL MODE
        //public const byte TURN_ROOMBA_OFF = (byte)('\x85');
        //public const byte CLEAN = (byte)('\x86');

    }
}
