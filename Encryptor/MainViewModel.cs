using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Encryptor
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _downloadProgram;

        public bool DownloadProgram 
        {
            get => _downloadProgram;
            set
            {
                if(_downloadProgram != value)
                {
                    _downloadProgram = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DownloadProgram)));
                }
            }
        }

        private bool _downloadParameter;

        public bool DownloadParameter 
        {
            get => _downloadParameter;
            set
            {
                if (_downloadParameter != value)
                {
                    _downloadParameter = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DownloadParameter)));
                }
            }
        }

        private bool _encryption;
        public bool Encryption 
        {
            get => _encryption;
            set
            {
                if(_encryption != value)
                {
                    _encryption = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Encryption)));
                }
            }
        }

        private string? _programFile;

        public string? ProgramFile 
        {
            get => _programFile; 
            set
            {
                _programFile = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProgramFile)));
            }
        }

        private string? _parameterFile;
        public string? ParameterFile 
        {
            get => _parameterFile; 
            set
            {
                _parameterFile = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParameterFile)));
            }
        }
    }
}
