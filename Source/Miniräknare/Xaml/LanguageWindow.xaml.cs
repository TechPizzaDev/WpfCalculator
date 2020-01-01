using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Miniräknare
{
    public partial class LanguageWindow : Window
    {
        private List<AppLanguageEntry> _languageEntryList;
        private BindingList<AppLanguageEntry> _languageEntryViewList;
        private AppLanguageProvider _languageProvider;

        public AppLanguage InitialLanguage { get; private set; }
        public bool? IsApplied { get; private set; }

        public LanguageWindow()
        {
            InitializeComponent();

            _languageProvider = FindResource(App.LanguageProviderKey) as AppLanguageProvider;
            InitialLanguage = _languageProvider?.Language;

            _languageEntryList = new List<AppLanguageEntry>();
            _languageEntryViewList = new BindingList<AppLanguageEntry>(_languageEntryList);
            LanguageList.ItemsSource = _languageEntryViewList;
        }

        public Task RefreshLanguages()
        {
            return Task.Run(() =>
            {
                var languages = AppLanguage.GetEmbeddedLanguages(Application.ResourceAssembly);
                var languageList = languages.ToList();

                //int index = 0;
                //Color itemColor = index++ % 2 == 0
                //    ? Colors.Transparent
                //    : Color.FromArgb(25, 127, 127, 127);

                foreach (var pair in languageList)
                {
                    var entry = new AppLanguageEntry(pair.Key, pair.Value);
                    entry.GetLanguage(); // preload here in task
                    _languageEntryList.Add(entry);
                }

                _languageEntryList.Sort((x, y) => x.Key.CompareTo(y.Key));
                Dispatcher.Invoke(_languageEntryViewList.ResetBindings);
            });
        }

        #region LanguageList handlers

        private void LanguageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var list = sender as ListView;
            if (list.SelectedItem is AppLanguageEntry languageEntry)
                _languageProvider.Language = languageEntry.Language;
        }

        private void LanguageList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var list = sender as ListView;
            list.UnselectAll();
        }

        private void LanguageList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var list = sender as ListView;
            if (list.SelectedItem is AppLanguageEntry)
            {
                IsApplied = true;
                Close();
            }
        }

        private void LanguageList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var list = sender as ListView;
            var grid = list.View as GridView;

            var workingWidth = list.ActualWidth - SystemParameters.VerticalScrollBarWidth;
            
            var t1 = workingWidth * 0.5;
            var t2 = workingWidth * 0.5;
            grid.Columns[0].Width = t1 > 0 ? t1 : 1;
            grid.Columns[1].Width = t2 > 0 ? t2 : 1;
        }

        #endregion

        #region Button handlers

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            IsApplied = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsApplied = false;
            Close();
        }

        #endregion

        #region Window handlers

        private void Window_StateChanged(object sender, EventArgs e)
        {
            var window = sender as LanguageWindow;
            if (window.WindowState == WindowState.Minimized)
                window.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(RefreshLanguages);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (IsApplied.GetValueOrDefault() &&
                LanguageList.SelectedItem is AppLanguageEntry languageEntry)
            {
                _languageProvider.Language = languageEntry.Language;
            }
            else
            {
                _languageProvider.Language = InitialLanguage;
            }
        }

        #endregion
    }
}
