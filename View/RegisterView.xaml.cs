using Npgsql;
using System;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Constructor_Gallerix.View;

namespace Constructor_Gallerix.View
{
    public partial class RegisterView : Window
    {
        private readonly string connectionString = "Host=localhost;Username=postgres;Password=1234;Database=GallerixDB";
        private bool _isPasswordVisible = false;
        private bool _isConfirmPasswordVisible = false;

        public RegisterView()
        {
            InitializeComponent();
            Loaded += RegisterView_Loaded;
        }

        private async void RegisterView_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckDatabaseConnectionAsync();
        }

        private async Task CheckDatabaseConnectionAsync()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginView loginView = new LoginView();
            loginView.Show();
            Close();
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (txtPasswordVisible.Visibility == Visibility.Visible)
            {
                txtPasswordVisible.Text = txtPassword.Password;
            }
        }

        private void txtConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (txtConfirmPasswordVisible.Visibility == Visibility.Visible)
            {
                txtConfirmPasswordVisible.Text = txtConfirmPassword.Password;
            }
        }

        private async void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            btnRegister.IsEnabled = false;

            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = _isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;
            string confirmPassword = _isConfirmPasswordVisible ? txtConfirmPasswordVisible.Text : txtConfirmPassword.Password;

            if (!ValidateInput(username, email, password, confirmPassword))
            {
                btnRegister.IsEnabled = true;
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    if (await AuthService.UserExistsAsync(conn, username, email))
                    {
                        ShowError("Пользователь с таким именем или почтой уже зарегистрирован.");
                        return;
                    }

                    string passwordHash = AuthService.HashPassword(password);

                    const string insertQuery = @"INSERT INTO users (username, password_hash, email) 
                                                        VALUES (@username, @passwordHash, @email);";


                    using (var cmd = new NpgsqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
                        cmd.Parameters.AddWithValue("@email", email);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    MessageBox.Show("Регистрация прошла успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    new LoginView().Show();
                    Close();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Произошла ошибка:\n{ex.Message}");
            }
            finally
            {
                btnRegister.IsEnabled = true;
            }
        }

        private bool ValidateInput(string username, string email, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 4)
            {
                ShowError("Имя пользователя должно содержать минимум 4 символа.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ShowError("Введите корректный email.");
                return false;
            }

            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                ShowError("Пароль должен содержать минимум 8 символов.");
                return false;
            }

            if (password != confirmPassword)
            {
                ShowError("Пароли не совпадают.");
                return false;
            }

            return true;
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void btnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                imgEye1.Source = new BitmapImage(new Uri("/Images/eye-open.png", UriKind.Relative));
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                imgEye1.Source = new BitmapImage(new Uri("/Images/eye-closed.png", UriKind.Relative));
            }
        }

        private void btnToggleConfirmPassword_Click(object sender, RoutedEventArgs e)
        {
            _isConfirmPasswordVisible = !_isConfirmPasswordVisible;

            if (_isConfirmPasswordVisible)
            {
                txtConfirmPasswordVisible.Text = txtConfirmPassword.Password;
                txtConfirmPassword.Visibility = Visibility.Collapsed;
                txtConfirmPasswordVisible.Visibility = Visibility.Visible;
                imgEye2.Source = new BitmapImage(new Uri("/Images/eye-open.png", UriKind.Relative));
            }
            else
            {
                txtConfirmPassword.Password = txtConfirmPasswordVisible.Text;
                txtConfirmPassword.Visibility = Visibility.Visible;
                txtConfirmPasswordVisible.Visibility = Visibility.Collapsed;
                imgEye2.Source = new BitmapImage(new Uri("/Images/eye-closed.png", UriKind.Relative));
            }
        }

        private void Field_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender == txtUsername)
                imgUsername.Source = new BitmapImage(new Uri("/Images/user-icon-pink.png", UriKind.Relative));
            else if (sender == txtEmail)
                imgEmail.Source = new BitmapImage(new Uri("/Images/email-icon-pink.png", UriKind.Relative));
            else if (sender == txtPassword || sender == txtPasswordVisible)
                imgPassword.Source = new BitmapImage(new Uri("/Images/key-icon-pink.png", UriKind.Relative));
            else if (sender == txtConfirmPassword || sender == txtConfirmPasswordVisible)
                imgConfirmPassword.Source = new BitmapImage(new Uri("/Images/key-icon-pink.png", UriKind.Relative));
        }

        private void Field_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender == txtUsername)
                imgUsername.Source = new BitmapImage(new Uri("/Images/user-icon.png", UriKind.Relative));
            else if (sender == txtEmail)
                imgEmail.Source = new BitmapImage(new Uri("/Images/email-icon.png", UriKind.Relative));
            else if (sender == txtPassword || sender == txtPasswordVisible)
                imgPassword.Source = new BitmapImage(new Uri("/Images/key-icon.png", UriKind.Relative));
            else if (sender == txtConfirmPassword || sender == txtConfirmPasswordVisible)
                imgConfirmPassword.Source = new BitmapImage(new Uri("/Images/key-icon.png", UriKind.Relative));
        }
    }
}
