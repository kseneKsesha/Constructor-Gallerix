// File: AppReviewView.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Npgsql;

namespace Constructor_Gallerix.View
{
    public partial class AppReviewView : Window
    {
        private int rating = 0;
        private const string ConnectionString ="Host=localhost;Username=postgres;Password=1234;Database=GallerixDB";

        public AppReviewView(int userId)
        {
            InitializeComponent();
            // userId не используется внутри этой формы, но может быть сохранён, если потребуется
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Star_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag.ToString(), out int value))
            {
                rating = value;
                SetStars(rating);
            }
        }

        private void SetStars(int starCount)
        {
            var stars = new[] { star1, star2, star3, star4, star5 };
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i].Content is TextBlock tb)
                {
                    tb.Foreground = i < starCount
                        ? new SolidColorBrush(Color.FromRgb(255, 180, 0))   // жёлтая звезда
                        : new SolidColorBrush(Color.FromRgb(102, 102, 102)); // серая
                }
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (rating == 0)
            {
                MessageBox.Show("Поставьте хотя бы одну звезду!",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string comment = txtComment.Text.Trim();

            // Проверяем комментарий на запрещённые слова
            if (ForbiddenWordsService.ContainsForbiddenWords(comment))
            {
                MessageBox.Show(
                    "В отзыве содержатся запрещённые слова. Пожалуйста, измените текст.",
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
                            (NULL, @comment, @rating, 'app', NOW())";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@comment",
                            string.IsNullOrWhiteSpace(comment) ? (object)DBNull.Value : comment);
                        cmd.Parameters.AddWithValue("@rating", rating);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Спасибо за отзыв!",
                                "Спасибо", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при сохранении отзыва.\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
