using eSearch.Interop;

namespace eSearch_S3_Plugin
{
    public class PluginInformation : IPluginManifestESearch
    {
        public string GetPluginAuthor()
        {
            return "ElectronArt Design Ltd";
        }

        public string GetPluginDescription()
        {
            return "Adds the ability to index S3 Buckets";
        }

        public string GetPluginName()
        {
            return "S3 Indexer";
        }

        public void RequiresESearchVersion(out int major, out int minor)
        {
            major = 1;
            minor = 1;
        }
    }
}
