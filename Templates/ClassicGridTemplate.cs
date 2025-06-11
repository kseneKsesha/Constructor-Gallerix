using Constructor_Gallerix.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace Constructor_Gallerix.Templates
{
    public class ClassicGridTemplate : IExhibitionTemplate
    {
        public bool IsSpecialMode => false;

        public UIElement Render(List<Picture> pictures, string title, string description,
                       BitmapImage coverImage, RoutedEventHandler pictureClickHandler)
        {
            var mainGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(27, 20, 72))
            };

            if (pictures == null || pictures.Count == 0)
            {
                mainGrid.Children.Add(CreateEmptyGalleryMessage(coverImage));
                return mainGrid;
            }

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(5)
            };

            var wrapPanel = new WrapPanel
            {
                Margin = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top
            };

            foreach (var picture in pictures)
            {
                if (picture?.Image == null) continue;
                wrapPanel.Children.Add(CreatePictureCard(picture, pictureClickHandler));
            }

            // Прокрутка колесиком мыши
            scrollViewer.PreviewMouseWheel += (s, e) =>
            {
                if (e.Delta > 0)
                    scrollViewer.LineUp();
                else
                    scrollViewer.LineDown();
                e.Handled = true;
            };

            scrollViewer.Content = wrapPanel;
            mainGrid.Children.Add(scrollViewer);
            return mainGrid;
        }

        private Border CreatePictureCard(Picture picture, RoutedEventHandler pictureClickHandler)
        {
            // Рассчитываем размеры карточки динамически по пропорциям картинки
            double maxWidth = 320;
            double maxHeight = 340;
            double minWidth = 150;

            double imgWidth = picture.Image.PixelWidth;
            double imgHeight = picture.Image.PixelHeight;
            double aspect = imgWidth / imgHeight;

            double cardWidth, cardHeight;

            if (System.Math.Abs(aspect - 1.0) < 0.13) // квадрат ±13%
            {
                cardWidth = cardHeight = System.Math.Min(maxWidth, maxHeight);
            }
            else if (aspect > 1.1) // горизонтальная
            {
                cardWidth = maxWidth;
                cardHeight = System.Math.Max(minWidth, maxWidth / aspect);
            }
            else // вертикальная
            {
                cardWidth = System.Math.Max(minWidth, maxHeight * aspect);
                cardHeight = maxHeight;
            }

            // Рамка вокруг картины
            var frame = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
                BorderThickness = new Thickness(2.5),
                BorderBrush = new LinearGradientBrush(
                    Color.FromRgb(200, 180, 255),
                    Color.FromRgb(160, 110, 240),
                    90),
                CornerRadius = new CornerRadius(15),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(90, 20, 120),
                    BlurRadius = 10,
                    Opacity = 0.13,
                    ShadowDepth = 2
                },
                Padding = new Thickness(5)
            };

            // Картина
            var image = new Image
            {
                Source = picture.Image,
                Width = cardWidth,
                Height = cardHeight,
                Stretch = Stretch.Uniform,
                DataContext = picture
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

            // Оверлей для наведения
            var hoverOverlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                CornerRadius = new CornerRadius(10)
            };

            var cardGrid = new Grid();
            cardGrid.Children.Add(image);
            cardGrid.Children.Add(hoverOverlay);
            cardGrid.Children.Add(CreatePictureCaption(picture));

            // Собираем всю карточку
            frame.Child = cardGrid;
            var cardBorder = new Border
            {
                Width = cardWidth + 10,
                Height = cardHeight + 10,
                Margin = new Thickness(15),
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(18),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(44, 9, 84), // глухой тёмно-фиолетовый
                    BlurRadius = 14,
                    Opacity = 0.38,
                    ShadowDepth = 7
                },
                Cursor = Cursors.Hand,
                ToolTip = CreatePictureTooltip(picture),
                DataContext = picture,
                Child = frame
            };

            // Наведение и убирание курсора
            cardBorder.MouseEnter += (s, e) =>
            {
                hoverOverlay.Background = new SolidColorBrush(Color.FromArgb(50, 30, 20, 70));
                cardBorder.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(130, 67, 255), // красивый фиолетовый свет
                    BlurRadius = 24,
                    Opacity = 0.55,
                    ShadowDepth = 12
                };
                frame.BorderBrush = new LinearGradientBrush(
                    Color.FromRgb(240, 200, 255),
                    Color.FromRgb(200, 120, 250),
                    90);
            };

            cardBorder.MouseLeave += (s, e) =>
            {
                hoverOverlay.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                cardBorder.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(44, 9, 84),
                    BlurRadius = 14,
                    Opacity = 0.38,
                    ShadowDepth = 7
                };
                frame.BorderBrush = new LinearGradientBrush(
                    Color.FromRgb(200, 180, 255),
                    Color.FromRgb(160, 110, 240),
                    90);
            };

            cardBorder.MouseLeftButtonUp += (s, e) => pictureClickHandler?.Invoke(cardBorder, e);

            return cardBorder;
        }

        private UIElement CreateEmptyGalleryMessage(BitmapImage coverImage)
        {
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (coverImage != null)
            {
                stack.Children.Add(new Image
                {
                    Source = coverImage,
                    Height = 200,
                    Margin = new Thickness(0, 0, 0, 15)
                });
            }
            return stack;
        }

        private ToolTip CreatePictureTooltip(Picture picture)
        {
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = picture.Title,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            stack.Children.Add(new Separator());
            stack.Children.Add(new TextBlock { Text = $"Автор: {picture.Author}" });
            stack.Children.Add(new TextBlock { Text = $"Год создания: {picture.Year}" });
            stack.Children.Add(new TextBlock { Text = $"Ориентация: {(picture.IsHorizontal ? "Горизонтальная" : "Вертикальная")}" });

            if (!string.IsNullOrEmpty(picture.Description))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"Описание: {picture.Description}",
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 300,
                    Margin = new Thickness(0, 5, 0, 0)
                });
            }

            return new ToolTip { Content = stack };
        }

        private UIElement CreatePictureCaption(Picture picture)
        {
            var captionPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = new LinearGradientBrush(
                    Color.FromArgb(210, 0, 0, 0),
                    Color.FromArgb(140, 0, 0, 0),
                    new Point(0.5, 0),
                    new Point(0.5, 1)),
                Opacity = 0.93
            };

            captionPanel.Children.Add(new TextBlock
            {
                Text = picture.Title,
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            captionPanel.Children.Add(new TextBlock
            {
                Text = $"Автор: {picture.Author}",
                FontSize = 14,
                Foreground = Brushes.WhiteSmoke,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            });

            return new Border
            {
                Padding = new Thickness(10),
                Child = captionPanel
            };
        }
    }
}
