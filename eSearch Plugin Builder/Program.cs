// See https://aka.ms/new-console-template for more information

using Newtonsoft.Json.Linq;
using System.IO.Compression;

try
{

    Thread.Sleep(2000);


    string location = System.Reflection.Assembly.GetEntryAssembly().Location;
    string cfg = File.ReadAllText(Path.Combine(Path.GetDirectoryName(location), "PluginConfig.json"));

    Console.WriteLine("Config");
    Console.WriteLine(cfg);

    JObject config = JObject.Parse(cfg);

    //config.mainDLL.
    
    string main_dll = config["mainDLL"].ToString();
    string zip_name = config["zipName"].ToString();

    string version = args[0];

    File.WriteAllText("MainDLL", main_dll);

    string fileName = zip_name + " " + version + ".esplugin";
    if (File.Exists(fileName)) File.Delete(fileName);

    using (ZipArchive zip = ZipFile.Open(fileName, ZipArchiveMode.Create))
    {
        foreach (var file in Directory.GetFiles(System.IO.Directory.GetCurrentDirectory()))
        {
            if (!file.EndsWith(".esplugin"))
            {
                zip.CreateEntryFromFile(file, Path.GetFileName(file));
            }
        }
    }


    Environment.Exit(0);
} catch (Exception ex)
{
    Console.WriteLine("Error building plugin");
    Console.WriteLine(ex.ToString());
    Environment.Exit(1);
}
