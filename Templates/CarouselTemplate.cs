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
    public class CarouselTemplate : IExhibitionTemplate
    {
        private const int CenterImageMaxWidth = 550;
        private const int SideImageMaxWidth = 300;
        private const int AnimationDuration = 600;
        private RoutedEventHandler externalClickHandler;
        private Canvas canvas;
        private List<Border> items;
        private List<Picture> pictures;
        private int currentIndex;
        private double canvasWidth = 1200;
        private double canvasHeight = 700;

        public bool IsSpecialMode => false;

        public UIElement Render(List<Picture> pictures, string title, string description,
                              BitmapImage coverImage, RoutedEventHandler onImageClick)
        {
            this.externalClickHandler = onImageClick;
            this.pictures = pictures;
            currentIndex = 0;

            var grid = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(27, 20, 72)) // Тёмно-фиолетовый фон
            };

            if (pictures == null || pictures.Count == 0)
            {
                grid.Children.Add(CreateEmptyMessage(coverImage));
                return grid;
            }

            canvas = new Canvas
            {
                Width = canvasWidth,
                Height = canvasHeight,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            items = new List<Border>();
            for (int offset = -1; offset <= 1; offset++)
            {
                int idx = Mod(currentIndex + offset, pictures.Count);
                var border = CreateCarouselItem(pictures[idx]);
                items.Add(border);
                canvas.Children.Add(border);
            }

            ShowImage(0);

            grid.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Right) ShowImage(1);
                else if (e.Key == Key.Left) ShowImage(-1);
            };
            grid.Focusable = true;
            grid.Loaded += (s, e) => grid.Focus();

            var btnPrev = CreateNavButton("❮", () => ShowImage(-1), HorizontalAlignment.Left);
            var btnNext = CreateNavButton("❯", () => ShowImage(1), HorizontalAlignment.Right);

            grid.Children.Add(canvas);
            grid.Children.Add(btnPrev);
            grid.Children.Add(btnNext);

            return grid;
        }

        private void ShowImage(int direction)
        {
            if (pictures == null || pictures.Count == 0) return;

            currentIndex = Mod(currentIndex + direction, pictures.Count);

            for (int i = 0; i < 3; i++)
            {
                int offset = i - 1;
                int picIndex = Mod(currentIndex + offset, pictures.Count);
                var pic = pictures[picIndex];
                var border = items[i];
                var image = (Image)border.Child;
                image.Source = pic.Image;
                border.DataContext = pic;

                double imgW = pic.Image.PixelWidth;
                double imgH = pic.Image.PixelHeight;
                double aspect = imgW / imgH;

                double targetWidth = (offset == 0) ? CenterImageMaxWidth : SideImageMaxWidth;
                double targetHeight = targetWidth / aspect;

                if (targetHeight > canvasHeight - 100)
                {
                    targetHeight = canvasHeight - 100;
                    targetWidth = targetHeight * aspect;
                }

                double centerX = (canvasWidth - targetWidth) / 2;
                double centerY = (canvasHeight - targetHeight) / 2;

                double targetX, targetY = centerY;
                double targetOpacity = (offset == 0) ? 1.0 : 0.4;
                double targetScale = (offset == 0) ? 1.0 : 0.8;
                int targetZ = (offset == 0) ? 1 : 0;

                if (offset == 0)
                {
                    targetX = centerX;
                }
                else if (offset < 0)
                {
                    targetX = 50;
                }
                else
                {
                    targetX = canvasWidth - targetWidth - 50;
                }

                border.Width = targetWidth;
                border.Height = targetHeight;

                if (!(border.RenderTransform is ScaleTransform))
                {
                    border.RenderTransform = new ScaleTransform(1, 1);
                    border.RenderTransformOrigin = new Point(0.5, 0.5);
                }

                var xAnim = new DoubleAnimation(targetX, TimeSpan.FromMilliseconds(AnimationDuration))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Canvas.SetLeft(border, targetX);
                border.BeginAnimation(Canvas.LeftProperty, xAnim);

                var yAnim = new DoubleAnimation(targetY, TimeSpan.FromMilliseconds(AnimationDuration))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Canvas.SetTop(border, targetY);
                border.BeginAnimation(Canvas.TopProperty, yAnim);

                var opacityAnim = new DoubleAnimation(targetOpacity, TimeSpan.FromMilliseconds(AnimationDuration));
                border.BeginAnimation(UIElement.OpacityProperty, opacityAnim);

                var scaleAnim = new DoubleAnimation(targetScale, TimeSpan.FromMilliseconds(AnimationDuration))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                ((ScaleTransform)border.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                ((ScaleTransform)border.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

                Panel.SetZIndex(border, targetZ);
            }
        }

        private Button CreateNavButton(string content, Action action, HorizontalAlignment alignment)
        {
            var button = new Button
            {
                Content = content,
                Width = 60,
                Height = 60,
                Background = new SolidColorBrush(Color.FromArgb(200, 30, 25, 60)),
                Foreground = Brushes.White,
                FontSize = 28,
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 60, 160)),
                BorderThickness = new Thickness(2),
                HorizontalAlignment = alignment,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(30, 0, 30, 0),
                Cursor = Cursors.Hand,
                Opacity = 0.85,
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(20, 10, 40),
                    BlurRadius = 16,
                    Opacity = 0.5,
                    ShadowDepth = 4
                }
            };

            button.MouseEnter += (s, e) => button.Background = new SolidColorBrush(Color.FromArgb(230, 46, 40, 80));
            button.MouseLeave += (s, e) => button.Background = new SolidColorBrush(Color.FromArgb(200, 30, 25, 60));

            button.Click += (s, e) => action();
            return button;
        }

        private Border CreateCarouselItem(Picture pic)
        {
            var image = new Image
            {
                Source = pic.Image,
                Stretch = Stretch.Uniform,
                Cursor = Cursors.Hand
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

            var border = new Border
            {
                CornerRadius = new CornerRadius(15),
                Background = Brushes.Transparent,
                Child = image,
                DataContext = pic,
                Opacity = 0,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 22,
                    Opacity = 0.8,
                    ShadowDepth = 8
                }
            };

            border.MouseLeftButtonUp += (s, e) =>
            {
                externalClickHandler?.Invoke(image, e);
            };

            return border;
        }

        private UIElement CreateEmptyMessage(BitmapImage coverImage)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 30, 30, 30)),
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

        private int Mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }
    }
}
