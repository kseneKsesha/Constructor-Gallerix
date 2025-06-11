using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Npgsql;
using Constructor_Gallerix.Repository;

namespace Constructor_Gallerix.View
{
    public partial class ProfileView : Window
    {
        private readonly int userId;
        private readonly string username;
        private byte[] avatarBytes;
        private bool isEditMode = false;

        // Строка подключения к вашей базе
        private readonly string connectionString =
            "Host=localhost;Port=5432;Username=postgres;Password=1234;Database=GallerixDB";

        public ProfileView(int userId, string username)
        {
            InitializeComponent();

            this.userId = userId;
            this.username = username;

            txtMenuUsername.Text = username;
            LoadUserProfile();
        }

        /// <summary>
        /// Загружает данные пользователя из БД и заполняет поля.
        /// </summary>
        private void LoadUserProfile()
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"SELECT username, 
                             full_name, 
                             email, 
                             bio, 
                             created_at, 
                             avatar 
                      FROM users 
                      WHERE user_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtUsername.Text = reader.GetString(0);
                            txtFullName.Text = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            txtEmail.Text = reader.GetString(2);
                            txtBio.Text = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            txtCreatedAt.Text = reader.GetDateTime(4).ToString("dd.MM.yyyy");
                            avatarBytes = reader.IsDBNull(5) ? null : (byte[])reader[5];

                            if (avatarBytes != null && avatarBytes.Length > 0)
                            {
                                imgAvatar.Source = ByteArrayToBitmapImage(avatarBytes);
                                txtAvatarPlus.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                imgAvatar.Source = null;
                                txtAvatarPlus.Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
            }

            // Загрузка избранных галерей
            lstFavorites.Items.Clear();
            using (var repo = new ExhibitionRepository(connectionString))
            {
                var favs = repo.GetUserExhibitions(userId)
                               .Where(x => x.IsFavorite)
                               .Select(x => x.Title)
                               .ToList();

                foreach (var title in favs)
                {
                    lstFavorites.Items.Add(title);
                }
            }
        }

        /// <summary>
        /// Обработчик нажания «Редактировать» — включает режим редактирования.
        /// </summary>
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            isEditMode = true;

            txtFullName.IsReadOnly = false;
            txtEmail.IsReadOnly = false;
            txtBio.IsReadOnly = false;

            btnSave.IsEnabled = true;

            // Меняем фон, чтобы стало видно, что поля доступны для редактирования
            var editBackground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(58, 58, 111));
            txtFullName.Background = editBackground;
            txtEmail.Background = editBackground;
            txtBio.Background = editBackground;
        }

        /// <summary>
        /// Обработчик нажания «Сохранить» — сохраняет изменения в БД.
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        @"UPDATE users
                          SET full_name = @name,
                              email     = @mail,
                              bio       = @bio,
                              avatar    = @avatar
                          WHERE user_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@name", txtFullName.Text);
                        cmd.Parameters.AddWithValue("@mail", txtEmail.Text);
                        cmd.Parameters.AddWithValue("@bio",
                            string.IsNullOrWhiteSpace(txtBio.Text)
                                ? (object)DBNull.Value
                                : txtBio.Text);
                        cmd.Parameters.AddWithValue("@avatar",
                            avatarBytes ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@id", userId);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Данные профиля успешно обновлены!",
                                "Успех",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                // Выключаем режим редактирования
                isEditMode = false;
                txtFullName.IsReadOnly = true;
                txtEmail.IsReadOnly = true;
                txtBio.IsReadOnly = true;
                btnSave.IsEnabled = false;

                // Возвращаем фон полей в исходное состояние
                var normalBackground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(42, 42, 77));
                txtFullName.Background = normalBackground;
                txtEmail.Background = normalBackground;
                txtBio.Background = normalBackground;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message,
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Конвертирует массив байт в BitmapImage для отображения аватара.
        /// </summary>
        private BitmapImage ByteArrayToBitmapImage(byte[] bytes)
        {
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
        /// Обработчик загрузки нового аватара (крутит диалог выбора файла).
        /// </summary>
        private void UploadAvatar_Click(object sender, RoutedEventArgs e)
        {
            if (!isEditMode) return;

            var dlg = new OpenFileDialog
            {
                Filter = "Изображения (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Выберите аватар"
            };

            if (dlg.ShowDialog() == true)
            {
                avatarBytes = File.ReadAllBytes(dlg.FileName);
                imgAvatar.Source = ByteArrayToBitmapImage(avatarBytes);
                txtAvatarPlus.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Подсветка бордера текстового элемента при фокусе.
        /// </summary>
        private void Field_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is Control c)
                c.BorderBrush = System.Windows.Media.Brushes.DeepPink;
        }

        /// <summary>
        /// Возвращение бордера в исходный цвет при потере фокуса.
        /// </summary>
        private void Field_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is Control c)
                c.BorderBrush = System.Windows.Media.Brushes.DarkGray;
        }

        // Системные кнопки:
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void btnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        // --- Навигация (верхнее меню) ---
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            var mainView = new MainView(username, userId);
            mainView.Show();
            this.Close();
        }

        private void ConstructorButton_Click(object sender, RoutedEventArgs e)
        {
            var constructorView = new ConstructorView(userId, username, 0);
            constructorView.Show();
            this.Close();
        }

        private void MyWorksButton_Click(object sender, RoutedEventArgs e)
        {
            var myWorksView = new MyWorksView(userId, username);
            myWorksView.Show();
            this.Close();
        }

        private void DemoButton_Click(object sender, RoutedEventArgs e)
        {
            var demoView = new DemoView(userId, username);
            demoView.Show();
            this.Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var help = new HelpView(username, userId);
            help.Owner = this;
            help.Show();
            // **Профиль остаётся открытым**
        }
    }
}
