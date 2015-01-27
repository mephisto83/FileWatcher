using MEPH.util.FileWatcher.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MEPH.util.FileWatcher
{
    public class StateWriter
    {
        bool readerFlag = false;  // State flag
        IList<StateOfFile> Backlog = new List<StateOfFile>();

        public void UpdateState(StateOfFile fileState)
        {
            lock (this)  // Enter synchronization block
            {
                if (readerFlag)
                {      // Wait until Cell.ReadFromCell is done consuming.
                    try
                    {
                        Monitor.Wait(this);   // Wait for the Monitor.Pulse in
                        // ReadFromCell
                    }
                    catch (SynchronizationLockException e)
                    {
                        Console.WriteLine(e);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Console.WriteLine(e);
                    }
                }
                Backlog.Add(fileState);
                Console.WriteLine("Updating State");
                readerFlag = true;    // Reset the state flag to say producing
                // is done
                Monitor.Pulse(this);  // Pulse tells Cell.ReadFromCell that 
                // Cell.WriteToCell is done.
            }   // Exit synchronization block
        }

        public IList<StateOfFile> ReadState()
        {
            lock (this)   // Enter synchronization block
            {
                if (!readerFlag)
                {            // Wait until Cell.WriteToCell is done producing
                    try
                    {
                        // Waits for the Monitor.Pulse in WriteToCell
                        Monitor.Wait(this);
                    }
                    catch (SynchronizationLockException e)
                    {
                        Console.WriteLine(e);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Console.WriteLine(e);
                    }
                }
                var res = Backlog.ToList();
                Console.WriteLine("Reading State");
                readerFlag = false;    // Reset the state flag to say consuming
                // is done.

                Monitor.Pulse(this);   // Pulse tells Cell.WriteToCell that
                return res;
                // Cell.ReadFromCell is done.
            }   // Exit synchronization block
        }
    }
}
