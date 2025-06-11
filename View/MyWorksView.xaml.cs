using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Data;
using Constructor_Gallerix.Models;
using Constructor_Gallerix.Repository;

namespace Constructor_Gallerix.View
{
    public partial class MyWorksView : Window
    {
        private readonly int _userId;
        private readonly string _username;
        private readonly string _connectionString =
            "Host=localhost;Port=5432;Username=postgres;Password=1234;Database=GallerixDB";

        private ObservableCollection<Exhibition> allExhibitions = new ObservableCollection<Exhibition>();
        private ObservableCollection<Exhibition> filteredExhibitions = new ObservableCollection<Exhibition>();

        public MyWorksView(int userId, string username)
        {
            InitializeComponent();
            _userId = userId;
            _username = username;
            txtUsername.Text = username;
            LoadGalleries();
        }

        private void LoadGalleries()
        {
            try
            {
                allExhibitions.Clear();

                using (var repo = new ExhibitionRepository(_connectionString))
                {
                    var basicExhibitions = repo.GetUserExhibitions(_userId);
                    foreach (var basicExhibition in basicExhibitions.Where(ex => ex != null))
                    {
                        // Для каждой галереи подгружаем полностью (с обложкой и картинами)
                        var fullExhibition = repo.GetExhibitionWithPictures(basicExhibition.ExhibitionId);
                        allExhibitions.Add(fullExhibition);
                    }
                }

                // Задаём источник для ItemsControl
                filteredExhibitions = new ObservableCollection<Exhibition>(allExhibitions);
                GalleriesList.ItemsSource = filteredExhibitions;

                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки галерей: {ex.Message}");
                MessageBox.Show("Не удалось загрузить галереи", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                if (txtNoResults == null || cmbFilter == null || txtSearch == null)
                    return;

                if (allExhibitions == null || allExhibitions.Count == 0)
                {
                    filteredExhibitions?.Clear();
                    txtNoResults.Visibility = Visibility.Visible;
                    return;
                }

                string searchText = txtSearch.Text?.ToLower()?.Trim() ?? "";
                if (searchText == "введите название галереи...") searchText = "";

                string selectedFilter = (cmbFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "all";

                var filtered = allExhibitions.Where(ex => ex != null);

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    filtered = filtered.Where(ex =>
                        !string.IsNullOrEmpty(ex.Title) &&
                        ex.Title.ToLower().Contains(searchText)
                    );
                }

                switch (selectedFilter)
                {
                    case "favorites":
                        filtered = filtered.Where(ex => ex.IsFavorite);
                        break;
                    case "public":
                        filtered = filtered.Where(ex => ex.IsPublic);
                        break;
                    case "private":
                        filtered = filtered.Where(ex => !ex.IsPublic);
                        break;
                }

                filteredExhibitions.Clear();
                foreach (var item in filtered)
                    filteredExhibitions.Add(item);

                txtNoResults.Visibility = filteredExhibitions.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка фильтрации: {ex}");
                MessageBox.Show("Ошибка при фильтрации галерей", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Exhibition exhibition)
            {
                try
                {
                    exhibition.IsPublic = !exhibition.IsPublic;
                    using (var repo = new ExhibitionRepository(_connectionString))
                        repo.UpdateExhibition(exhibition);

                    ApplyFilters();
                    CollectionViewSource.GetDefaultView(filteredExhibitions).Refresh();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка изменения видимости: {ex.Message}");
                    MessageBox.Show("Не удалось изменить видимость галереи", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            e.Handled = true;
        }

        private void DeleteGallery_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Exhibition exhibition)
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить галерею '{exhibition.Title}'?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var repo = new ExhibitionRepository(_connectionString))
                        {
                            // Удаляем галерею вместе со всеми её картинами
                            repo.DeleteExhibition(exhibition.ExhibitionId);
                            allExhibitions.Remove(exhibition);
                            ApplyFilters();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении галереи: {ex.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            e.Handled = true;
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Exhibition exhibition)
            {
                try
                {
                    exhibition.IsFavorite = !exhibition.IsFavorite;
                    using (var repo = new ExhibitionRepository(_connectionString))
                        repo.UpdateExhibition(exhibition);

                    ApplyFilters();
                    CollectionViewSource.GetDefaultView(filteredExhibitions).Refresh();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка изменения избранного: {ex.Message}");
                    MessageBox.Show("Не удалось обновить избранное", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            e.Handled = true;
        }

        private void Gallery_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            if (sender is FrameworkElement element && element.DataContext is Exhibition exhibition)
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    var constructorView = new ConstructorView(_userId, txtUsername.Text, exhibition.ExhibitionId);
                    constructorView.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии галереи: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Введите название галереи...")
                txtSearch.Text = "";
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                txtSearch.Text = "Введите название галереи...";
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e) => ApplyFilters();
        private void cmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

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

        // --- МЕНЮ ---
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            var mainView = new MainView(txtUsername.Text, _userId);
            mainView.Show();
            this.Close();
        }

        private void ConstructorButton_Click(object sender, RoutedEventArgs e)
        {
            var constructorView = new ConstructorView(_userId, txtUsername.Text, 0);
            constructorView.Show();
            this.Close();
        }

        private void DemoButton_Click(object sender, RoutedEventArgs e)
        {
            var demoView = new DemoView(_userId, _username);
            demoView.Show();
            this.Close();
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileView = new ProfileView(_userId, _username);
            profileView.Show();
            this.Close();
        }

        // **Новый обработчик для кнопки "Справка"**
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var help = new HelpView(_username, _userId);
            help.Owner = this;
            help.Show();
            // MyWorksView остаётся открытым
        }

        private void ShowAllButton_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            cmbFilter.SelectedIndex = 0; // "Все галереи"
            ApplyFilters();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
