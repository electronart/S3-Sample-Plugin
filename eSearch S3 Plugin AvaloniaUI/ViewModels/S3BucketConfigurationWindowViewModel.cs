using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace eSearch_S3_Plugin_AvaloniaUI.ViewModels
{
    public class S3BucketConfigurationWindowViewModel : ViewModelBase
    {
        public string BucketName {  
            get
            {
                return _bucketName;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _bucketName, value);
            }
        }

        private string _bucketName = string.Empty;

        public string Region
        {
            get
            {
                return _bucketRegion;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _bucketRegion, value);
            }
        }

        private string _bucketRegion = string.Empty;

        public bool UseAuthentication
        {
            get
            {
                return _useAuthentication;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _useAuthentication, value);
                if (_useAuthentication == false)
                {
                    AccessKey = string.Empty;
                    SecretAccessKey = string.Empty;
                }
            }
        }

        private bool _useAuthentication = false;

        public string AccessKey
        {
            get
            {
                return _accessKey;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _accessKey, value);
            }
        }

        private string _accessKey = string.Empty;

        public string SecretAccessKey
        {
            get
            {
                return _secretAccessKey;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _secretAccessKey, value);
            }
        }

        private string _secretAccessKey = string.Empty;

        public bool IsConnecting
        {
            get
            {
                return _isConnecting;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isConnecting, value);
            }
        }

        private bool _isConnecting = false;

        public string ValidationError
        {
            get
            {
                return _validationError;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _validationError, value);
            }
        }

        private string _validationError = " ";

        /// <summary>
        /// This isn't displayed on the UI. It's so we know which eSearch Index this is for.
        /// </summary>
        public required string IndexID;

    }
}
