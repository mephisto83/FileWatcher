using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEPH.util.FileWatcher
{
    public class CommandReader
    {
        public dynamic LoadJson(string file)
        {
            try
            {
                using (StreamReader r = new StreamReader(file))
                {
                    string json = r.ReadToEnd();
                    return JsonConvert.DeserializeObject(json);
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }


    }
}
