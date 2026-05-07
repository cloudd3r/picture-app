using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace PictureApp
{
    public partial class MainWindow : Window
    {
        // Расширения, которые поддерживает WIC (BitmapDecoder) на Windows 10
        private static readonly HashSet<string> SupportedExtensions = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".jpe", ".jfif",
            ".png", ".bmp", ".dib", ".gif",
            ".tif", ".tiff", ".ico", ".wdp", ".jxr",
            ".webp" // Win10 имеет WIC-кодек для WebP с обновлениями; LTSC может не иметь.
        };

        private List<string> _files = new List<string>();
        private int _index = -1;
        private readonly string _initialPath;

        public MainWindow() : this(null) { }

        public MainWindow(string initialPath)
        {
            InitializeComponent();
            _initialPath = initialPath;
        }

        // ----- Загрузка / навигация -----

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Включаем blur-behind (глассморфизм) после получения hwnd
            try { AcrylicHelper.EnableBlurBehind(this); }
            catch
            {
                // На системах без поддержки blur просто оставляем полупрозрачный фон
            }

            if (!string.IsNullOrEmpty(_initialPath) && File.Exists(_initialPath))
            {
                LoadFromPath(_initialPath);
            }
            else
            {
                UpdatePlaceholder();
            }

            Focus();
        }

        private void LoadFromPath(string path)
        {
            try
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(path));
                if (string.IsNullOrEmpty(directory))
                {
                    directory = Environment.CurrentDirectory;
                }

                _files = Directory
                    .EnumerateFiles(directory)
                    .Where(IsSupported)
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                _index = _files.FindIndex(
                    f => string.Equals(f, Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase));

                if (_index < 0 && _files.Count > 0)
                {
                    _index = 0;
                }

                ShowCurrent();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть файл:\n" + ex.Message,
                    "PictureApp", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static bool IsSupported(string path)
        {
            string ext = Path.GetExtension(path);
            return !string.IsNullOrEmpty(ext) && SupportedExtensions.Contains(ext);
        }

        private void ShowCurrent()
        {
            if (_index < 0 || _index >= _files.Count)
            {
                ImageView.Source = null;
                UpdatePlaceholder();
                return;
            }

            string path = _files[_index];
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();

                ImageView.Source = bmp;
                PlaceholderText.Visibility = Visibility.Collapsed;
                TitleText.Text = string.Format("{0}  —  {1}/{2}",
                    Path.GetFileName(path), _index + 1, _files.Count);
                Title = TitleText.Text;
            }
            catch (Exception ex)
            {
                ImageView.Source = null;
                PlaceholderText.Text = "Не удалось загрузить:\n" + Path.GetFileName(path);
                PlaceholderText.Visibility = Visibility.Visible;
                TitleText.Text = "PictureApp — ошибка";
                Title = TitleText.Text;
                System.Diagnostics.Debug.WriteLine(ex);
            }

            UpdateNavButtons();
        }

        private void UpdatePlaceholder()
        {
            if (ImageView.Source == null)
            {
                PlaceholderText.Text = _files.Count == 0
                    ? "Перетащите картинку\nили откройте файл"
                    : "Картинка";
                PlaceholderText.Visibility = Visibility.Visible;
            }
            UpdateNavButtons();
        }

        private void UpdateNavButtons()
        {
            PrevButton.IsEnabled = _files.Count > 1;
            NextButton.IsEnabled = _files.Count > 1;
        }

        private void Next()
        {
            if (_files.Count == 0) return;
            _index = (_index + 1) % _files.Count;
            ShowCurrent();
        }

        private void Prev()
        {
            if (_files.Count == 0) return;
            _index = (_index - 1 + _files.Count) % _files.Count;
            ShowCurrent();
        }

        // ----- Обработчики UI -----

        private void OnPrevClick(object sender, RoutedEventArgs e) => Prev();
        private void OnNextClick(object sender, RoutedEventArgs e) => Next();

        private void OnImageMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                e.Handled = true;
                return;
            }

            // Клик по левой/правой половине карточки = листание
            if (_files.Count == 0) return;
            var pos = e.GetPosition(ImageCard);
            if (pos.X < ImageCard.ActualWidth / 2.0)
                Prev();
            else
                Next();
            e.Handled = true;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.PageUp:
                    Prev();
                    e.Handled = true;
                    break;
                case Key.Right:
                case Key.PageDown:
                case Key.Space:
                    Next();
                    e.Handled = true;
                    break;
                case Key.Home:
                    if (_files.Count > 0) { _index = 0; ShowCurrent(); }
                    e.Handled = true;
                    break;
                case Key.End:
                    if (_files.Count > 0) { _index = _files.Count - 1; ShowCurrent(); }
                    e.Handled = true;
                    break;
                case Key.O:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        OpenFileDialog();
                        e.Handled = true;
                    }
                    break;
                case Key.F11:
                    ToggleMaximize();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    if (WindowState == WindowState.Maximized)
                        WindowState = WindowState.Normal;
                    else
                        Close();
                    e.Handled = true;
                    break;
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_files.Count == 0) return;
            if (e.Delta > 0) Prev(); else Next();
            e.Handled = true;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths == null || paths.Length == 0) return;
            string first = paths.FirstOrDefault(IsSupported) ?? paths[0];
            LoadFromPath(first);
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                return;
            }
            try { DragMove(); } catch { /* DragMove иногда кидает на быстрых кликах */ }
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void OnMaximizeClick(object sender, RoutedEventArgs e) => ToggleMaximize();
        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            // В развёрнутом режиме убираем поле под тень и скругление, чтобы окно занимало
            // всю клиентскую область монитора без зазоров.
            if (WindowState == WindowState.Maximized)
            {
                OuterBorder.Margin = new Thickness(0);
                OuterBorder.CornerRadius = new CornerRadius(0);
            }
            else
            {
                OuterBorder.Margin = new Thickness(16);
                OuterBorder.CornerRadius = new CornerRadius(14);
            }
        }

        private void OpenFileDialog()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Открыть изображение",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff;*.ico;*.webp|Все файлы|*.*"
            };
            if (dlg.ShowDialog(this) == true)
            {
                LoadFromPath(dlg.FileName);
            }
        }
    }
}
