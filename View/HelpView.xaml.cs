using System.Windows;
using System.Windows.Input;

namespace Constructor_Gallerix.View
{
    /// <summary>
    /// Interaction logic for HelpView.xaml
    /// </summary>
    public partial class HelpView : Window
    {
        private readonly string currentUsername;
        private readonly int currentUserId;

        /// <summary>
        /// Конструктор HelpView принимает параметры текущего пользователя,
        /// чтобы при закрытии обратно возвращаться к нужному окну (если потребуется).
        /// </summary>
        public HelpView(string username, int userId)
        {
            InitializeComponent();
            currentUsername = username;
            currentUserId = userId;
        }

        #region Window Controls

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        #endregion
    }
}
