using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MEPH.util.FileWatcher
{
    class FileOperation
    {
        public string Path { get; set; }

        public string Name { get; set; }

        public System.IO.WatcherChangeTypes ChangeType { get; set; }

        public Data.StateOfFile FileState { get; set; }

        public Data.SubConfiguration Config { get; set; }

        public string FullPath { get; set; }

        public string OldFullPath { get; set; }
    }
}
