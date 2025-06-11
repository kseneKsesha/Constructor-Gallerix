using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Constructor_Gallerix.Models;
using Constructor_Gallerix.Repository;
using Constructor_Gallerix.Templates;
using Microsoft.Win32;
using Npgsql;

namespace Constructor_Gallerix.View
{
    public partial class ConstructorView : Window
    {
        private int currentStep = 1;
        private string selectedTemplate = string.Empty;
        private readonly string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=1234;Database=GallerixDB";
        private readonly int currentUserId;
        private readonly string currentUsername;
        private bool isSaved = false;
        private bool isClosingConfirmed = false;
        private string tempPicturePath;
        private readonly int existingExhibitionId;
        private bool isEditingMode = false;
        private DateTime originalCreationDate;

        private readonly ObservableCollection<Picture> pictures = new ObservableCollection<Picture>();
        private readonly List<Border> templateBorders;

        // 1) Поле для хранения байтов оригинальной обложки
        private byte[] currentCoverImageData = null;
        // 2) Флаг, указывающий, что пользователь выбрал новую обложку
        private bool coverChanged = false;

        public ObservableCollection<Picture> Pictures => pictures;

        public ConstructorView(int userId, string username, int exhibitionId = 0)
        {
            InitializeComponent();
            this.DataContext = this;

            currentUserId = userId;
            currentUsername = username;
            existingExhibitionId = exhibitionId;
            isEditingMode = exhibitionId > 0;

            templateBorders = new List<Border> { Template1Border, Template2Border, Template3Border, Template4Border };

            InitializeDefaultValues();
            UpdateStep();
            this.Closing += ConstructorView_Closing;

            txtPictureYear.PreviewTextInput += TxtPictureYear_PreviewTextInput;
            DataObject.AddPastingHandler(txtPictureYear, OnPaste);

            if (isEditingMode)
            {
                LoadExistingExhibition();
            }
        }

