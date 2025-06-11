using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Constructor_Gallerix.Models;
using Constructor_Gallerix.Repository;
using Npgsql;

namespace Constructor_Gallerix.View
{
    public partial class DemoView : Window
    {
        private readonly int _userId;
        private readonly string _username;
        private readonly string _connectionString = "Host=localhost;Port=5432;Username=postgres;Password=1234;Database=GallerixDB";
        private List<DemoGalleryItem> galleries = new List<DemoGalleryItem>();

        public DemoView(int userId, string username)
        {
            InitializeComponent();
            _userId = userId;
            _username = username;
            txtUsername.Text = _username;

            LoadPublicGalleries();
        }

        // Загружаем только публичные галереи текущего пользователя + считаем средний рейтинг
        private void LoadPublicGalleries()
        {
            galleries.Clear();
            using (var repo = new ExhibitionRepository(_connectionString))
            {
                var publicGalleries = repo.GetUserExhibitions(_userId)
                                          .Where(x => x.IsPublic)
                                          .ToList();
                foreach (var gallery in publicGalleries)
                {
                    // Получить картинку (cover)
                    BitmapImage coverImage = null;
                    if (gallery.CoverImageData != null && gallery.CoverImageData.Length > 0)
                        coverImage = ByteArrayToBitmapImage(gallery.CoverImageData);

                    // Получить рейтинг и звездочки
                    var (avgRating, starStr) = GetAverageRating(gallery.ExhibitionId);

                    galleries.Add(new DemoGalleryItem
                    {
                        ExhibitionId = gallery.ExhibitionId,
                        Title = gallery.Title,
                        CreatedAt = gallery.CreatedAt,
                        CoverImage = coverImage,
                        Rating = avgRating,
                        Stars = starStr.Select(c => c.ToString()).ToArray(),
                        RatingText = avgRating > 0 ? avgRating.ToString("0.0") : "-"
                    });
                }
            }
            GalleriesList.ItemsSource = galleries;
            txtNoResults.Visibility = galleries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private (double, string) GetAverageRating(int exhibitionId)
        {
            double rating = 0;
            int count = 0;
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "SELECT AVG(rating)::float, COUNT(*) FROM reviews WHERE exhibition_id = @exid", conn))
                {
                    cmd.Parameters.AddWithValue("@exid", exhibitionId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            rating = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                            count = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        }
                    }
                }
            }
            // Формируем строку из 5 символов: ★ и ☆
            int starsToFill = (int)Math.Round(rating);
            string starStr = new string('★', starsToFill) + new string('☆', 5 - starsToFill);
            return (rating, starStr);
        }

        private BitmapImage ByteArrayToBitmapImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            using (var stream = new MemoryStream(bytes))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }

        // Открыть предпросмотр галереи (как в конструкторе)
        private void Gallery_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (sender is FrameworkElement el && el.DataContext is DemoGalleryItem gallery)
            {
                try
                {
                    using (var repo = new ExhibitionRepository(_connectionString))
                    {
                        var fullGallery = repo.GetExhibitionWithPictures(gallery.ExhibitionId);
                        var previewWindow = new ExhibitionView(
                            fullGallery.Title,
                            fullGallery.Description,
                            fullGallery.Template,
                            fullGallery.Pictures,
                            gallery.CoverImage
                        );
                        previewWindow.Show();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка предпросмотра: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Gallery_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(0.9, TimeSpan.FromMilliseconds(200)));
            }
        }

        private void Gallery_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(200)));
            }
        }

        // --- Меню ---
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            var mainView = new MainView(_username, _userId);
            mainView.Show();
            this.Close();
        }

        private void ConstructorButton_Click(object sender, RoutedEventArgs e)
        {
            var constructorView = new ConstructorView(_userId, _username, 0);
            constructorView.Show();
            this.Close();
        }

        private void MyWorksButton_Click(object sender, RoutedEventArgs e)
        {
            var myWorksView = new MyWorksView(_userId, _username);
            myWorksView.Show();
            this.Close();
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileView = new ProfileView(_userId, _username);
            profileView.Show();
            this.Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var help = new HelpView(_username, _userId);
            help.Owner = this;
            help.Show();
            // **DemoView остаётся открытой**
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        // DTO для показа галерей
        private class DemoGalleryItem
        {
            public int ExhibitionId { get; set; }
            public string Title { get; set; }
            public DateTime CreatedAt { get; set; }
            public BitmapImage CoverImage { get; set; }
            public double Rating { get; set; }
            public string[] Stars { get; set; }
            public string RatingText { get; set; }
        }
    }
}
