using eSearch.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using eSearch_S3_Plugin_AvaloniaUI;
using eSearch_S3_Plugin_AvaloniaUI.ViewModels;
using eSearch_S3_Plugin.Utils;
using eSearch_S3_Plugin.Configuration;
using Avalonia.Controls;
using System.Reflection;
using System.Diagnostics;
using Avalonia;

namespace eSearch_S3_Plugin
{
    public class S3DataSourceManager : IPluginDataSourceManager
    {
        private PluginConfig? _pluginConfig = null;

        PluginConfig PluginConfig
        {
            get
            {
                if (_pluginConfig == null)
                {
                    _pluginConfig = PluginConfig.LoadConfig();
                }
                return _pluginConfig;
            }
        }


        public IEnumerable<IDataSource> GetConfiguredDataSources(string indexID)
        {
            return PluginConfig.S3BucketDataSources.Where(x => x.IndexID == indexID);
        }

        public string GetDataSourceName()
        {
            return "S3 Bucket";
        }

        #region Connection UI Handling

        eSearch_S3_Plugin_AvaloniaUI.LifeTime? windowLifeTime;

        /// <summary>
        /// When this task completes, eSearch will again call GetConfiguredDataSources to refresh the list.
        /// </summary>
        /// <param name="indexID">The Index this datasource should belong to.</param>
        /// <param name="dataSource">Null if creating a new datasource.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">dataSource is not a S3BucketDataSource</exception>
        public async Task InvokeDataSourceConfigurator(string indexID, IDataSource? dataSource)
        {
            #region Hard-coded setup
            //await Task.Run(() =>
            //{
            //    S3BucketDataSource newDataSource = new S3BucketDataSource
            //    {
            //        IndexID = indexID,
            //        BucketName = "s3-pugin-demo-bucket", //"electronart.co.uk",
            //        AWSAccessKey = string.Empty,
            //        AWSSecretKey = string.Empty,
            //        BucketRegionEndpoint = "eu-west-2"
            //    };
            //    PluginConfig.S3BucketDataSources.Add(newDataSource);
            //    PluginConfig.SaveConfig();
            //});
            #endregion

            #region Temporarily Commented out for Demo
            if (dataSource == null)
            {
                // Creating a new DataSource.
                dataSource = new S3BucketDataSource
                {
                    AWSAccessKey = string.Empty,
                    AWSSecretKey = string.Empty,
                    BucketName = "s3-plugin-demo-bucket",
                    IndexID = indexID,
                    BucketRegionEndpoint = "eu-west-2"
                };
            }
            if (dataSource is S3BucketDataSource bucketDS)
            {
                try
                {
                    S3BucketConfigurationWindowViewModel viewModel = new S3BucketConfigurationWindowViewModel
                    {
                        BucketName = bucketDS.BucketName,
                        UseAuthentication = bucketDS.AWSAccessKey != string.Empty,
                        AccessKey = Base64.Decode(bucketDS.AWSAccessKey),
                        SecretAccessKey = Base64.Decode(bucketDS.AWSSecretKey),
                        IndexID = indexID,
                        Region = bucketDS.BucketRegionEndpoint
                    };
                    #region Start a new Window Lifetime in the Plugin (Needed for Avalonia)
                    if (windowLifeTime != null) {
                        windowLifeTime.EndLifeTime();
                    }
                    windowLifeTime = eSearch_S3_Plugin_AvaloniaUI.LifeTime.NewLifeTime(); // Needed for Plugin Avalonia UI. The lifetime is ended when the window is closed.
                    #endregion
                    S3BucketConfigurationWindow window = new S3BucketConfigurationWindow();
                    window.DataContext = viewModel;
                    window.ClickedOK += Window_ClickedOK;
                    window.Show();
                    var tcs = new TaskCompletionSource<object>();
                    window.Closed += (sender, args) =>
                    {
                        windowLifeTime?.EndLifeTime();
                        tcs.SetResult(true);
                    };
                    await tcs.Task;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
            else
            {
                throw new NotSupportedException("Unrecognized Data Source Type");
            }
            #endregion
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            
        }

        /// <summary>
        /// The window doesn't close when clicking OK. Instead we test the form validity and then that it can
        /// connect successfully. Only afterwards will the window be closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Window_ClickedOK(object? sender, S3BucketConfigurationWindowViewModel e)
        {
            #region Basic validation
            if (string.IsNullOrWhiteSpace(e.BucketName))
            {
                e.ValidationError = "Bucket Name Required";
                return;
            }
            if (string.IsNullOrWhiteSpace(e.Region))
            {
                e.ValidationError = "Region Required";
            }
            if (e.UseAuthentication)
            {
                if (string.IsNullOrWhiteSpace(e.AccessKey))
                {
                    e.ValidationError = "Access Key Required";
                    return;
                }
                if (string.IsNullOrWhiteSpace(e.SecretAccessKey))
                {
                    e.ValidationError = "Secret Access Key Required";
                    return;
                }
            }
            #endregion
            #region Test the connection...
            e.IsConnecting = true;
            try
            {
                S3BucketDataSource testDS = new S3BucketDataSource
                {
                    AWSAccessKey = e.AccessKey,
                    AWSSecretKey = e.SecretAccessKey,
                    BucketName = e.BucketName,
                    IndexID = e.IndexID,
                    BucketRegionEndpoint = e.Region
                };

                var res = await testDS.TestS3Connection();
                if (res.Item1 == false)
                {
                    // Connection Failed.
                    e.ValidationError = res.Item2;
                } else
                {
                    // Connected Successfully. Save the new Configuration.
                    PluginConfig.S3BucketDataSources.Add(testDS);
                    PluginConfig.SaveConfig();
                    if (sender is Window window)
                    {
                        window.Close();
                    }
                }
            }
            finally
            {
                e.IsConnecting = false;
            }
            #endregion
            
        }

        public void RemoveDataSource(IDataSource dataSource)
        {
            if (dataSource is S3BucketDataSource s3Source)
            {
                PluginConfig.S3BucketDataSources.Remove(s3Source);
                PluginConfig.SaveConfig();
            }
        }

        public string GetDataSourceIconPath()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icons8-S3Icon.png");
        }
        #endregion


    }
}