        private void LoadExistingExhibition()
        {
            try
            {
                using (var exhibitionRepo = new ExhibitionRepository(connectionString))
                {
                    var exhibition = exhibitionRepo.GetExhibitionWithPictures(existingExhibitionId);
                    if (exhibition != null)
                    {
                        originalCreationDate = exhibition.CreatedAt;
                        txtCreationDate.Text = originalCreationDate.ToShortDateString();
                        txtGalleryName.Text = exhibition.Title;
                        txtDescription.Text = exhibition.Description;
                        selectedTemplate = exhibition.Template;

                        // Отображаем имя пользователя и автора
                        txtAuthor.Text = currentUsername;

                        // Выделение сохранённого шаблона
                        if (!string.IsNullOrEmpty(selectedTemplate))
                        {
                            foreach (var border in templateBorders)
                            {
                                if (border.Child is Button button && button.Tag.ToString() == selectedTemplate)
                                {
                                    border.BorderBrush = Brushes.DodgerBlue;
                                    border.Background = new SolidColorBrush(Color.FromArgb(30, 30, 144, 255));
                                    break;
                                }
                            }
                        }

                        // 3) Загрузка обложки из byte[] и запоминаем байты
                        if (exhibition.CoverImageData != null && exhibition.CoverImageData.Length > 0)
                        {
                            currentCoverImageData = exhibition.CoverImageData; // оригинальные байты
                            imgCover.Source = exhibition.CoverImage;            // сам BitmapImage
                            imgCover.Visibility = Visibility.Visible;
                            txtAddCover.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            currentCoverImageData = null;
                            imgCover.Source = null;
                            imgCover.Visibility = Visibility.Collapsed;
                            txtAddCover.Visibility = Visibility.Visible;
                        }

                        // Загрузка картин
                        pictures.Clear();
                        if (exhibition.Pictures != null && exhibition.Pictures.Count > 0)
                        {
                            foreach (var pic in exhibition.Pictures)
                                pictures.Add(pic);
                        }

                        UpdatePreviewData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки галереи: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtPictureYear_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            var textBox = sender as TextBox;
            string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            if (!string.IsNullOrEmpty(newText))
            {
                if (int.TryParse(newText, out int year))
                {
                    if (year > DateTime.Now.Year)
                    {
                        e.Handled = true;
                        ShowError($"Год не может быть больше {DateTime.Now.Year}");
                    }
                }
            }
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!int.TryParse(text, out int year) || year > DateTime.Now.Year || year < 0)
                {
                    e.CancelCommand();
                    ShowError($"Год должен быть положительным числом не больше {DateTime.Now.Year}");
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void ConstructorView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isClosingConfirmed || isSaved) return;
            if (!ConfirmCloseWithoutSaving()) e.Cancel = true;
        }

        private bool ConfirmCloseWithoutSaving()
        {
            return MessageBox.Show(
                "Вы не сохранили галерею. Закрыть без сохранения?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        private void InitializeDefaultValues()
        {
            txtUsername.Text = currentUsername;
            txtCreationDate.Text = DateTime.Now.ToShortDateString();
            txtAuthor.Text = currentUsername;
            txtGalleryName.Text = string.Empty;
            txtDescription.Text = string.Empty;
        }

        #region Шаги конструктора

        private void UpdateStep()
        {
            Step1Grid.Visibility = currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2Grid.Visibility = currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3Grid.Visibility = currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
            Step4Grid.Visibility = currentStep == 4 ? Visibility.Visible : Visibility.Collapsed;

            if (currentStep == 2 && !string.IsNullOrEmpty(selectedTemplate))
            {
                var templateBorder = templateBorders.FirstOrDefault(b =>
                    (b.Child as Button)?.Tag?.ToString() == selectedTemplate);
                if (templateBorder != null)
                {
                    templateBorder.BorderBrush = Brushes.DodgerBlue;
                    templateBorder.Background = new SolidColorBrush(Color.FromArgb(30, 30, 144, 255));
                }
            }

            if (currentStep == 4)
                UpdatePreviewData();
        }

        private void UpdatePreviewData()
        {
            previewGalleryName.Text = txtGalleryName.Text;
            previewDescription.Text = txtDescription.Text;
            previewAuthor.Text = txtAuthor.Text;
            previewCreationDate.Text = txtCreationDate.Text;

            previewTemplate.Text = GetTemplateDisplayName(selectedTemplate);

            if (imgCover.Source != null)
            {
                previewCoverImage.Source = imgCover.Source;
            }
        }

        private string GetTemplateDisplayName(string template)
        {
            switch (template)
            {
                case "ClassicGrid":
                    return "Классическая сетка";
                case "Carousel":
                    return "Карусель";
                case "Mosaic":
                    return "Мозаика";
                case "FullscreenSlider":
                    return "Полноэкранный слайдер";
                default:
                    return "Не выбран";
            }
        }

        #endregion

        #region Навигация

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            currentStep = Math.Max(currentStep - 1, 1);
            UpdateStep();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateCurrentStep()) return;
            currentStep = Math.Min(currentStep + 1, 4);
            UpdateStep();
        }

        private bool ValidateCurrentStep()
        {
            // Шаг 1: проверяем всё, что необходимо заполнить
            if (currentStep == 1)
            {
                if (imgCover.Source == null)
                {
                    ShowError("Выберите обложку галереи.");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(txtGalleryName.Text))
                {
                    ShowError("Введите название галереи.");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(txtAuthor.Text))
                {
                    ShowError("Поле 'Автор' не может быть пустым.");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(txtCreationDate.Text))
                {
                    ShowError("Поле 'Дата создания' не может быть пустым.");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(txtDescription.Text))
                {
                    ShowError("Введите описание галереи.");
                    return false;
                }
            }

            // Шаг 3: проверяем, что есть хотя бы одна картина
            if (currentStep == 3 && !pictures.Any())
            {
                ShowError("Добавьте хотя бы одну картину.");
                return false;
            }

            return true;
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void UpdatePictures(NpgsqlConnection conn, List<Picture> pictures, int exhibitionId)
        {
            // Удаляем старые картины
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM pictures WHERE exhibition_id = @exhibitionId", conn))
            {
                cmd.Parameters.AddWithValue("@exhibitionId", exhibitionId);
                cmd.ExecuteNonQuery();
            }

            // Добавляем новые
            foreach (var picture in pictures)
            {
                InsertPicture(conn, picture, exhibitionId);
            }
        }

        private void UpdateExhibition(NpgsqlConnection conn, Exhibition exhibition)
        {
            const string sql = @"
                UPDATE exhibitions SET
                    title = @title,
                    description = @description,
                    is_public = @isPublic,
                    template = @template,
                    cover_image_data = @coverImageData
                WHERE exhibition_id = @exhibitionId";

            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@exhibitionId", exhibition.ExhibitionId);
                cmd.Parameters.AddWithValue("@title", exhibition.Title);
                cmd.Parameters.AddWithValue("@description", exhibition.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@isPublic", exhibition.IsPublic);
                cmd.Parameters.AddWithValue("@template", exhibition.Template);

                // 4) При апдейте используем либо текущие (если не меняли), либо новые байты:
                cmd.Parameters.AddWithValue(
                    "@coverImageData",
                    coverChanged
                        ? (exhibition.CoverImageData ?? (object)DBNull.Value)
                        : (currentCoverImageData ?? (object)DBNull.Value));

                cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region Работа с изображениями

        private bool ValidateBeforeSave()
        {
            if (string.IsNullOrWhiteSpace(txtGalleryName.Text))
            {
                MessageBox.Show("Введите название галереи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(selectedTemplate))
            {
                MessageBox.Show("Выберите шаблон галереи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (pictures.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одну картину!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void UploadPicture_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Изображения (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Выберите изображение картины"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    tempPicturePath = dlg.FileName;
                    var bitmap = new BitmapImage();

                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(tempPicturePath);
                    bitmap.DecodePixelWidth = 300;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    imgPreview.Source = bitmap;
                    imgPreview.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}",
                                  "Ошибка",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
            }
        }

        private void CoverUpload_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите изображение",
                Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png"
            };

            if (dialog.ShowDialog() == true)
            {
                var bitmap = new BitmapImage(new Uri(dialog.FileName));
                imgCover.Source = bitmap;
                imgCover.Visibility = Visibility.Visible;
                txtAddCover.Visibility = Visibility.Collapsed;

                // 5) Помечаем, что пользователь выбрал новую обложку
                coverChanged = true;
            }
        }

        private void AddSinglePicture_Click(object sender, RoutedEventArgs e)
        {
            // Валидация полей
            if (string.IsNullOrWhiteSpace(txtPictureTitle.Text))
            {
                MessageBox.Show("Введите название картины!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPictureAuthor.Text))
            {
                MessageBox.Show("Введите автора картины!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (imgPreview.Source == null)
            {
                MessageBox.Show("Добавьте изображение картины!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка года
            if (!int.TryParse(txtPictureYear.Text, out int year) || year < 0 || year > DateTime.Now.Year)
            {
                MessageBox.Show($"Год должен быть числом от 0 до {DateTime.Now.Year}!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Создание новой картины
                var newPicture = new Picture
                {
                    Title = txtPictureTitle.Text,
                    Author = txtPictureAuthor.Text,
                    Year = year,
                    Description = txtPictureDescription.Text,
                    ExpertComment = txtExpertComment.Text,
                    ImageData = GetImageBytes(imgPreview.Source) // Конвертация изображения в byte[]
                };

                // Добавление в коллекцию
                pictures.Add(newPicture);

                // Очистка полей
                ClearPictureFields();

                // Обновление интерфейса
                imgPreview.Source = null;
                imgPreview.Visibility = Visibility.Collapsed;
                tempPicturePath = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении картины: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private byte[] GetImageBytes(ImageSource imageSource)
        {
            if (imageSource == null) return null;

            try
            {
                if (imageSource is BitmapImage bitmapImage)
                {
                    if (bitmapImage.StreamSource != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            bitmapImage.StreamSource.CopyTo(memoryStream);
                            return memoryStream.ToArray();
                        }
                    }
                    else if (bitmapImage.UriSource != null)
                    {
                        return File.ReadAllBytes(bitmapImage.UriSource.LocalPath);
                    }
                }

                // Для других случаев (например, RenderTargetBitmap)
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imageSource));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка конвертации изображения: {ex.Message}");
                return null;
            }
        }

        private void ClearPictureFields()
        {
            txtPictureTitle.Clear();
            txtPictureAuthor.Clear();
            txtPictureYear.Clear();
            txtPictureDescription.Clear();
            txtExpertComment.Clear();
        }

        private void RemoveImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Picture picture)
            {
                try
                {
                    if (MessageBox.Show("Удалить эту картину?", "Подтверждение",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        if (picture.Id > 0)
                        {
                            using (var repo = new PictureRepository(connectionString))
                            {
                                repo.DeletePicture(picture.Id);
                            }
                        }

                        pictures.Remove(picture);
                        isSaved = false;
                    }
                }
                catch (Exception ex)
                {
                    ShowError("Ошибка удаления: " + ex.Message);
                }
            }
        }

        #endregion

        #region Сохранение и предпросмотр

        private void SaveGallery_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateBeforeSave()) return;

            try
            {
                // Собираем объект выставки из UI
                var exhibition = new Exhibition
                {
                    UserId = currentUserId,
                    Title = txtGalleryName.Text.Trim(),
                    Description = txtDescription.Text.Trim(),
                    Template = selectedTemplate,
                    IsPublic = true,
                    CreatedAt = isEditingMode ? originalCreationDate : DateTime.Now,
                    // 6) Сюда ещё не кладём CoverImageData, сделаем это чуть ниже
                    Pictures = new List<Picture>(pictures)
                };

                if (isEditingMode)
                {
                    exhibition.ExhibitionId = existingExhibitionId; // запись ID для апдейта
                }

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            if (isEditingMode)
                            {
                                // Проверяем, нужно ли обновить CoverImageData
                                if (coverChanged && imgCover.Source != null)
                                {
                                    exhibition.CoverImageData = GetImageBytes(imgCover.Source);
                                }
                                else
                                {
                                    // если не меняли, используем старую байтовую картинку
                                    exhibition.CoverImageData = currentCoverImageData;
                                }

                                // Обновляем саму выставку
                                UpdateExhibition(conn, exhibition);

                                // Обновляем все картинки для этой выставки
                                UpdatePictures(conn, exhibition.Pictures, exhibition.ExhibitionId);
                            }
                            else
                            {
                                // Новая выставка: конвертируем обложку (возможно null, если не загрузили)
                                exhibition.CoverImageData = imgCover.Source != null
                                    ? GetImageBytes(imgCover.Source)
                                    : null;

                                InsertExhibition(conn, exhibition);
                            }

                            transaction.Commit();

                            MessageBox.Show("Галерея успешно сохранена!", "Успех",
                                          MessageBoxButton.OK, MessageBoxImage.Information);

                            isSaved = true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
                                            "Ошибка",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Неожиданная ошибка: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private void InsertPicture(NpgsqlConnection conn, Picture picture, int exhibitionId)
        {
            const string sql = @"
                INSERT INTO pictures 
                    (exhibition_id, title, author, year, description, expert_comment, image_data, orientation) 
                VALUES 
                    (@exhibitionId, @title, @author, @year, @description, @expertComment, @imageData, @orientation)";

            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@exhibitionId", exhibitionId);
                cmd.Parameters.AddWithValue("@title", picture.Title ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@author", picture.Author ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@year", picture.Year);
                cmd.Parameters.AddWithValue("@description", picture.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@expertComment", picture.ExpertComment ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@imageData", picture.ImageData ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@orientation", picture.Orientation ?? "Horizontal");

                cmd.ExecuteNonQuery();
            }
        }

        private void InsertExhibition(NpgsqlConnection conn, Exhibition exhibition)
        {
            const string sql = @"
                INSERT INTO exhibitions 
                    (user_id, title, description, is_public, template, created_at, cover_image_data) 
                VALUES 
                    (@userId, @title, @description, @isPublic, @template, @createdAt, @coverImageData)
                RETURNING exhibition_id";

            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@userId", exhibition.UserId);
                cmd.Parameters.AddWithValue("@title", exhibition.Title);
                cmd.Parameters.AddWithValue("@description", exhibition.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@isPublic", exhibition.IsPublic);
                cmd.Parameters.AddWithValue("@template", exhibition.Template);
                cmd.Parameters.AddWithValue("@createdAt", exhibition.CreatedAt);
                cmd.Parameters.AddWithValue(
                    "@coverImageData",
                    exhibition.CoverImageData ?? (object)DBNull.Value);

                exhibition.ExhibitionId = (int)cmd.ExecuteScalar();
            }

            foreach (var picture in exhibition.Pictures)
            {
                InsertPicture(conn, picture, exhibition.ExhibitionId);
            }
        }

        private void PreviewResult_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedTemplate))
            {
                MessageBox.Show("Выберите шаблон галереи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var previewData = new Exhibition
                {
                    Title = txtGalleryName.Text,
                    Description = txtDescription.Text,
                    Template = selectedTemplate,
                    Pictures = new List<Picture>(pictures)
                };

                BitmapImage coverImage = null;
                if (imgCover.Source != null)
                {
                    coverImage = (BitmapImage)imgCover.Source;
                }

                var previewWindow = new ExhibitionView(
                    previewData.Title,
                    previewData.Description,
                    previewData.Template,
                    previewData.Pictures,
                    coverImage
                );
                previewWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании предпросмотра: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Выбор шаблона

        private void TemplateButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                // Сбрасываем рамки у всех
                foreach (var border in templateBorders)
                {
                    border.BorderBrush = Brushes.Transparent;
                    border.Background = Brushes.Transparent;
                }

                var selectedBorder = button.Parent as Border;
                selectedBorder.BorderBrush = Brushes.DodgerBlue;
                selectedBorder.Background = new SolidColorBrush(Color.FromArgb(30, 30, 144, 255));

                selectedTemplate = button.Tag.ToString();
                Debug.WriteLine($"Выбран шаблон: {selectedTemplate}");
            }
        }

        #endregion

        #region Управление окном

        private void btnClose_Click(object sender, RoutedEventArgs e) => CloseWithConfirmation();
        private void btnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void CloseWithConfirmation()
        {
            if (isSaved || ConfirmCloseWithoutSaving())
            {
                isClosingConfirmed = true;
                Close();
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToNewWindow(() => new MainView(currentUsername, currentUserId));
        }

        private void DemoButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToNewWindow(() => new DemoView(currentUserId, currentUsername));
        }

        private void MyWorksButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToNewWindow(() => new MyWorksView(currentUserId, currentUsername));
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
            help.Show();
            // **Форма ConstructorView остаётся открытой**
        }

        private void NavigateToNewWindow(Func<Window> factory)
        {
            if (isSaved || ConfirmCloseWithoutSaving())
            {
                isClosingConfirmed = true;
                factory().Show();
                Close();
            }
        }

        #endregion
    }
}
