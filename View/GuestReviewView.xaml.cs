using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Npgsql;

namespace Constructor_Gallerix.View
{
    public partial class GuestReviewView : Window
    {
        private int galleryRating = 0;
        private int appRating = 0;
        private readonly int exhibitionId;
        private const string ConnectionString =
            "Host=localhost;Username=postgres;Password=1234;Database=GallerixDB";

        public GuestReviewView(int exhibitionId)
        {
            InitializeComponent();
            this.exhibitionId = exhibitionId;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // --- Звёздочки для оценки галереи ---
        private void GalleryStar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag.ToString(), out int value))
            {
                galleryRating = value;
                SetStars(new[] { gstar1, gstar2, gstar3, gstar4, gstar5 }, galleryRating);
            }
        }

        // --- Звёздочки для оценки приложения ---
        private void AppStar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag.ToString(), out int value))
            {
                appRating = value;
                SetStars(new[] { astar1, astar2, astar3, astar4, astar5 }, appRating);
            }
        }

        private void SetStars(Button[] starButtons, int value)
        {
            for (int i = 0; i < starButtons.Length; i++)
            {
                if (starButtons[i].Content is TextBlock tb)
                {
                    tb.Foreground = i < value
                        ? new SolidColorBrush(Color.FromRgb(255, 180, 0))   // жёлтая звезда
                        : new SolidColorBrush(Color.FromRgb(102, 102, 102)); // серая
                }
            }
        }

        // --- Кнопка «Отправить отзывы» ---
        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            string reviewer = txtReviewerName.Text.Trim();
            bool gallerySent = false;
            bool appSent = false;

            // 1) Отзыв на галерею
            string galleryComment = txtGalleryComment.Text.Trim();
            if (galleryRating > 0 || !string.IsNullOrWhiteSpace(galleryComment))
            {
                // Проверяем «запрещённые слова» и в имени, и в комментарии:
                if (ForbiddenWordsService.ContainsForbiddenWords(galleryComment)
                    || ForbiddenWordsService.ContainsForbiddenWords(reviewer))
                {
                    MessageBox.Show(
                        "В отзыве на галерею содержатся запрещённые слова. Пожалуйста, измените текст.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (var conn = new NpgsqlConnection(ConnectionString))
                    {
                        conn.Open();
                        string sql = @"
                            INSERT INTO reviews
                                (exhibition_id, reviewer_name, comment, rating, review_type, created_at)
                            VALUES
                                (@eid, @name, @comment, @rating, 'gallery', NOW())";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@eid", exhibitionId);
                            cmd.Parameters.AddWithValue("@name",
                                string.IsNullOrWhiteSpace(reviewer) ? (object)DBNull.Value : reviewer);
                            cmd.Parameters.AddWithValue("@comment",
                                string.IsNullOrWhiteSpace(galleryComment) ? (object)DBNull.Value : galleryComment);

                            // Если галерея не оценена (rating = 0), вставляем NULL
                            if (galleryRating > 0)
                                cmd.Parameters.AddWithValue("@rating", galleryRating);
                            else
                                cmd.Parameters.AddWithValue("@rating", DBNull.Value);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    gallerySent = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Ошибка при сохранении отзыва на галерею:\n" + ex.Message,
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // 2) Отзыв на приложение
            string appComment = txtAppComment.Text.Trim();
            if (appRating > 0 || !string.IsNullOrWhiteSpace(appComment))
            {
                // Проверка «запрещённых слов»
                if (ForbiddenWordsService.ContainsForbiddenWords(appComment)
                    || ForbiddenWordsService.ContainsForbiddenWords(reviewer))
                {
                    MessageBox.Show(
                        "В отзыве на приложение содержатся запрещённые слова. Пожалуйста, измените текст.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (var conn = new NpgsqlConnection(ConnectionString))
                    {
                        conn.Open();
                        string sql = @"
                            INSERT INTO reviews
                                (reviewer_name, comment, rating, review_type, created_at)
                            VALUES
                                (@name, @comment, @rating, 'app', NOW())";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@name",
                                string.IsNullOrWhiteSpace(reviewer) ? (object)DBNull.Value : reviewer);
                            cmd.Parameters.AddWithValue("@comment",
                                string.IsNullOrWhiteSpace(appComment) ? (object)DBNull.Value : appComment);

                            // Если приложение не оценено (rating = 0), вставляем NULL
                            if (appRating > 0)
                                cmd.Parameters.AddWithValue("@rating", appRating);
                            else
                                cmd.Parameters.AddWithValue("@rating", DBNull.Value);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    appSent = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Ошибка при сохранении отзыва на приложение:\n" + ex.Message,
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Если ни один отзыв не отправлен, предупреждаем
            if (!gallerySent && !appSent)
            {
                MessageBox.Show(
                    "Невозможно отправить пустой отзыв. Пожалуйста, заполните хотя бы один раздел.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Сообщаем об успешном сохранении
            string successMsg = "";
            if (gallerySent && appSent)
                successMsg = "Отзывы на галерею и приложение успешно сохранены!";
            else if (gallerySent)
                successMsg = "Отзыв на галерею успешно сохранён!";
            else if (appSent)
                successMsg = "Отзыв на приложение успешно сохранён!";

            MessageBox.Show(successMsg, "Спасибо", MessageBoxButton.OK, MessageBoxImage.Information);

            // Закрываем окно, чтобы на главном экране (GuestMainView) можно было обновить данные
            this.Close();
        }
    }
}
