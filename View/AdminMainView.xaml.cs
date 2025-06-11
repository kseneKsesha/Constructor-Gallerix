using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Npgsql;
using Constructor_Gallerix.Models;
using Constructor_Gallerix.Repository;

namespace Constructor_Gallerix.View
{
    public partial class AdminMainView : Window
    {
        // Строка подключения к базе данных PostgreSQL
        private readonly string connectionString =
            "Host=localhost;Port=5432;Username=postgres;Password=1234;Database=GallerixDB";

        // Абсолютный путь к forbidden_words.txt (рядом с EXE)
        private readonly string forbiddenWordsFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "forbidden_words.txt");

        public AdminMainView()
        {
            InitializeComponent();
            ShowUsersTab();
        }

        // --- Переключение вкладок ---
        private void btnTabUsers_Click(object sender, RoutedEventArgs e) => ShowUsersTab();
        private void btnTabGalleries_Click(object sender, RoutedEventArgs e) => ShowGalleriesTab();
        private void btnTabComments_Click(object sender, RoutedEventArgs e) => ShowCommentsTab();
        private void btnTabCensorship_Click(object sender, RoutedEventArgs e) => ShowCensorshipTab();

        private void ShowUsersTab()
        {
            UsersPanel.Visibility = Visibility.Visible;
            GalleriesPanel.Visibility = Visibility.Collapsed;
            CommentsPanel.Visibility = Visibility.Collapsed;
            CensorshipPanel.Visibility = Visibility.Collapsed;
            SetActiveTab(btnTabUsers);
            LoadUsers();
        }

        private void ShowGalleriesTab()
        {
            UsersPanel.Visibility = Visibility.Collapsed;
            GalleriesPanel.Visibility = Visibility.Visible;
            CommentsPanel.Visibility = Visibility.Collapsed;
            CensorshipPanel.Visibility = Visibility.Collapsed;
            SetActiveTab(btnTabGalleries);
            LoadGalleries();
        }

        private void ShowCommentsTab()
        {
            UsersPanel.Visibility = Visibility.Collapsed;
            GalleriesPanel.Visibility = Visibility.Collapsed;
            CommentsPanel.Visibility = Visibility.Visible;
            CensorshipPanel.Visibility = Visibility.Collapsed;
            SetActiveTab(btnTabComments);
            LoadComments();
        }

        private void ShowCensorshipTab()
        {
            UsersPanel.Visibility = Visibility.Collapsed;
            GalleriesPanel.Visibility = Visibility.Collapsed;
            CommentsPanel.Visibility = Visibility.Collapsed;
            CensorshipPanel.Visibility = Visibility.Visible;
            SetActiveTab(btnTabCensorship);
            LoadForbiddenWords();
        }

        // --- Выделяем активную кнопку в меню вкладок ---
        private void SetActiveTab(Button activeButton)
        {
            var mainStyle = (Style)FindResource("MainButtonStyle");
            var secondaryStyle = (Style)FindResource("SecondaryButtonStyle");

            btnTabUsers.Style = secondaryStyle;
            btnTabGalleries.Style = secondaryStyle;
            btnTabComments.Style = secondaryStyle;
            btnTabCensorship.Style = secondaryStyle;

            activeButton.Style = mainStyle;
        }

