using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Constructor_Gallerix.Models;
using Constructor_Gallerix.Repository;
using Constructor_Gallerix.Templates;
using Constructor_Gallerix.View;  // AudioGuideService

namespace Constructor_Gallerix.View
{
    public partial class ExhibitionView : Window
    {
        private readonly List<Picture> _pictures;
        private readonly string _templateName;
        private readonly string _galleryTitle;
        private readonly string _galleryDescription;
        private readonly BitmapImage _coverImage;
        private IExhibitionTemplate _currentTemplate;
        private bool _isAudioEnabled;

        public ExhibitionView(string title, string description, string template,
                              List<Picture> pictures, BitmapImage coverImage)
        {
            InitializeComponent();

            if (pictures == null)
                throw new ArgumentNullException(nameof(pictures));
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            _pictures = pictures;
            _templateName = template;
            _galleryTitle = title ?? string.Empty;
            _galleryDescription = description ?? string.Empty;
            _coverImage = coverImage;

            InitializeWindowSettings();
            InitializeEventHandlers();
        }

        private void InitializeWindowSettings()
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;
            Background = Brushes.Black;
        }

        private void InitializeEventHandlers()
        {
            Loaded += OnWindowLoaded;
            KeyDown += OnWindowKeyDown;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Определяем текущее состояние аудиогида из Application properties
            _isAudioEnabled = Application.Current.Properties.Contains("IsAudioGuideEnabled") &&
                              (bool)Application.Current.Properties["IsAudioGuideEnabled"];

            // Устанавливаем текст кнопки в зависимости от состояния
            btnAudioGuideToggle.Content = _isAudioEnabled
                ? "Отключить аудиогид"
                : "Включить аудиогид";

            // Если аудиогид включён, озвучиваем название и описание выставки
            if (_isAudioEnabled)
            {
                string title = string.IsNullOrWhiteSpace(_galleryTitle)
                               ? "Без названия"
                               : _galleryTitle;
                string desc = string.IsNullOrWhiteSpace(_galleryDescription)
                              ? "Описание отсутствует."
                              : _galleryDescription;

                AudioGuideService.Speak(
                    $"Вы открыли выставку «{title}». {desc} При нажатии на картину вы услышите её подробную информацию.");
            }

            // Загружаем и отображаем шаблон
            try
            {
                _currentTemplate = TemplateFactory.CreateTemplate(_templateName);
                if (_currentTemplate == null)
                {
                    ShowErrorAndClose("Не удалось создать шаблон галереи.");
                    return;
                }

                var galleryContent = _currentTemplate.Render(
                    _pictures,
                    _galleryTitle,
                    _galleryDescription,
                    _coverImage,
                    Picture_ClickHandler);

                MainContainer.Children.Clear();
                MainContainer.Children.Add(galleryContent);

                GalleryTitle.Text = _galleryTitle;
                GalleryDescription.Text = string.IsNullOrEmpty(_galleryDescription)
                    ? "Нет описания"
                    : _galleryDescription;

                Focus();
            }
            catch (Exception ex)
            {
                ShowErrorAndClose($"Ошибка при загрузке шаблона: {ex.Message}");
            }
        }

        private void ShowErrorAndClose(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        /// <summary>
        /// Переключает состояние аудиогида и обновляет кнопку.
        /// </summary>
        private void BtnAudioGuideToggle_Click(object sender, RoutedEventArgs e)
        {
            _isAudioEnabled = !_isAudioEnabled;
            Application.Current.Properties["IsAudioGuideEnabled"] = _isAudioEnabled;

            if (_isAudioEnabled)
            {
                btnAudioGuideToggle.Content = "Отключить аудиогид";
                AudioGuideService.Speak("Аудиогид включён");
            }
            else
            {
                btnAudioGuideToggle.Content = "Включить аудиогид";
                AudioGuideService.Stop();
            }
        }

        /// <summary>
        /// Обработчик клика по любой картине внутри шаблона.
        /// Асинхронно озвучивает метаданные картины (если аудиогид включён),
        /// затем открывает PictureDetailWindow.
        /// </summary>
        private void Picture_ClickHandler(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Picture picture)
            {
                if (_isAudioEnabled)
                {
                    // Собираем всю фразу сразу
                    string title = string.IsNullOrWhiteSpace(picture.Title) ? "Без названия" : picture.Title;
                    string author = string.IsNullOrWhiteSpace(picture.Author) ? "Автор неизвестен" : picture.Author;
                    string year = picture.Year > 0 ? picture.Year.ToString() : "Год не указан";
                    string desc = string.IsNullOrWhiteSpace(picture.Description)
                                    ? "Описание картины отсутствует."
                                    : picture.Description;
                    string expert = string.IsNullOrWhiteSpace(picture.ExpertComment)
                                    ? "Комментарий эксперта отсутствует."
                                    : picture.ExpertComment;

                    // Склеиваем всё в одну строку. Синтезатор поставит паузы на точках.
                    string fullText =
                        $"Вы выбрали картину «{title}». " +
                        $"Автор: {author}. " +
                        $"Год создания: {year}. " +
                        $"{desc} " +
                        $"Комментарий эксперта: {expert}.";

                    AudioGuideService.Speak(fullText);
                }

                var detailWindow = new PictureDetailWindow(picture)
                {
                    Owner = this
                };
                detailWindow.ShowDialog();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Loaded -= OnWindowLoaded;
            KeyDown -= OnWindowKeyDown;
            if (_currentTemplate is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
