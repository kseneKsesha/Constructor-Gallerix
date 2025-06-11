using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Constructor_Gallerix.Models;
using Constructor_Gallerix.Repository;
using Constructor_Gallerix.View; // AudioGuideService
using Npgsql;

namespace Constructor_Gallerix.View
{
    public partial class GuestMainView : Window
    {
        private readonly string connectionString =
            "Host=localhost;Port=5432;Username=postgres;Password=1234;Database=GallerixDB";

        // Локальный список загруженных галерей
        private List<GalleryItem> allGalleries = new List<GalleryItem>();

        // Флаг состояния аудиогида
        private bool isAudioEnabled = false;

        // Плейсхолдер для TextBox
        private const string SearchPlaceholder = "Поиск по названию галереи или картине...";

        public GuestMainView()
        {
            InitializeComponent();

            // Изначально аудиогид выключён
            isAudioEnabled = false;
            Application.Current.Properties["IsAudioGuideEnabled"] = false;

            // Сразу подгружаем все публичные галереи без фильтра
            LoadPublicGalleries();
        }

        /// <summary>
        /// Обработчик клика по кнопке аудиогида: переключаем состояние и меняем текст.
        /// </summary>
        private void BtnAudioGuideToggle_Click(object sender, RoutedEventArgs e)
        {
            isAudioEnabled = !isAudioEnabled;
            Application.Current.Properties["IsAudioGuideEnabled"] = isAudioEnabled;

            if (isAudioEnabled)
            {
                btnAudioGuideToggle.Content = "Отключить аудиогид";

                MessageBox.Show(
                    "Пожалуйста, убедитесь, что ваш звук включён.",
                    "Аудиогид",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                AudioGuideService.Speak("Выберите выставку, которую вы хотите посмотреть.");
            }
            else
            {
                btnAudioGuideToggle.Content = "Включить аудиогид";
                AudioGuideService.Stop();
            }
        }

        /// <summary>
        /// Кнопка «Найти»: подгружает галереи с фильтром по тексту поиска.
        /// </summary>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string raw = txtSearch.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(raw) || raw == SearchPlaceholder)
            {
                // Если пусто или плейсхолдер — показываем все
                LoadPublicGalleries();
            }
            else
            {
                // Иначе фильтруем по введённой строке (нижний регистр)
                LoadPublicGalleries(raw.ToLower());
            }
        }

        /// <summary>
        /// Кнопка «Показать все галереи»: сбрасывает текст в TextBox и выводит все.
        /// </summary>
        private void ShowAllButton_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = SearchPlaceholder;
            LoadPublicGalleries();
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == SearchPlaceholder)
                txtSearch.Text = "";
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                txtSearch.Text = SearchPlaceholder;
        }

        /// <summary>
        /// Загружает публичные галереи из БД.
        /// Если передан непустой searchQuery, то фильтрует по названиям/описаниям галерей и картин.
        /// </summary>
        private void LoadPublicGalleries(string searchQuery = null)
        {
            allGalleries.Clear();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                // Базовый SQL-запрос
                string sql =
                    @"SELECT e.exhibition_id, 
                             e.title, 
                             e.description, 
                             u.username, 
                             u.user_id, 
                             e.created_at, 
                             e.cover_image_data, 
                             COALESCE(AVG(r.rating), 0) as avg_rating
                      FROM exhibitions e
                      LEFT JOIN users u ON e.user_id = u.user_id
                      LEFT JOIN reviews r ON r.exhibition_id = e.exhibition_id
                      WHERE e.is_public = true";

                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    sql += @"
                        AND (
                            LOWER(e.title) LIKE @query 
                            OR LOWER(e.description) LIKE @query
                            OR EXISTS (
                                SELECT 1 
                                FROM pictures p 
                                WHERE p.exhibition_id = e.exhibition_id
                                  AND LOWER(p.title) LIKE @query
                            )
                        )";
                }

                sql += @"
                      GROUP BY e.exhibition_id, u.username, u.user_id, e.title, e.description, e.created_at, e.cover_image_data
                      ORDER BY e.created_at DESC";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(searchQuery))
                    {
                        cmd.Parameters.AddWithValue("@query", "%" + searchQuery.Trim() + "%");
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new GalleryItem
                            {
                                ExhibitionId = reader.GetInt32(0),
                                Title = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Author = reader.IsDBNull(3) ? "—" : reader.GetString(3),
                                AuthorId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                                CreatedAt = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5),
                                CoverImage = reader.IsDBNull(6)
                                             ? null
                                             : ByteArrayToBitmapImage((byte[])reader[6]),
                                AvgRating = Math.Round(
                                    reader.IsDBNull(7) ? 0.0 : reader.GetDouble(7), 1)
                            };

                            allGalleries.Add(item);
                        }
                    }
                }
            }

            GalleriesList.ItemsSource = null;
            GalleriesList.ItemsSource = allGalleries;
        }

        private void AuthorProfile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int authorId && authorId > 0)
            {
                var author = allGalleries.FirstOrDefault(x => x.AuthorId == authorId);
                if (author != null)
                {
                    var guestProfileView = new GuestProfileView(author.Author);
                    guestProfileView.ShowDialog();
                }
            }
        }

        private void ReviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int exhibitionId)
            {
                var reviewView = new GuestReviewView(exhibitionId)
                {
                    Owner = this
                };
                reviewView.ShowDialog();

                // После закрытия окна отзыва обновляем список (с учётом текущей строки поиска)
                string raw = txtSearch.Text?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(raw) || raw == SearchPlaceholder)
                    LoadPublicGalleries();
                else
                    LoadPublicGalleries(raw.ToLower());
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
                    Opacity = 0.5
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

        /// <summary>
        /// При клике на карточку галереи: открываем ExhibitionView.
        /// Озвучка названия/описания выполняется уже в Loaded самого ExhibitionView.
        /// </summary>
        private void GalleryCard_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            if (sender is Border border && border.DataContext is GalleryItem item)
            {
                using (var repo = new ExhibitionRepository(connectionString))
                {
                    var exhibition = repo.GetExhibitionWithPictures(item.ExhibitionId);
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
                        exhibitionView.Owner = this;
                        exhibitionView.ShowDialog();
                    }
                }
            }
        }

        /// <summary>
        /// Закрывает приложение.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Преобразует byte[] в BitmapImage.
        /// </summary>
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

        /// <summary>
        /// Класс представления одной карточки галереи.
        /// </summary>
        private class GalleryItem
        {
            public int ExhibitionId { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Author { get; set; }
            public int AuthorId { get; set; }
            public DateTime CreatedAt { get; set; }
            public BitmapImage CoverImage { get; set; }
            public double AvgRating { get; set; }
        }
    }
}