        // --- Загрузка списка пользователей ---
        private void LoadUsers()
        {
            var users = new List<UserItem>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"SELECT user_id, username, full_name, email, bio, created_at 
                      FROM users 
                      ORDER BY user_id", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new UserItem
                        {
                            UserId = reader.GetInt32(0),
                            Username = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            FullName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Bio = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            CreatedAt = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5)
                        });
                    }
                }
            }

            UsersList.ItemsSource = users;
        }

        // --- Удаление пользователя (и всех связанных с ним галерей/картин/отзывов) ---
        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int userId)
            {
                var result = MessageBox.Show(
                    "Удалить пользователя и все его галереи?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand(@"
                            DELETE FROM reviews 
                             WHERE exhibition_id IN 
                               (SELECT exhibition_id 
                                  FROM exhibitions 
                                 WHERE user_id = @uid);
                            DELETE FROM pictures 
                             WHERE exhibition_id IN 
                               (SELECT exhibition_id 
                                  FROM exhibitions 
                                 WHERE user_id = @uid);
                            DELETE FROM exhibitions 
                             WHERE user_id = @uid;
                            DELETE FROM users 
                             WHERE user_id = @uid;", conn))
                        {
                            cmd.Parameters.AddWithValue("@uid", userId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    LoadUsers();
                    LoadGalleries();
                }
            }
        }

        // --- Загрузка списка галерей ---
        private void LoadGalleries()
        {
            var galleries = new List<GalleryItem>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"SELECT e.exhibition_id, e.title, u.username, e.created_at, e.cover_image_data,
                             COALESCE(AVG(r.rating), 0) as avg_rating
                      FROM exhibitions e
                      LEFT JOIN users u ON e.user_id = u.user_id
                      LEFT JOIN reviews r ON r.exhibition_id = e.exhibition_id
                      GROUP BY e.exhibition_id, u.username, e.title, e.created_at, e.cover_image_data
                      ORDER BY e.created_at DESC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        galleries.Add(new GalleryItem
                        {
                            ExhibitionId = reader.GetInt32(0),
                            Title = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Author = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            CreatedAt = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                            CoverImage = reader.IsDBNull(4)
                                             ? null
                                             : ByteArrayToBitmapImage((byte[])reader[4]),
                            AvgRating = Math.Round(reader.IsDBNull(5) ? 0.0 : reader.GetDouble(5), 1)
                        });
                    }
                }
            }

            GalleriesList.ItemsSource = galleries;
        }

        // --- При клике «Открыть галерею» (ExhibitionView) ---
        private void OpenGallery_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is int exhibitionId)
            {
                using (var repo = new ExhibitionRepository(connectionString))
                {
                    var exhibition = repo.GetExhibitionWithPictures(exhibitionId);
                    if (exhibition != null)
                    {
                        BitmapImage coverImage = null;
                        if (exhibition.CoverImageData != null && exhibition.CoverImageData.Length > 0)
                        {
                            using (var ms = new MemoryStream(exhibition.CoverImageData))
                            {
                                coverImage = new BitmapImage();
                                coverImage.BeginInit();
                                coverImage.CacheOption = BitmapCacheOption.OnLoad;
                                coverImage.StreamSource = ms;
                                coverImage.EndInit();
                                coverImage.Freeze();
                            }
                        }

                        var exhibitionView = new ExhibitionView(
                            exhibition.Title ?? "",
                            exhibition.Description ?? "",
                            exhibition.Template ?? "",
                            exhibition.Pictures ?? new List<Picture>(),
                            coverImage
                        );
                        exhibitionView.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось загрузить галерею.",
                                        "Ошибка",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                    }
                }
            }
        }

        private void GalleryCard_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(218, 52, 174));
                border.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = System.Windows.Media.Color.FromRgb(218, 52, 174),
                    BlurRadius = 20,
                    ShadowDepth = 0,
                    Opacity = 0.55
                };
            }
        }

        private void GalleryCard_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = System.Windows.Media.Brushes.Transparent;
                border.Effect = null;
            }
        }

        // --- Удаление галереи ---
        private void DeleteGallery_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int exhibitionId)
            {
                var result = MessageBox.Show(
                    "Удалить галерею?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand(@"
                            DELETE FROM reviews WHERE exhibition_id = @eid;
                            DELETE FROM pictures WHERE exhibition_id = @eid;
                            DELETE FROM exhibitions WHERE exhibition_id = @eid;", conn))
                        {
                            cmd.Parameters.AddWithValue("@eid", exhibitionId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LoadGalleries();
                }
            }
        }

        // --- Загрузка списка комментариев ---
        private void LoadComments()
        {
            var comments = new List<CommentItem>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"SELECT r.review_id, r.reviewer_name, r.comment, r.created_at, e.title
                      FROM reviews r
                      LEFT JOIN exhibitions e ON r.exhibition_id = e.exhibition_id
                      ORDER BY r.created_at DESC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        comments.Add(new CommentItem
                        {
                            ReviewId = reader.GetInt32(0),
                            Reviewer = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Comment = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            CreatedAt = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                            ExhibitionTitle = reader.IsDBNull(4) ? "" : reader.GetString(4)
                        });
                    }
                }
            }

            CommentsList.ItemsSource = comments;
        }

        // --- Удаление комментария ---
        private void DeleteComment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int reviewId)
            {
                var result = MessageBox.Show(
                    "Удалить комментарий?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("DELETE FROM reviews WHERE review_id = @rid", conn))
                        {
                            cmd.Parameters.AddWithValue("@rid", reviewId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LoadComments();
                }
            }
        }

        // --- Загрузка списка запрещённых слов ---
        private void LoadForbiddenWords()
        {
            if (File.Exists(forbiddenWordsFilePath))
            {
                txtForbiddenWords.Text = string.Join(
                    Environment.NewLine,
                    File.ReadAllLines(forbiddenWordsFilePath));
            }
            else
            {
                txtForbiddenWords.Text = string.Empty;
            }
        }

        /// <summary>
        /// Обработчик кнопки «Сохранить и обновить» в разделе Цензуры.
        /// Записывает содержимое txtForbiddenWords в файл и сразу перезагружает ForbiddenWordsService.
        /// </summary>
        private void btnSaveAndReloadForbiddenWords_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Разбиваем текст на строки, удаляем пустые, приводим к нижнему регистру, убираем дубликаты
                var lines = txtForbiddenWords.Text
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim().ToLower())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct();

                // Записываем в файл forbidden_words.txt
                File.WriteAllLines(forbiddenWordsFilePath, lines);

                // Обновляем поле (чтобы убрать лишние пробелы, дубли и т. п.)
                txtForbiddenWords.Text = string.Join(Environment.NewLine, lines);

                // Перезагружаем «в памяти» список через сервис
                ForbiddenWordsService.Reload();

                MessageBox.Show("Список запрещённых слов сохранён и обновлён.",
                                "Успешно",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении списка: " + ex.Message,
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        // --- Конвертация byte[] → BitmapImage (для обложек галерей) ---
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

        // --- Кнопки «Свернуть» / «Закрыть окно» ---
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void btnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        // === Вспомогательные внутренние DTO-классы для биндинга ===

        private class UserItem
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Bio { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private class GalleryItem
        {
            public int ExhibitionId { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public DateTime CreatedAt { get; set; }
            public BitmapImage CoverImage { get; set; }
            public double AvgRating { get; set; }
        }

        private class CommentItem
        {
            public int ReviewId { get; set; }
            public string Reviewer { get; set; }
            public string Comment { get; set; }
            public DateTime CreatedAt { get; set; }
            public string ExhibitionTitle { get; set; }
        }
    }
}
