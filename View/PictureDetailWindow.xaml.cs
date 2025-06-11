// File: PictureDetailWindow.xaml.cs
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using Constructor_Gallerix.Models;
using Constructor_Gallerix.View; // AudioGuideService

namespace Constructor_Gallerix.View
{
    public partial class PictureDetailWindow : Window
    {
        private readonly Picture _picture;

        public PictureDetailWindow(Picture picture)
        {
            InitializeComponent();
            _picture = picture ?? throw new ArgumentNullException(nameof(picture));
            DataContext = this;
            Title = _picture.Title;
            LoadPictureData();

            // События для окна
            Loaded += PictureDetailWindow_Loaded;
            Closing += PictureDetailWindow_Closing;
            KeyDown += Window_KeyDown;
        }

        private void PictureDetailWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current.Properties.Contains("IsAudioGuideEnabled") &&
                (bool)Application.Current.Properties["IsAudioGuideEnabled"])
            {
                string title = string.IsNullOrWhiteSpace(_picture.Title) ? "Без названия" : _picture.Title;
                string author = string.IsNullOrWhiteSpace(_picture.Author) ? "Автор неизвестен" : _picture.Author;
                string year = (_picture.Year > 0) ? _picture.Year.ToString() : "Год не указан";
                string descPic = string.IsNullOrWhiteSpace(_picture.Description)
                                 ? "Описание картины отсутствует."
                                 : _picture.Description;
                string expert = string.IsNullOrWhiteSpace(_picture.ExpertComment)
                                ? "Комментарий эксперта отсутствует."
                                : _picture.ExpertComment;

                string fullText =
                    $"Вы открыли картину «{title}». " +
                    $"Автор: {author}. " +
                    $"Год создания: {year}. " +
                    $"{descPic} " +
                    $"Комментарий эксперта: {expert}";

                AudioGuideService.Speak(fullText);
            }
        }

        private void PictureDetailWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AudioGuideService.Stop();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void LoadPictureData()
        {
            txtTitle.Text = _picture.Title;
            runAuthor.Text = _picture.Author;
            runYear.Text = _picture.Year > 0 ? _picture.Year.ToString() : "";
            txtDescription.Text = string.IsNullOrEmpty(_picture.Description)
                                  ? "Нет описания"
                                  : _picture.Description;
            txtExpertComment.Text = string.IsNullOrEmpty(_picture.ExpertComment)
                                     ? "Нет комментария эксперта"
                                     : _picture.ExpertComment;

            imgPicture.Source = _picture.Image;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Открывает картину во весь экран, позволяет перетаскивать и зумировать.
        /// </summary>
        private void ShowFullscreenImage(object sender, RoutedEventArgs e)
        {
            var fsWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                WindowState = WindowState.Maximized,
                Background = Brushes.Black
            };

            var rootGrid = new Grid();

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = Brushes.Transparent,
                CanContentScroll = false
            };

            var containerGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var img = new Image
            {
                Source = _picture.Image,
                Stretch = Stretch.None,
                Cursor = Cursors.SizeAll
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

            var scaleTransform = new ScaleTransform(1.0, 1.0);
            img.RenderTransform = scaleTransform;
            img.RenderTransformOrigin = new Point(0.5, 0.5);

            // Обработка перетаскивания мышью (панорамирование)
            Point origin = new Point();
            Point start = new Point();
            img.MouseLeftButtonDown += (s, ev) =>
            {
                img.CaptureMouse();
                start = ev.GetPosition(scroll);
                origin = new Point(scroll.HorizontalOffset, scroll.VerticalOffset);
            };
            img.MouseLeftButtonUp += (s, ev) =>
            {
                img.ReleaseMouseCapture();
            };
            img.MouseMove += (s, ev) =>
            {
                if (img.IsMouseCaptured)
                {
                    Vector delta = start - ev.GetPosition(scroll);
                    scroll.ScrollToHorizontalOffset(origin.X + delta.X);
                    scroll.ScrollToVerticalOffset(origin.Y + delta.Y);
                }
            };

            containerGrid.Children.Add(img);
            scroll.Content = containerGrid;
            rootGrid.Children.Add(scroll);

            var btnCloseFs = new Button
            {
                Content = "×",
                Style = (Style)Application.Current.FindResource("CloseButtonStyle"),
                Width = 40,
                Height = 40,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 20, 20, 0)
            };
            btnCloseFs.Click += (s, ev) => fsWindow.Close();
            rootGrid.Children.Add(btnCloseFs);

            var btnZoomIn = new Button
            {
                Content = "+",
                Width = 50,
                Height = 50,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 60)),
                BorderBrush = Brushes.Transparent,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(20, 0, 0, 20)
            };
            var hoverIn = new ScaleTransform(1, 1);
            btnZoomIn.RenderTransform = hoverIn;
            btnZoomIn.RenderTransformOrigin = new Point(0.5, 0.5);
            btnZoomIn.MouseEnter += (s, ev) =>
            {
                var ani = new DoubleAnimation(1.1, TimeSpan.FromMilliseconds(100));
                hoverIn.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                hoverIn.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
            };
            btnZoomIn.MouseLeave += (s, ev) =>
            {
                var ani = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(100));
                hoverIn.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                hoverIn.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
            };
            btnZoomIn.Click += (s, ev) =>
            {
                scaleTransform.ScaleX = Math.Min(scaleTransform.ScaleX + 0.2, 5.0);
                scaleTransform.ScaleY = Math.Min(scaleTransform.ScaleY + 0.2, 5.0);
                CenterImageAfterZoom();
            };
            rootGrid.Children.Add(btnZoomIn);

            var btnZoomOut = new Button
            {
                Content = "–",
                Width = 50,
                Height = 50,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 60)),
                BorderBrush = Brushes.Transparent,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(90, 0, 0, 20)
            };
            var hoverOut = new ScaleTransform(1, 1);
            btnZoomOut.RenderTransform = hoverOut;
            btnZoomOut.RenderTransformOrigin = new Point(0.5, 0.5);
            btnZoomOut.MouseEnter += (s, ev) =>
            {
                var ani = new DoubleAnimation(1.1, TimeSpan.FromMilliseconds(100));
                hoverOut.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                hoverOut.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
            };
            btnZoomOut.MouseLeave += (s, ev) =>
            {
                var ani = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(100));
                hoverOut.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                hoverOut.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
            };
            btnZoomOut.Click += (s, ev) =>
            {
                scaleTransform.ScaleX = Math.Max(scaleTransform.ScaleX - 0.2, 0.2);
                scaleTransform.ScaleY = Math.Max(scaleTransform.ScaleY - 0.2, 0.2);
                CenterImageAfterZoom();
            };
            rootGrid.Children.Add(btnZoomOut);

            fsWindow.KeyDown += (s, ev) =>
            {
                if (ev.Key == Key.Escape)
                    fsWindow.Close();
            };

            fsWindow.Content = rootGrid;

            fsWindow.ContentRendered += (s, ev) =>
            {
                CenterImageAfterZoom();
            };

            fsWindow.ShowDialog();

            void CenterImageAfterZoom()
            {
                if (img.ActualWidth <= 0 || img.ActualHeight <= 0)
                    return;

                double scaledWidth = img.ActualWidth * scaleTransform.ScaleX;
                double scaledHeight = img.ActualHeight * scaleTransform.ScaleY;

                // Центрируем контейнер, если изображение меньше области просмотра
                double marginLeft = 0, marginTop = 0;
                if (scaledWidth < scroll.ViewportWidth)
                    marginLeft = (scroll.ViewportWidth - scaledWidth) / 2;
                if (scaledHeight < scroll.ViewportHeight)
                    marginTop = (scroll.ViewportHeight - scaledHeight) / 2;
                containerGrid.Margin = new Thickness(marginLeft, marginTop, 0, 0);

                // Если изображение больше области просмотра, прокручиваем к центру
                if (scaledWidth > scroll.ViewportWidth)
                    scroll.ScrollToHorizontalOffset((scaledWidth - scroll.ViewportWidth) / 2);
                else
                    scroll.ScrollToHorizontalOffset(0);

                if (scaledHeight > scroll.ViewportHeight)
                    scroll.ScrollToVerticalOffset((scaledHeight - scroll.ViewportHeight) / 2);
                else
                    scroll.ScrollToVerticalOffset(0);
            }
        }
    }
}
