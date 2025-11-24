using Amazon.S3.Model;
using Amazon.S3;
using eSearch.Interop;
using eSearch.Interop.IDataSourceExtensions;
using eSearch_S3_Plugin.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eSearch_S3_Plugin.Utils;
using Amazon;
using Amazon.Runtime;


namespace eSearch_S3_Plugin
{
    public class S3BucketDataSource : IDataSource , IRequiresESearchFileParser
    {

        #region Override Equality Checks to allow eSearch to find this datasource for various operations such as edit/remove.
        public string Identifier = Guid.NewGuid().ToString();

        public override bool Equals(object? obj)
        {
            if (obj is S3BucketDataSource ds)
            {
                return ds.Identifier.Equals(Identifier);
            }
            return false;
        }

        public override int GetHashCode() { 
            return Identifier.GetHashCode();
        }
        #endregion


        #region Configuration Data
        /// <summary>
        /// The eSearch index this DataSource belongs to.
        /// </summary>
        public required string IndexID;

        /// <summary>
        /// The S3 Bucket that this DataSource indexes from.
        /// </summary>
        public required string BucketName;

        // For S3 Authentication.
        public required string AWSAccessKey;
        // For S3 Authentication.
        public required string AWSSecretKey;

        // For S3. Eg. eu-west-2
        public required string BucketRegionEndpoint;

        /// <summary>
        /// Method to test that the configuration is OK.
        /// </summary>
        /// <returns></returns>
        public async Task<Tuple<bool, string>> TestS3Connection()
        {
            try
            {
                var request = new ListObjectsV2Request
                {

                    BucketName = BucketName,
                    ContinuationToken = null
                };
                var response = await S3Client.ListObjectsV2Async(request);
                return new Tuple<bool, string> ( true, "" );
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>( false, ex.Message );
            }
        }

        IESearchFileParser? fileParser;

        #endregion

        #region State Tracking
        private ILogger? _logger = null;

        private Task?   _listS3ObjectsTask    = null;
        private bool    _listS3ObjectsFinished = false;
        
        // How many S3 Documents have been discovered in this bucket so far.
        private int     _discoveredDocumentCount      = 0;
        // How many bytes of S3 Documents have been discovered in this bucket so far.
        private long    _totalDiscoveredDocumentBytes = 0;
        // How many bytes of S3 Documents have been passed to eSearch via GetNextDoc
        private long    _totalIndexedDocumentBytes    = 0;

        private int     _currentDocumentIndex = 0;

        private string _currentFileName = string.Empty;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private List<Tuple<string, long>> _queuedS3Obects = new List<Tuple<string, long>>();
        #endregion

        AmazonS3Client S3Client { 
            get
            {
                if (_s3Client == null)
                {
                    if (string.IsNullOrWhiteSpace(AWSAccessKey))
                    {
                        _s3Client = new AmazonS3Client(new AnonymousAWSCredentials(), RegionEndpoint.GetBySystemName(BucketRegionEndpoint));
                    } else
                    {
                        _s3Client = new AmazonS3Client(AWSAccessKey, AWSSecretKey, RegionEndpoint.GetBySystemName(BucketRegionEndpoint));
                    }
                }
                return _s3Client;
            } 
        }

        private AmazonS3Client? _s3Client = null;

        private string _tempFile = string.Empty;
        


        public string Description()
        {
            string str = "S3 Bucket: " + BucketName;
            if (_currentFileName != string.Empty)
            {
                str += " - " + _currentFileName;
            }
            return str;
        }

