using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MEPH.util.FileWatcher.Data
{
    public class StateOfFile
    {
        public long Ticks { get; set; }

        public string FullName { get; set; }

        public long LastUpdated { get; set; }

        public string Name { get; set; }
    }
}
