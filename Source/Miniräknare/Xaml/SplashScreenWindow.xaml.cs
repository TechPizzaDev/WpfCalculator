using System.Windows;

namespace Miniräknare
{
    public partial class SplashScreenWindow : Window
    {
        public double Progress
        {
            get => ProgressBar.Value;
            set => ProgressBar.Value = value;
        }

        public string ProgressTip
        {
            get => ProgressTipBlock.Text;
            set => ProgressTipBlock.Text = value;
        }

        public SplashScreenWindow()
        {
            InitializeComponent();
        }

        public void DispatchProgress(double value)
        {
            Dispatcher.Invoke(() => { Progress = value; });
        }

        public void DispatchProgressTip(string value)
        {
            Dispatcher.Invoke(() => { ProgressTip = value; });
        }
    }
}
