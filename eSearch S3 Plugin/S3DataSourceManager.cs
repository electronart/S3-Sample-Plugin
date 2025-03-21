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
        private TaskCompletionSource<bool>? taskCompletionSource;

        /// <summary>
        /// When this task completes, eSearch will again call GetConfiguredDataSources to refresh the list.
        /// </summary>
        /// <param name="indexID">The Index this datasource should belong to.</param>
        /// <param name="dataSource">Null if creating a new datasource.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">dataSource is not a S3BucketDataSource</exception>
        public async Task InvokeDataSourceConfigurator(string indexID, IDataSource? dataSource)
        {
            if (dataSource == null)
            {
                // Creating a new DataSource.
                dataSource = new S3BucketDataSource { 
                    AWSAccessKey = string.Empty, 
                    AWSSecretKey = string.Empty, 
                    BucketName   = string.Empty,
                    IndexID      = indexID
                };
            }
            if (dataSource is S3BucketDataSource bucketDS)
            {
                S3BucketConfigurationWindowViewModel viewModel = new S3BucketConfigurationWindowViewModel
                {
                    BucketName = bucketDS.BucketName,
                    UseAuthentication = bucketDS.AWSAccessKey != string.Empty,
                    AccessKey = Base64.Decode(bucketDS.AWSAccessKey),
                    SecretAccessKey = Base64.Decode(bucketDS.AWSSecretKey),
                    IndexID = indexID,
                };

                S3BucketConfigurationWindow window = new S3BucketConfigurationWindow();
                window.DataContext = viewModel;
                window.ClickedOK += Window_ClickedOK;
                window.Show();
                window.Closed += Window_Closed;
                taskCompletionSource = new TaskCompletionSource<bool>();
                await taskCompletionSource.Task;
            }
            else
            {
                throw new NotSupportedException("Unrecognized Data Source Type");
            }
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            taskCompletionSource?.SetResult(true);
        }

        /// <summary>
        /// The window doesn't close when clicking OK. Instead we test the form validity and then that it can
        /// connect successfully. Only afterwards will the window be closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_ClickedOK(object? sender, S3BucketConfigurationWindowViewModel e)
        {
            #region Basic validation
            if (string.IsNullOrWhiteSpace(e.BucketName))
            {
                e.ValidationError = "Bucket Name Required";
                return;
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
                    IndexID = e.IndexID
                };
                if (!testDS.TestS3Connection(out string errorMsg))
                {
                    e.ValidationError = errorMsg;
                    return;
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
