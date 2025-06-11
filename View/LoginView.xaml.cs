using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Constructor_Gallerix.View
{
    public partial class LoginView : Window
    {
        private bool _isPasswordVisible = false;

        public LoginView()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            var registerView = new RegisterView();
            registerView.Show();
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = _isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Вход как администратор
            if (username == "admin" && password == "admin12345")
            {
                MessageBox.Show("Вход администратора!", "Администратор", MessageBoxButton.OK, MessageBoxImage.Information);

                var adminView = new AdminMainView();
                adminView.Show();
                this.Close();
                return;
            }

            // Обычная авторизация пользователя
            bool isLoginSuccessful = AuthService.Login(username, password);

            if (isLoginSuccessful)
            {
                int userId = AuthService.GetUserId(username);

                if (userId == -1)
                {
                    MessageBox.Show("Не удалось определить ID пользователя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show("Успешный вход!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                var mainView = new MainView(username, userId);
                mainView.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnGuest_Click(object sender, RoutedEventArgs e)
        {
            // Открываем отдельную форму для гостя
            var guestView = new GuestMainView(); // <-- реализуй GuestMainView (или как назовёшь)
            guestView.Show();
            this.Close();
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

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isPasswordVisible)
                txtPasswordVisible.Text = txtPassword.Password;
        }

        private void Field_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender == txtUsername)
                imgUsername.Source = new BitmapImage(new Uri("/Images/user-icon-pink.png", UriKind.Relative));
            else if (sender == txtPassword || sender == txtPasswordVisible)
                imgPassword.Source = new BitmapImage(new Uri("/Images/key-icon-pink.png", UriKind.Relative));
        }

        private void Field_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender == txtUsername)
                imgUsername.Source = new BitmapImage(new Uri("/Images/user-icon.png", UriKind.Relative));
            else if (sender == txtPassword || sender == txtPasswordVisible)
                imgPassword.Source = new BitmapImage(new Uri("/Images/key-icon.png", UriKind.Relative));
        }
    }
}
