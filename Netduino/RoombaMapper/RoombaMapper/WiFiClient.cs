using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
//using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

using System.Text;

namespace RoombaMapper
{
    class WiFiClient
    {
        #region Variables and Constants
        private const int POLLING_PERIOD_MS = 3000;
        private const int RX_BUFFER_LENGTH = 256;

        // TO BE REPLACED WITH LED CLASS/THREAD //
        private OutputPort  Led = new OutputPort(Pins.ONBOARD_LED, false);
        // ------------------------------ //

        private IPEndPoint HostAddress;
        public bool IsConnected = false;
        #endregion

        #region Constructor
        public WiFiClient(string HostIp, int HostPort)
        {
            HostAddress = new IPEndPoint(IPAddress.Parse(HostIp), HostPort);
            TRACE("WiFiClient", "Class created {" + HostIp + ":" + HostPort.ToString() + "}.");
        }

        public void Run()
        {
            // Wait for Network Connectivity
            while (IPAddress.GetDefaultLocalAddress() == IPAddress.Any) ;
            TRACE("Run", "Network connectivity obtained {" + IPAddress.GetDefaultLocalAddress().ToString() + "}");
            IsConnected = true;

            while (true)
            {
                SendData();
                Thread.Sleep(POLLING_PERIOD_MS);
            }
        }
        #endregion

        #region Internal Functions
        private void SendData()
        {
            if (FifoBuffer.Count() == 0)
            {
                TRACE("SendData", "Buffer empty, skipping update.");
                return;
            }

            TRACE("SendData", "Attempting connection... {" + HostAddress.ToString() + "}");
            Led.Write(true);

            try
            {
                Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sender.Connect(HostAddress);
                TRACE("SendData", "Connected.");

                while (FifoBuffer.Count() > 0)
                {
                    byte[] outgoing_msg = Encoding.UTF8.GetBytes(FifoBuffer.Pop());
                    byte[] incoming_msg = new byte[RX_BUFFER_LENGTH];

                    int txbytes = sender.Send(outgoing_msg);
                    int rxbytes = sender.Receive(incoming_msg);

                    // ADD: OK/ERROR HANDLING IF NEEDED...
                    
                    TRACE("SendData", "Received: {" + new string(Encoding.UTF8.GetChars(incoming_msg)) +"}");
                    //TRACE("SendData", "Bytes sent: {" + txbytes.ToString() + "}. Bytes received: {" + rxbytes.ToString() + "}.");
                }
                
                sender.Close();
                TRACE("SendData", "Connection terminated.");
            }
            catch (SocketException s)
            {
                TRACE("SendData", "SocketException: could not connect to server {" + s.ToString() + "}.");
            }
            catch (Exception e)
            {
                TRACE("SendData", "Exception: " + e.ToString());
            }
            Led.Write(false);
        }
        #endregion

        #region Auxilliary
        private void TRACE(string func, string msg)
        {
            #if DEBUG
            TimeSpan ts = Microsoft.SPOT.Hardware.Utility.GetMachineTime();
            Debug.Print("[" + ts.ToString() + "] [WiFiClient." + func + "()] " + msg);
            #endif
        }
        #endregion
    }
}