        public void GetNextDoc(out IDocument document, out bool isDiscoveryComplete)
        {
            #region Cleanup Previous Temp file, if any.
            try
            {
                if (!string.IsNullOrWhiteSpace(_tempFile) && File.Exists(_tempFile))
                {
                    File.Delete(_tempFile);
                }
            }
            catch (Exception ex)
            {
                _logger?.Log(ILogger.Severity.ERROR, "Error deleting temp file", ex);
            }
            #endregion
            isDiscoveryComplete = _listS3ObjectsFinished;
            if (_listS3ObjectsTask == null)
            {
                _listS3ObjectsTask = ListS3ObjectsInBucket(_cancellationTokenSource.Token);
            }
            if (_queuedS3Obects.Count > 0)
            {
                // 1 or more items in the queue. Pop the first one out.
                var s3Object = _queuedS3Obects[0];
                _queuedS3Obects.RemoveAt(0);
                var key     = s3Object.Item1;
                var size    = s3Object.Item2;
                _currentFileName = key;
                try
                {
                    using var res = S3Client.GetObjectAsync(BucketName, key, _cancellationTokenSource.Token).Result;

                    string outputDir = FilePaths.S3_DOWNLOAD_PATH;
                    Directory.CreateDirectory(outputDir);
                    string tempFileName = Path.Combine(outputDir, Path.GetFileName(key));

                    var result = Task.Run(async () =>
                    {
                        await res.WriteResponseStreamToFileAsync(tempFileName, false, _cancellationTokenSource.Token);
                        return true;
                    }).Result;

                    _tempFile = tempFileName; // We'll delete this on GetNextFile. Deleting it syncrhonously here would cause parse issues.

                    if (fileParser is IESearchFileParser mFileParser)
                    {
                        document = mFileParser.ParseFile(tempFileName);
                        
                        return;
                    } else
                    {
                        document = null;
                        throw new Exception("IESearchFileParser has not been set.");
                    }
                } catch (AmazonS3Exception aex)
                {
                    document = null;
                    _logger?.Log(ILogger.Severity.ERROR, "Error retrieving file", aex);
                }
                return;
            }
            else
            {
                document = null;
                return;
            }
        }

        public double GetProgress()
        {
            try
            {
                int totalFilesIterated = _discoveredDocumentCount - _queuedS3Obects.Count;

                float val = (float.Parse(totalFilesIterated.ToString()) / float.Parse((_discoveredDocumentCount).ToString())) * 100.0f;
                int progress = (int)Math.Round(val);
                if (progress < 0) progress = 0;
                if (progress > 100) progress = 100;
                return progress;
            } catch (Exception ex)
            {
                return 0;
            }
        }

        public int GetTotalDiscoveredDocuments()
        {
            return _discoveredDocumentCount;
        }

        public void Rewind()
        {
            _listS3ObjectsTask = null;
            _listS3ObjectsFinished = false;
            _discoveredDocumentCount = 0;
            _totalDiscoveredDocumentBytes = 0;
            _totalIndexedDocumentBytes = 0;
            _currentDocumentIndex = 0;
            _cancellationTokenSource = new CancellationTokenSource();
            _queuedS3Obects = new List<Tuple<string, long>>();
            _currentFileName = string.Empty;
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return Description();
        }

        public void UseIndexTaskLog(ILogger logger)
        {
            _logger = logger;
        }

        public void SetESearchFileParser(IESearchFileParser eSearchFileParser)
        {
            fileParser = eSearchFileParser;
        }

        private async Task ListS3ObjectsInBucket(CancellationToken cancellationToken)
        {
            string? continuationToken = null;
            try
            {
                do
                {
                    var request = new ListObjectsV2Request
                    {
                        
                        BucketName = BucketName,
                        ContinuationToken = continuationToken
                    };

                    var response = await S3Client.ListObjectsV2Async(request);

                    foreach (S3Object obj in response.S3Objects)
                    {
                        _totalDiscoveredDocumentBytes += obj.Size;
                        _discoveredDocumentCount++;

                        Console.WriteLine($"Key: {obj.Key}, Size: {obj.Size}, LastModified: {obj.LastModified}");
                        // Process each file here
                        _queuedS3Obects.Add(new Tuple<string, long>(obj.Key, obj.Size));
                    }

                    continuationToken = response.NextContinuationToken;
                } while (continuationToken != null);
            } catch (Exception ex)
            {
                _logger?.Log(ILogger.Severity.ERROR, "Error listing objects in bucket", ex);
            } finally
            {
                _listS3ObjectsFinished = true;
            }
        }

        
    }
}
