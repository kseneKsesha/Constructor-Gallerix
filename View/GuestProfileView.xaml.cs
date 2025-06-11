using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Npgsql;

namespace Constructor_Gallerix.View
{
    public partial class GuestProfileView : Window
    {
        private const string ConnectionString = "Host=localhost;Username=postgres;Password=1234;Database=GallerixDB";

        public GuestProfileView(string username)
        {
            InitializeComponent();
            LoadUserProfile(username);
        }

        private void LoadUserProfile(string username)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"SELECT username, full_name, bio, created_at, avatar 
                      FROM users WHERE username = @uname", conn))
                {
                    cmd.Parameters.AddWithValue("@uname", username);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtUsername.Text = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            txtFullName.Text = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            txtBio.Text = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            txtCreatedAt.Text = reader.IsDBNull(3)
                                ? ""
                                : reader.GetDateTime(3).ToString("dd.MM.yyyy");

                            if (!reader.IsDBNull(4))
                            {
                                var bytes = (byte[])reader[4];
                                // Устанавливаем ImageSource внутрь ImageBrush:
                                var bmp = ByteArrayToBitmapImage(bytes);
                                if (bmp != null)
                                {
                                    imgAvatarBrush.ImageSource = bmp;
                                }
                            }
                        }
                        else
                        {
                            // Если не нашли в базе, по-умолчанию выводим имя, а остальное — пусто:
                            txtUsername.Text = username;
                            txtFullName.Text = "—";
                            txtBio.Text = "";
                            txtCreatedAt.Text = "";
                        }
                    }
                }
            }
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

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
