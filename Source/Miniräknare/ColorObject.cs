using System.ComponentModel;
using System.Windows.Media;

namespace Miniräknare
{
    public class ColorObject : INotifyPropertyChanged
    {
        private Color _color;

        public event PropertyChangedEventHandler PropertyChanged;

        public Color Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));

                    Brush.Color = value;
                }
            }
        }

        public SolidColorBrush Brush { get; }

        public ColorObject(Color color)
        {
            _color = color;
            Brush = new SolidColorBrush(_color);
        }

        public ColorObject() : this(Colors.White)
        {
        }
    }
}
