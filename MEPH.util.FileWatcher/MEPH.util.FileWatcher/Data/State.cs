using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEPH.util.FileWatcher.Data
{
    public class State
    {
        public State()
        {
            States = new List<StateOfFile>();
        }
        public IList<StateOfFile> States { get; set; }
    }
}
