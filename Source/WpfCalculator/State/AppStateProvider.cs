using System.ComponentModel;

namespace WpfCalculator
{
    public class AppStateProvider : INotifyPropertyChanged
    {
        private AppState _state;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AppState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
                }
            }
        }
    }
}
