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


namespace eSearch_S3_Plugin
{
    public class S3BucketDataSource : IDataSource , IRequiresESearchFileParser
    {
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

        /// <summary>
        /// Method to test that the configuration is OK.
        /// </summary>
        /// <returns></returns>
        public bool TestS3Connection(out string errorMsg)
        {
            try
            {
                // TODO
                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                return false;
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
                        _s3Client = new AmazonS3Client();
                    } else
                    {
                        _s3Client = new AmazonS3Client(AWSAccessKey, AWSSecretKey);
                    }
                }
                return _s3Client;
            } 
        }

        private AmazonS3Client? _s3Client = null;




        public string Description()
        {
            return "S3 Bucket: " + BucketName;
        }

        public void GetNextDoc(out IDocument document, out bool isDiscoveryComplete)
        {
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
                try
                {
                    using var res = S3Client.GetObjectAsync(BucketName, key, _cancellationTokenSource.Token).Result;

                    string outputDir = FilePaths.S3_DOWNLOAD_PATH;
                    Directory.CreateDirectory(outputDir);
                    string tempFileName = Path.Combine(outputDir, Path.GetFileName(key));

                    res.WriteResponseStreamToFileAsync(tempFileName, false, _cancellationTokenSource.Token).RunSynchronously();
                    if (fileParser is IESearchFileParser mFileParser)
                    {
                        document = mFileParser.ParseFile(tempFileName);
                        try
                        {
                            File.Delete(tempFileName);
                        } catch (Exception ex)
                        {
                            _logger?.Log(ILogger.Severity.ERROR, "Error deleting temp file", ex);
                        }
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
            throw new NotImplementedException();
        }

        public int GetTotalDiscoveredDocuments()
        {
            return _discoveredDocumentCount;
        }

        public void Rewind()
        {
            throw new NotImplementedException();
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            throw new NotImplementedException();
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
        }

        
    }
}
