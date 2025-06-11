using Constructor_Gallerix.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace Constructor_Gallerix.Templates
{
    public class MosaicTemplate : IExhibitionTemplate
    {
        public bool IsSpecialMode => false;

        public UIElement Render(List<Picture> pictures, string title, string description,
                        BitmapImage coverImage, RoutedEventHandler onImageClick)
        {
            var mainStack = new StackPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240))
            };

            if (pictures == null || pictures.Count == 0)
            {
                mainStack.Children.Add(CreateEmptyMessage(coverImage));
                return mainStack;
            }

            var wrapPanel = new WrapPanel
            {
                Margin = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            foreach (var pic in pictures)
            {
                if (pic?.Image == null) continue;
                wrapPanel.Children.Add(CreatePinterestMosaicItem(pic, onImageClick));
            }

            var scrollViewer = new ScrollViewer
            {
                Content = wrapPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(10),
                Background = Brushes.Transparent
            };

            scrollViewer.PreviewMouseWheel += (s, e) =>
            {
                if (e.Delta > 0)
                    scrollViewer.LineUp();
                else
                    scrollViewer.LineDown();
                e.Handled = true;
            };

            mainStack.Children.Add(scrollViewer);
            return mainStack;
        }

        private Border CreatePinterestMosaicItem(Picture pic, RoutedEventHandler onImageClick)
        {
            // Настройки для ограничения размеров
            double maxWidth = 260;   // Максимальная ширина карточки
            double maxHeight = 340;  // Максимальная высота карточки
            double minWidth = 140;   // Минимальная ширина для узких работ

            // Получаем пропорции
            double imgWidth = pic.Image.PixelWidth;
            double imgHeight = pic.Image.PixelHeight;
            double aspect = imgWidth / imgHeight;

            // Выбираем размеры под ориентацию (пропорции)
            double displayWidth, displayHeight;

            if (Math.Abs(aspect - 1.0) < 0.12) // квадратная (±12%)
            {
                displayWidth = displayHeight = Math.Min(maxWidth, maxHeight);
            }
            else if (aspect > 1.1) // горизонтальная
            {
                displayWidth = maxWidth;
                displayHeight = Math.Max(minWidth, maxWidth / aspect);
            }
            else // вертикальная
            {
                displayWidth = Math.Max(minWidth, maxHeight * aspect);
                displayHeight = maxHeight;
            }

            // Картинка с сохранением пропорций
            var image = new Image
            {
                Source = pic.Image,
                Width = displayWidth,
                Height = displayHeight,
                Stretch = Stretch.Uniform,
                DataContext = pic
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

            // Подложка под картинку с закруглёнными углами
            var imageContainer = new Border
            {
                Width = displayWidth,
                Height = displayHeight,
                Background = Brushes.White,
                CornerRadius = new CornerRadius(14),
                ClipToBounds = true,
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(100, 100, 100),
                    BlurRadius = 12,
                    Opacity = 0.20,
                    ShadowDepth = 3
                },
                Child = image
            };

            // Блок с подписью, появляющийся при наведении
            var caption = new TextBlock
            {
                Text = $"{pic.Title}\nАвтор: {pic.Author}",
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(12, 0, 12, 12),
                VerticalAlignment = VerticalAlignment.Bottom,
                TextAlignment = TextAlignment.Left,
                Visibility = Visibility.Collapsed,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    ShadowDepth = 1,
                    BlurRadius = 5
                }
            };

            var captionBackground = new Border
            {
                Background = new LinearGradientBrush(
                    new GradientStopCollection {
                        new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.5),
                        new GradientStop(Color.FromArgb(200, 30, 30, 30), 1)
                    },
                    new Point(0.5, 0),
                    new Point(0.5, 1)
                ),
                CornerRadius = new CornerRadius(0, 0, 14, 14)
            };

            var captionContainer = new Grid
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Height = 70
            };
            captionContainer.Children.Add(captionBackground);
            captionContainer.Children.Add(caption);

            var grid = new Grid();
            grid.Children.Add(imageContainer);
            grid.Children.Add(captionContainer);

            var border = new Border
            {
                Width = displayWidth,
                Height = displayHeight,
                Margin = new Thickness(9, 9, 9, 9),
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(14),
                Cursor = Cursors.Hand,
                Child = grid,
                DataContext = pic
            };

            // Hover-эффект: появляется подпись и тень, увеличение масштаба
            border.MouseEnter += (s, e) =>
            {
                caption.Visibility = Visibility.Visible;
                border.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(70, 70, 70),
                    BlurRadius = 22,
                    Opacity = 0.42,
                    ShadowDepth = 7
                };

                var scaleAnim = new DoubleAnimation(1.05, TimeSpan.FromMilliseconds(160))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var transform = border.RenderTransform as ScaleTransform;
                if (transform == null)
                {
                    transform = new ScaleTransform(1.0, 1.0, 0.5, 0.5);
                    border.RenderTransform = transform;
                }
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            };

            border.MouseLeave += (s, e) =>
            {
                caption.Visibility = Visibility.Collapsed;
                border.Effect = imageContainer.Effect;

                var scaleAnim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(160))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var transform = border.RenderTransform as ScaleTransform;
                if (transform != null)
                {
                    transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                    transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                }
            };

            border.MouseLeftButtonUp += (s, e) => onImageClick?.Invoke(border, e);

            return border;
        }

        private UIElement CreateEmptyMessage(BitmapImage coverImage)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 240, 240, 240)),
                Padding = new Thickness(30),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                CornerRadius = new CornerRadius(12),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(100, 100, 100),
                    BlurRadius = 15,
                    Opacity = 0.2,
                    ShadowDepth = 5
                }
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
    }
}
