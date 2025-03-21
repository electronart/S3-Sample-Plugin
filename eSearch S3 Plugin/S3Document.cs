using Amazon.S3;
using Amazon.S3.Model;
using eSearch.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch_S3_Plugin
{
    public class S3Document : IDocument
    {
        #region Data / Constructor necessary to fetch all document info
        private string              _s3ObjectKey;
        private long                _s3ObjectSize;
        private AmazonS3Client      _s3Client;

        public S3Document(string key, long size, AmazonS3Client client)
        {
            this._s3ObjectKey = key;
            this._s3ObjectSize = size;
            this._s3Client = client;
        }
        #endregion

        public string? DisplayName => throw new NotImplementedException();

        public string Identifier => throw new NotImplementedException();

        public string? Text => throw new NotImplementedException();

        public string? FileName => throw new NotImplementedException();

        public string? Parser => throw new NotImplementedException();

        public long FileSize => throw new NotImplementedException();

        public DateTime? CreatedDate => throw new NotImplementedException();

        public DateTime? ModifiedDate => throw new NotImplementedException();

        public DateTime? IndexedDate => throw new NotImplementedException();

        public DateTime? AccessedDate => throw new NotImplementedException();

        public IEnumerable<IMetaData>? MetaData => throw new NotImplementedException();

        public IEnumerable<IDocument>? SubDocuments => throw new NotImplementedException();

        public int TotalKnownSubDocuments => throw new NotImplementedException();

        public IDocument.SkipReason ShouldSkipIndexing => throw new NotImplementedException();

        public bool IsVirtualDocument => throw new NotImplementedException();

        public string FileType => throw new NotImplementedException();

        public IEnumerable<string> ExtractedFiles => throw new NotImplementedException();

        public string HtmlRender => throw new NotImplementedException();
    }
}
