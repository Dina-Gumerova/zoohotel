using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Зоогостиница_диплом_
{
    public partial class ClientBooking : Window
    {
        public ObservableCollection<Cell> Cells { get; set; } = new ObservableCollection<Cell>();
        private readonly string connectionString = "Server=localhost; Port=5434; Username=postgres; Password=1234; Database=diplom";
        private string _userName;
        private string _role;
        public ClientBooking(string userName, string role)
        {
            InitializeComponent();
            _userName = userName;
            _role = role;
            DataContext = this;
            InitializeCells();
            CheckAvailability_Click(null, null);
        }

        private void InitializeCells()
        {
            for (int i = 1; i <= 20; i++)
            {
                var cell = new Cell { Number = i };
                Cells.Add(cell); 
            }
        }


        private void CheckAvailability_Click(object sender, RoutedEventArgs e)
        {
            DateTime selectedDate = BookingDatePicker.SelectedDate ?? DateTime.Now.Date;
            foreach (var cell in Cells)
            {
                cell.Occupant = null;
                cell.Status = CellStatus.Free;
                cell.UpdateStatusFromDatabase(connectionString, selectedDate);
            }
        }

        private void Cell_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var selectedCell = button?.Tag as Cell;
            if (sender is Button btn && btn.Tag is Cell cell)
            {
                if (cell.Status == CellStatus.Free)
                {
                    var form = new BookingWindow(connectionString, selectedCell.Number);
                    form.ShowDialog();
                    CheckAvailability_Click(null, null); 
                }
                else
                {
                    MessageBox.Show("Клетка занята или уже забронирована.", "Недоступно", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
