using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace eSearch_S3_Plugin.Configuration
{
    public class PluginConfig
    {

        public List<S3BucketDataSource> S3BucketDataSources { get; set; } = new List<S3BucketDataSource>();

        private static string GetConfigFilePath()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "S3Configuration.json");
        }

        public static PluginConfig LoadConfig()
        {
            string cfgFilePath = GetConfigFilePath();
            if (!File.Exists(cfgFilePath))
            {
                PluginConfig config = new PluginConfig();
                config.SaveConfig();
                return config;
            }
            PluginConfig? cfg = JsonConvert.DeserializeObject<PluginConfig>(File.ReadAllText(cfgFilePath));
            if (cfg != null)
            {
                return cfg;
            }
            throw new Exception("Error reading Plugin Config file - Missing or Invalid? \n" + cfgFilePath);
        }

        public void SaveConfig()
        {
            string strConfig = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(GetConfigFilePath(), strConfig);
        }
    }
}
