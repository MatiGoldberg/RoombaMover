using System;
//using Microsoft.SPOT;
using System.Collections;

namespace RoombaMapper
{
    class FifoBuffer
    {
        private const uint MIN_ACCEPTABLE_MEMORY = 32768;
        private static Queue Buffer = new Queue();

        public static bool Push(string msg)
        {
            if (NoFreeMemory()) return false;

            lock (Buffer)
            {
                Buffer.Enqueue((object)msg);
            }
            return true;
        }


        public static string Pop()
        {
            if (Buffer.Count > 0)
            {
                return (string)Buffer.Dequeue();
            }
            return null;
        }


        public static int Count()
        {
            return Buffer.Count;
        }


        private static bool NoFreeMemory()
        {
            uint free_memory = Microsoft.SPOT.Debug.GC(false);
            if (free_memory < MIN_ACCEPTABLE_MEMORY)
                return true;

            return false;
        }

    }
}
