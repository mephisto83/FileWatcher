using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEPH.util.FileWatcher
{
    class Program
    {
        static FileWatcherManager fileWatcherManager;
        static void Main(string[] args)
        {
            fileWatcherManager = new FileWatcherManager(args);

            if (fileWatcherManager.IsReady)
            {
                string res = String.Empty;
                res = Console.ReadLine();
            }
        }
    }
}
