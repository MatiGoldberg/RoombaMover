using System;
using Microsoft.SPOT;
using System.IO;

namespace RoombaMapper
{
    class Logger
    {
        #region [--Variables-----------------------------]
        private const string Path = @"SD\";
        private string LogFile = "Log.txt";
        private bool Valid = true;
        #endregion

        #region [--Constructor------------------------]
        public Logger(string filename = "Log.txt")
        {
            LogFile = filename;
            if (isSDValid())
                TRACE("Logger", "Log file {" + Path + LogFile + "}.");
            else Valid = false;
            
        }
        #endregion

        #region [--Public Methods-------------------]
        public bool Log(string message, bool timestamp = false)
        {
            if (!Valid) return false;

            try
            {
                using (StreamWriter file = new StreamWriter(Path + LogFile, true))
                {
                    if (timestamp)
                    {
                        TimeSpan ts = Microsoft.SPOT.Hardware.Utility.GetMachineTime();
                        file.WriteLine("[" + ts.ToString() + "] " + message);
                    }
                    else
                    {
                        file.WriteLine(message);
                    }
                    file.Flush();
                }
                TRACE("Log", "Line logged.");
                return true;
            }
            catch (Exception e)
            {
                TRACE("Log", "Exception: Could not log line. {" + e.ToString() + "}");
            }
            return false;

        }
        #endregion

        #region [--Private Methods----------------]
        private void TRACE(string func, string msg)
        {
            #if DEBUG
            TimeSpan ts = Microsoft.SPOT.Hardware.Utility.GetMachineTime();
            Debug.Print("[" + ts.ToString() + "] [Logger." + func + "()] " + msg);
            #endif
        }

        private bool isSDValid()
        {
            bool valid;

            try
            {
                valid = System.IO.Directory.Exists(Path);
                TRACE("isSDValid", "Directory found {" + valid.ToString() + "}");
            }
            catch (Exception e)
            {
                TRACE("isSDValid", "Exception caught {" + e.ToString() + "}");
                return false;
            }

            return valid;
        }
        #endregion
    }
}
