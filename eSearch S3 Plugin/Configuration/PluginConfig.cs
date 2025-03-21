using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch_S3_Plugin.Configuration
{
    public class PluginConfig
    {

        public List<S3BucketDataSource> S3BucketDataSources { get; set; } = new List<S3BucketDataSource>();

        public static PluginConfig LoadConfig()
        {
            throw new NotImplementedException();
        }

        public void SaveConfig()
        {
            throw new NotImplementedException();
        }

    }
}
