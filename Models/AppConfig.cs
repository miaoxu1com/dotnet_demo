using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace XmindToExcelConverter.Models
{
    public class FieldConfig : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _label = string.Empty;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Label
        {
            get => _label;
            set
            {
                if (_label != value)
                {
                    _label = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AppConfig
    {
        public List<FieldConfig> Fields { get; set; } = new();
        public List<string> Templates { get; set; } = new();
    }
}