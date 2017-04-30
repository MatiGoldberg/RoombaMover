using System;
using Microsoft.SPOT;
using System.IO;

namespace DataInjector
{
    class Logger
    {
        #region [--Variables-----------------------------]
        private const string Path = @"SD\";
        private string LogFile = "Log.txt";
        private bool Valid = true;
        private bool ReadBack = false;
        private StreamReader file = null;
        private bool ReadLock = false;
        #endregion

        #region [--Constructor------------------------]
        public Logger(string filename = "Log.txt", bool readback = false)
        {
            LogFile = filename;

            if (!isSDValid())
            {
                Valid = false;
                return;
            }

            if (readback)
            {
                ReadBack = readback;
                if (MountFile())
                    TRACE("Logger", "Mounted file {" + Path + LogFile + "}.");
                else
                    TRACE("Logger", "Unable to mount file {" + Path + LogFile + "}.");
            }
            else
            {
                TRACE("Logger", "Log file {" + Path + LogFile + "}.");
            }
            
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
        
        public string ReadLine()
        {
            if (!ReadBack) return null;

            if (ReadLock)
            {
                TRACE("ReadLine", "Read Locked.");
                return null;
            }
            ReadLock = true;
            
            try
            {
                string line = file.ReadLine();
                ReadLock = false;
                return line;
            }
            catch (Exception e)
            {
                TRACE("ReadLine", "Exception: cannot read file {" + e.ToString() + "}");
                ReadLock = false;
                return null;
            }
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

        private bool MountFile()
        {
            try
            {
                file = new StreamReader(Path + LogFile);
            }
            catch (Exception e)
            {
                TRACE("MountFile", "Exception: " + e.ToString());
                return false;
            }
            return true;

        }
        #endregion
    }
}
