using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace eSearch_S3_Plugin.Utils
{
    public static class FilePaths
    {
        public static string S3_DOWNLOAD_PATH
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TempFiles", "S3Downloads");
            }
        }

    }
}
