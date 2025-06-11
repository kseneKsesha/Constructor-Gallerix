// File: FullscreenSliderTemplate.cs
using Constructor_Gallerix.Models;
using Constructor_Gallerix.View; // AudioGuideService
using System;
using System.Collections.Generic;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace Constructor_Gallerix.Templates
{
    public class FullscreenSliderTemplate : IExhibitionTemplate
    {
        private int currentIndex = 0;
        private List<Picture> pictures;
        private ScrollViewer mainScrollViewer;
        private StackPanel contentPanel;
        private Border imageContainer;
        private Grid mainGrid;
        private Border infoPanel;
        private string galleryTitle;
        private string galleryDescription;

        public bool IsSpecialMode => true;

        /// <summary>
        /// Рендерит шаблон с возможностью полноэкранной навигации и голосовым сопровождением.
        /// </summary>
        public UIElement Render(
            List<Picture> pictures,
            string title,
            string description,
            BitmapImage coverImage,
            RoutedEventHandler unusedHandler)
        {
            this.pictures = pictures;
            this.galleryTitle = string.IsNullOrWhiteSpace(title) ? "Без названия" : title;
            this.galleryDescription = string.IsNullOrWhiteSpace(description)
                                      ? "Описание отсутствует."
                                      : description;

            mainGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 13, 22))
            };

            if (pictures == null || pictures.Count == 0)
            {
                mainGrid.Children.Add(CreateEmptyMessage(coverImage));
                return mainGrid;
            }

            //--------------------------------------------------
            // 1) Онлайн‐озвучка заголовка/описания галереи
            //--------------------------------------------------
            if (Application.Current.Properties.Contains("IsAudioGuideEnabled") &&
                (bool)Application.Current.Properties["IsAudioGuideEnabled"])
            {
                string galleryText = $"Вы открыли выставку «{galleryTitle}». {galleryDescription}.";

                // Останавливаем предыдущую речь
                AudioGuideService.Stop();

                // Отписываемся от старого обработчика, если он был
                AudioGuideService.SpeakCompleted -= OnGalleryTextSpoken;

                // Начинаем озвучку заголовка и описания
                AudioGuideService.Speak(galleryText);

                // Подписываемся на окончание речи, чтобы после него озвучить картину
                AudioGuideService.SpeakCompleted += OnGalleryTextSpoken;
            }

            //--------------------------------------------------
            // 2) Построение UI: картинки и инфопанели
            //--------------------------------------------------
            var visualGrid = new Grid();

            // Контейнер для первой картинки
            imageContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(240, 25, 20, 40)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(30),
                CornerRadius = new CornerRadius(22),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(20, 10, 40),
                    BlurRadius = 32,
                    Opacity = 0.35,
                    ShadowDepth = 14
                },
                Child = CreateImageControl(pictures[0])
            };

            // Инфопанель для первой картины
            infoPanel = CreateInfoPanel(pictures[0]);
            infoPanel.Margin = new Thickness(0, 25, 0, 0);

            // Кнопки навигации «‹» и «›»
            var leftBtn = CreateNavButton("❮", () => ChangeImage(-1));
            var rightBtn = CreateNavButton("❯", () => ChangeImage(1));
            leftBtn.VerticalAlignment = VerticalAlignment.Center;
            rightBtn.VerticalAlignment = VerticalAlignment.Center;
            leftBtn.HorizontalAlignment = HorizontalAlignment.Left;
            rightBtn.HorizontalAlignment = HorizontalAlignment.Right;
            leftBtn.Margin = new Thickness(40, 0, 0, 0);
            rightBtn.Margin = new Thickness(0, 0, 40, 0);

            visualGrid.Children.Add(imageContainer);
            visualGrid.Children.Add(leftBtn);
            visualGrid.Children.Add(rightBtn);

            // Складываем всё в StackPanel
            contentPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    visualGrid,
                    infoPanel
                }
            };

            // Оборачиваем в ScrollViewer
            mainScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = contentPanel,
                Background = Brushes.Transparent
            };

            mainGrid.Children.Add(mainScrollViewer);

            // Обработка клавиш (← → Пробел F)
            mainGrid.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Right) ChangeImage(1);
                else if (e.Key == Key.Left) ChangeImage(-1);
                else if (e.Key == Key.Space) ScrollToTop();
                else if (e.Key == Key.F) ToggleFullscreen();
            };

            mainGrid.Focusable = true;
            mainGrid.Loaded += (s, e) => mainGrid.Focus();

            //--------------------------------------------------
            // Если аудиогид выключён, озвучиваем текущую картину сразу
            //--------------------------------------------------
            if (!(Application.Current.Properties.Contains("IsAudioGuideEnabled") &&
                  (bool)Application.Current.Properties["IsAudioGuideEnabled"]))
            {
                TrySpeakCurrentPicture();
            }

            //--------------------------------------------------
            // При выгрузке окна останавливаем речь и озвучиваем финальную фразу
            //--------------------------------------------------
            mainGrid.Unloaded += (s, e) =>
            {
                if (Application.Current.Properties.Contains("IsAudioGuideEnabled") &&
                    (bool)Application.Current.Properties["IsAudioGuideEnabled"])
                {
                    AudioGuideService.Stop();
                    AudioGuideService.Speak("Оцените, пожалуйста, галерею.");
                }

                // Отписываемся от события, чтобы не осталось висячих подписок
                AudioGuideService.SpeakCompleted -= OnGalleryTextSpoken;
            };

            return mainGrid;
        }

        /// <summary>
        /// После завершения озвучки заголовка/описания выставки —
        /// озвучиваем первую (или текущую) картину.
        /// </summary>
        private void OnGalleryTextSpoken(object sender, SpeakCompletedEventArgs e)
        {
            // Отписываемся сразу, чтобы не было повторных вызовов
            AudioGuideService.SpeakCompleted -= OnGalleryTextSpoken;

            // Озвучиваем первую картину
            TrySpeakCurrentPicture();
        }

        private Button CreateNavButton(string content, Action action)
        {
            var button = new Button
            {
                Content = content,
                Width = 58,
                Height = 58,
                Background = new SolidColorBrush(Color.FromArgb(170, 30, 25, 60)),
                Foreground = Brushes.White,
                FontSize = 28,
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 60, 160)),
                BorderThickness = new Thickness(2),
                Cursor = Cursors.Hand,
                Opacity = 0.92,
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(30, 15, 60),
                    BlurRadius = 18,
                    Opacity = 0.35,
                    ShadowDepth = 5
                }
            };

            button.MouseEnter += (s, e) =>
            {
                button.Background = new SolidColorBrush(Color.FromArgb(200, 46, 40, 80));
            };
            button.MouseLeave += (s, e) =>
            {
                button.Background = new SolidColorBrush(Color.FromArgb(170, 30, 25, 60));
            };

            button.Click += (s, e) => action();
            return button;
        }

        private void ToggleFullscreen()
        {
            var window = Window.GetWindow(mainGrid);
            if (window != null)
            {
                if (window.WindowState == WindowState.Maximized)
                {
                    window.WindowState = WindowState.Normal;
                    window.WindowStyle = WindowStyle.SingleBorderWindow;
                }
                else
                {
                    window.WindowState = WindowState.Maximized;
                    window.WindowStyle = WindowStyle.None;
                }
            }
        }

        private Image CreateImageControl(Picture picture)
        {
            var image = new Image
            {
                Source = picture.Image,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = SystemParameters.PrimaryScreenWidth * 0.92,
                MaxHeight = SystemParameters.PrimaryScreenHeight * 0.78,
                Cursor = Cursors.Hand,
                Opacity = 1
            };

            // Устанавливаем DataContext, чтобы использовать в озвучке
            image.DataContext = picture;

            // Двойной клик — переключение в полноэкранный режим
            image.MouseLeftButtonUp += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    ToggleFullscreen();
                }
            };

            return image;
        }

        private void ChangeImage(int delta)
        {
            if (pictures == null || pictures.Count == 0) return;

            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(280)
            };

            var oldImage = imageContainer.Child as Image;
            fadeOut.Completed += (s, e) =>
            {
                currentIndex = (currentIndex + delta + pictures.Count) % pictures.Count;
                var pic = pictures[currentIndex];

                var newImage = CreateImageControl(pic);
                imageContainer.Child = newImage;

                // Обновляем инфопанель
                var newInfoPanel = CreateInfoPanel(pic);
                contentPanel.Children.Remove(infoPanel);
                contentPanel.Children.Add(newInfoPanel);
                infoPanel = newInfoPanel;

                ScrollToTop();

                // Анимация появления
                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(320)
                };
                newImage.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                // После смены картинки — озвучиваем данные о ней
                TrySpeakCurrentPicture();
            };

            oldImage?.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void ScrollToTop()
        {
            mainScrollViewer.ScrollToVerticalOffset(0);
        }

        private Border CreateInfoPanel(Picture picture)
        {
            var grid = new Grid
            {
                Margin = new Thickness(0, 25, 0, 0),
                MinWidth = 350,
                MaxWidth = 700,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < 5; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var styleHeader = new Style(typeof(TextBlock));
            styleHeader.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
            styleHeader.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(190, 160, 255))));
            styleHeader.Setters.Add(new Setter(TextBlock.FontSizeProperty, 16.0));
            styleHeader.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(0, 0, 8, 0)));

            var styleValue = new Style(typeof(TextBlock));
            styleValue.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.WhiteSmoke));
            styleValue.Setters.Add(new Setter(TextBlock.FontSizeProperty, 15.0));
            styleValue.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(0, 0, 0, 4)));

            int row = 0;

            // 1. Название
            var titleLabel = new TextBlock { Text = "Название:", Style = styleHeader };
            var titleValue = new TextBlock { Text = picture.Title, Style = styleValue, TextWrapping = TextWrapping.Wrap };
            grid.Children.Add(titleLabel);
            grid.Children.Add(titleValue);
            Grid.SetRow(titleLabel, row);
            Grid.SetColumn(titleLabel, 0);
            Grid.SetRow(titleValue, row);
            Grid.SetColumn(titleValue, 1);

            row++;
            // 2. Автор
            var authorLabel = new TextBlock { Text = "Автор:", Style = styleHeader };
            var authorValue = new TextBlock { Text = picture.Author, Style = styleValue, TextWrapping = TextWrapping.Wrap };
            grid.Children.Add(authorLabel);
            grid.Children.Add(authorValue);
            Grid.SetRow(authorLabel, row);
            Grid.SetColumn(authorLabel, 0);
            Grid.SetRow(authorValue, row);
            Grid.SetColumn(authorValue, 1);

            row++;
            // 3. Год создания
            var yearLabel = new TextBlock { Text = "Год создания:", Style = styleHeader };
            var yearValue = new TextBlock
            {
                Text = picture.Year > 0 ? picture.Year.ToString() : "—",
                Style = styleValue
            };
            grid.Children.Add(yearLabel);
            grid.Children.Add(yearValue);
            Grid.SetRow(yearLabel, row);
            Grid.SetColumn(yearLabel, 0);
            Grid.SetRow(yearValue, row);
            Grid.SetColumn(yearValue, 1);

            row++;
            // 4. Комментарий эксперта
            var expertLabel = new TextBlock { Text = "Комментарий эксперта:", Style = styleHeader };
            var expertValue = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(picture.ExpertComment) ? "—" : picture.ExpertComment,
                Style = styleValue,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 600
            };
            grid.Children.Add(expertLabel);
            grid.Children.Add(expertValue);
            Grid.SetRow(expertLabel, row);
            Grid.SetColumn(expertLabel, 0);
            Grid.SetRow(expertValue, row);
            Grid.SetColumn(expertValue, 1);

            row++;
            // 5. Описание
            var descLabel = new TextBlock { Text = "Описание:", Style = styleHeader };
            var descValue = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(picture.Description) ? "—" : picture.Description,
                Style = styleValue,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 600
            };
            grid.Children.Add(descLabel);
            grid.Children.Add(descValue);
            Grid.SetRow(descLabel, row);
            Grid.SetColumn(descLabel, 0);
            Grid.SetRow(descValue, row);
            Grid.SetColumn(descValue, 1);

            var border = new Border
            {
                Child = grid,
                Padding = new Thickness(22, 12, 22, 12),
                Margin = new Thickness(0, 20, 0, 0),
                Background = new SolidColorBrush(Color.FromArgb(180, 20, 15, 38)),
                CornerRadius = new CornerRadius(16),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(24, 9, 40),
                    BlurRadius = 10,
                    Opacity = 0.17,
                    ShadowDepth = 4
                }
            };

            return border;
        }

        private UIElement CreateEmptyMessage(BitmapImage coverImage)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 22, 16, 40)),
                Padding = new Thickness(30),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var stack = new StackPanel();
            if (coverImage != null)
            {
                stack.Children.Add(new Image
                {
                    Source = coverImage,
                    Height = 200,
                    Margin = new Thickness(0, 0, 0, 15),
                    Stretch = Stretch.Uniform
                });
            }
            border.Child = stack;
            return border;
        }

        /// <summary>
        /// Озвучивает данные текущей картины одним единым вызовом.
        /// </summary>
        private void TrySpeakCurrentPicture()
        {
            if (!Application.Current.Properties.Contains("IsAudioGuideEnabled") ||
                !(bool)Application.Current.Properties["IsAudioGuideEnabled"])
                return;

            var pic = pictures[currentIndex];
            string title = string.IsNullOrWhiteSpace(pic.Title) ? "Без названия" : pic.Title;
            string author = string.IsNullOrWhiteSpace(pic.Author) ? "Автор неизвестен" : pic.Author;
            string year = pic.Year > 0 ? pic.Year.ToString() : "Год не указан";
            string desc = string.IsNullOrWhiteSpace(pic.Description)
                            ? "Описание картины отсутствует."
                            : pic.Description;
            string expertText = string.IsNullOrWhiteSpace(pic.ExpertComment)
                                ? "Комментарий эксперта отсутствует."
                                : pic.ExpertComment;

            string fullText =
                $"Вы выбрали картину «{title}». Автор: {author}. Год: {year}. " +
                $"{desc} " +
                $"Комментарий эксперта: {expertText}.";

            AudioGuideService.Speak(fullText);
        }
    }
}
