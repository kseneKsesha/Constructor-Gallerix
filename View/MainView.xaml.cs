using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Npgsql;
using Constructor_Gallerix.Models;

namespace Constructor_Gallerix.View
{
    public partial class MainView : Window
    {
        private const string ConnectionString = "Host=localhost;Username=postgres;Password=1234;Database=GallerixDB";
        private readonly int currentUserId;
        private readonly string currentUsername;

        public MainView(string username, int userId)
        {
            InitializeComponent();
            currentUsername = username;
            currentUserId = userId;
            txtUsername.Text = username;

            // Делаем кнопку "Главная" активной
            btnHome.Style = (Style)FindResource("ActiveMenuButtonStyle");
            btnHome.IsEnabled = false;

            LoadLastEditedGallery();
            LoadReviews();
        }

        private void ConstructorButton_Click(object sender, RoutedEventArgs e)
        {
            var constructorView = new ConstructorView(currentUserId, currentUsername, 0);
            constructorView.Show();
            this.Close();
        }

        private void MyWorksButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentUserId <= 0 || string.IsNullOrWhiteSpace(currentUsername))
            {
                MessageBox.Show("Недостаточно данных для открытия галереи", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var myWorksView = new MyWorksView(currentUserId, currentUsername);
            myWorksView.Show();
            this.Close();
        }

        private void DemoButton_Click(object sender, RoutedEventArgs e)
        {
            var demoView = new DemoView(currentUserId, currentUsername);
            demoView.Show();
            this.Close();
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileView = new ProfileView(currentUserId, currentUsername);
            profileView.Show();
            this.Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var help = new HelpView(currentUsername, currentUserId);
            help.Owner = this;
            help.ShowDialog();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        private void AppReview_Click(object sender, RoutedEventArgs e)
        {
            var appReviewView = new AppReviewView(currentUserId); // Предполагается, что форма реализована
            appReviewView.ShowDialog();
        }

        /// <summary>
        /// Загружает последнюю отредактированную галерею текущего пользователя и отображает её в блоке.
        /// </summary>
        private void LoadLastEditedGallery()
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"SELECT exhibition_id, title, created_at, cover_image_data 
                      FROM exhibitions 
                      WHERE user_id = @uid 
                      ORDER BY created_at DESC 
                      LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@uid", currentUserId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            borderLastGallery.Visibility = Visibility.Visible;
                            txtLastGalleryTitle.Text = reader.IsDBNull(1) ? "Без названия" : reader.GetString(1);
                            txtLastGalleryDate.Text = reader.GetDateTime(2).ToString("dd.MM.yyyy");
                            if (!reader.IsDBNull(3))
                            {
                                var bytes = (byte[])reader[3];
                                imgLastGalleryCover.Source = ByteArrayToBitmapImage(bytes);
                            }
                            else
                            {
                                imgLastGalleryCover.Source = null;
                            }
                        }
                        else
                        {
                            borderLastGallery.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Загружает последние 10 отзывов (новые сверху) для галерей текущего пользователя.
        /// </summary>
        private void LoadReviews()
        {
            var reviews = new List<ReviewItem>();
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"SELECT r.reviewer_name, r.comment, r.rating, r.created_at, e.title 
                      FROM reviews r 
                      JOIN exhibitions e ON r.exhibition_id = e.exhibition_id 
                      WHERE e.user_id = @userId AND r.exhibition_id IS NOT NULL
                      ORDER BY r.created_at DESC 
                      LIMIT 10", conn))
                {
                    cmd.Parameters.AddWithValue("@userId", currentUserId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string reviewer = reader.IsDBNull(0) || string.IsNullOrEmpty(reader.GetString(0))
                                              ? "—"
                                              : reader.GetString(0);
                            string comment = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            int rating = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            string exhibitionTitle = reader.IsDBNull(4) ? "Без названия" : reader.GetString(4);

                            reviews.Add(new ReviewItem
                            {
                                Reviewer = reviewer,
                                Comment = comment,
                                Rating = rating,
                                ExhibitionTitle = exhibitionTitle,
                                Stars = string.Concat(Enumerable.Repeat("★", Math.Max(0, Math.Min(rating, 5))))
                            });
                        }
                    }
                }
            }

            ReviewsList.ItemsSource = reviews
                .Select(r => new
                {
                    r.Reviewer,
                    r.Comment,
                    r.ExhibitionTitle,
                    Stars = r.Stars
                        .Select(ch => ch.ToString())
                        .ToArray()
                })
                .ToList();
        }

        private BitmapImage ByteArrayToBitmapImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;
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

        // DTO для отзывов
        private class ReviewItem
        {
            public string Reviewer { get; set; }
            public string Comment { get; set; }
            public int Rating { get; set; }
            public string ExhibitionTitle { get; set; }
            public string Stars { get; set; }
        }
    }
}
