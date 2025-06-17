using System.Windows;

namespace Зоогостиница_диплом_
{
    public partial class SelectActionWindow : Window
    {
        private string role;
        private string connectionString;
        private object cell;

        public string SelectedAction { get; private set; }

        public SelectActionWindow()
        {
            InitializeComponent();
        }

        private void Booking_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = "Booking";
            DialogResult = true;
            Close();
        }

        private void Settlement_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = "Settlement";
            DialogResult = true;
            Close();
        }

    }
}
