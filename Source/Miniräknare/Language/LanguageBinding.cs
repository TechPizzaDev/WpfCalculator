using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xaml;

namespace Miniräknare
{
    public class LanguageBinding : MarkupExtension
    {
        private PropertyPath _keyProperty;
        private object _value;

        private DependencyObject _targetObject;
        private DependencyProperty _targetProperty;

        public PropertyPath Key
        {
            get => _keyProperty;
            set
            {
                _keyProperty = value;
                Uri = new ResourceUri(_keyProperty.Path);
            }
        }

        public ResourceUri Uri { get; private set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                return null;

            var targetProvider = serviceProvider.GetService<IProvideValueTarget>();
            _targetObject = targetProvider.TargetObject as DependencyObject;
            _targetProperty = targetProvider.TargetProperty as DependencyProperty;

            var rootProvider = serviceProvider.GetService<IRootObjectProvider>();
            if (!(rootProvider.RootObject is FrameworkElement root))
                return _value = "[No Resource Provider]";

            if (!(root.TryFindResource(App.LanguageProviderKey) is AppLanguageProvider langProvider))
                return _value = "[No Language Provider]";

            langProvider.PropertyChanged += LanguageData_PropertyChange;
            Refresh(langProvider);

            // TODO: add options for turning this on/off
            if (DesignerProperties.GetIsInDesignMode(_targetObject))
            {
                var block = new TextBlock();

                Run AddRun(string text, Color color)
                {
                    var run = new Run(text)
                    {
                        Foreground = new SolidColorBrush(color)
                    };
                    block.Inlines.Add(run);
                    return run;
                }

                if (Uri.Segments.Length == 1)
                {
                    AddRun(Uri.Segments[0], Colors.Black);
                }
                else
                {
                    AddRun(Uri.Segments[^2] + ResourceUri.PathSeparator, Colors.Gray).FontSize *= 0.75;
                    AddRun(Uri.Segments[^1], Colors.Black);
                }

                _value = block;
                return _value;
            }

            return _value;
        }

        private void LanguageData_PropertyChange(object sender, EventArgs e)
        {
            var langProvider = (AppLanguageProvider)sender;
            Refresh(langProvider);

            if (_targetObject != null)
                _targetObject.SetValue(_targetProperty, _value);
        }

        private void Refresh(AppLanguageProvider languageProvider)
        {
            try
            {
                _value = languageProvider.GetValue(Uri);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _value = "[Invalid Key]";
            }
        }
    }
}