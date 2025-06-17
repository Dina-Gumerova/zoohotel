using Npgsql;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Drawing;
using System.Windows;
using System;
using System.Linq;
using System.Windows.Controls;

using static ZooHotel.DirectorWindow;
using Xceed.Wpf.Toolkit;
using System.Windows.Media;


namespace Зоогостиница_диплом_
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CellStatus status)
            {
                switch (status)
                {
                    case CellStatus.Free:
                        return Brushes.Green;
                    case CellStatus.Booked:
                        return Brushes.Yellow;
                    case CellStatus.Occupied:
                        return Brushes.Red;
                    default:
                        return Brushes.Gray;
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing; 
        }

    }

    public enum CellStatus
    {
        Free,
        Booked,
        Occupied
    }

    public class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Breed { get; set; }
        public string Size { get; set; }
        public double Weight { get; set; }
        public int Age { get; set; }
        public string Photo { get; set; }
        public string BehaviorDescription { get; set; }
        public string Color { get; set; }
        public string Gender { get; set; }
        public string BirthDate { get; set; }
        public string OwnerName { get; set; }
    }
 

    public class Cell : INotifyPropertyChanged
    {
        public int Number { get; set; }

        private CellStatus _status;
        public CellStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public string SizeRequirement { get; set; }
        public int WalksPerDay { get; set; }
        public string Feeding { get; set; }
        public Pet Occupant { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void SetCellSize()
        {
            if (Number >= 1 && Number <= 4)
                SizeRequirement = "Большие";
            else if (Number >= 5 && Number <= 10)
                SizeRequirement = "Средние";
            else
                SizeRequirement = "Малые";
        }


        public void UpdateStatusFromDatabase(string connectionString, DateTime date)
{
            if (Occupant != null)
            {
                Status = CellStatus.Occupied;
                return;
            }

    try
    {
        using (var conn = new NpgsqlConnection(connectionString))
        {
            conn.Open();

            // Проверка занятости (красный) из bookings
            var cmdOccupied = new NpgsqlCommand(
                @"SELECT 1 FROM animals a 
                    LEFT JOIN bookings b ON b.animal_id = a.id 
                    WHERE a.allowed = true AND cell_number = @placeId 
                    AND start_date <= @date AND end_date >= @date;", conn);
            cmdOccupied.Parameters.AddWithValue("@placeId", Number);
            cmdOccupied.Parameters.AddWithValue("@date", date);

            var isOccupied = cmdOccupied.ExecuteScalar() != null;

            if (isOccupied)
            {
                Status = CellStatus.Occupied;
                return;
            }

            // Проверка бронирования (жёлтый) из full_bookings
            var cmdBooked = new NpgsqlCommand(
                @"SELECT 1 FROM full_bookings 
                  WHERE cell_number = @placeId 
                  AND start_date <= @date AND end_date >= @date;", conn);
            cmdBooked.Parameters.AddWithValue("@placeId", Number);
            cmdBooked.Parameters.AddWithValue("@date", date);

            var isBooked = cmdBooked.ExecuteScalar() != null;

            Status = isBooked ? CellStatus.Booked : CellStatus.Free;
        }
    }
    catch
    {
        Status = CellStatus.Free;
    }
}

    }

    public partial class Booking : Window
    {
        public ObservableCollection<Cell> Cells { get; set; } = new ObservableCollection<Cell>();
        public ObservableCollection<Cell> Cells1 { get; set; }
        public ObservableCollection<Cell> Cells2 { get; set; }
        public ObservableCollection<Cell> Cells3 { get; set; }

        private readonly string connectionString = "Server=localhost; Port=5434; Username=postgres; Password=1234; Database=diplom";
        private FullBooking bookingData;

        public Booking()
        {
            InitializeComponent();
            InitializeCells();
            LoadAnimalsIntoCellsByDate(DateTime.Now.Date);

            Cells1 = new ObservableCollection<Cell>(Cells.Where(c => c.Number >= 1 && c.Number <= 4));
            Cells2 = new ObservableCollection<Cell>(Cells.Where(c => c.Number >= 5 && c.Number <= 10));
            Cells3 = new ObservableCollection<Cell>(Cells.Where(c => c.Number >= 11 && c.Number <= 20));

            DataContext = this;
        }

        private void InitializeCells()
        {
            for (int i = 1; i <= 20; i++)
            {
                var cell = new Cell { Number = i };
                cell.SetCellSize();
                cell.Status = CellStatus.Free;
                Cells.Add(cell);
            }
        }

        private void LoadAnimalsIntoCellsByDate(DateTime date)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new NpgsqlCommand(@"
             SELECT a.id, a.nickname, a.breed, a.weight, a.size, a.age,
                           b.cell_number
                    FROM animals a
                    JOIN bookings b ON b.animal_id = a.id
                    WHERE b.start_date <= @date AND b.end_date >= @date
                      AND a.allowed = true;", conn);
                    cmd.Parameters.AddWithValue("@date", date);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var pet = new Pet
                            {
                                Id = reader["id"] is DBNull ? 0 : Convert.ToInt32(reader["id"]),
                                Name = reader["nickname"]?.ToString() ?? "Без имени",
                                Breed = reader["breed"]?.ToString() ?? "Не указано",
                                Size = reader["size"]?.ToString() ?? "Не указано",
                                Weight = double.TryParse(reader["weight"]?.ToString(), out var weight) ? weight : 0.0,
                                Age = int.TryParse(reader["age"]?.ToString(), out var age) ? age : 0,
                                Photo = "default.jpg"
                            };
                            int placeId = reader["cell_number"] is DBNull ? -1 : Convert.ToInt32(reader["cell_number"]);

                            var cell = Cells.FirstOrDefault(c => c.Number == placeId);
                            if (cell != null)
                            {
                                cell.Occupant = pet;
                                cell.Status = CellStatus.Occupied;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка загрузки животных: " + ex.Message);
            }
        }

        private void RefreshCells_Click(object sender, RoutedEventArgs e)
        {
            RefreshCells(DateTime.Now);
        }
        public void RefreshCells(DateTime date)
        {
            foreach (var cell in Cells)
            {
                cell.Occupant = null;
                cell.Status = CellStatus.Free;
            }

            foreach (var cell in Cells)
            {
                cell.UpdateStatusFromDatabase(connectionString, date);
            }

            LoadAnimalsIntoCellsByDate(date);
        }

        private void CheckAvailability_Click(object sender, RoutedEventArgs e)
        {
            DateTime date = BookingDatePicker.SelectedDate ?? DateTime.Now.Date;

            foreach (var cell in Cells)
            {
                cell.Occupant = null;
                cell.Status = CellStatus.Free;
            }

            foreach (var cell in Cells)
            {
                cell.UpdateStatusFromDatabase(connectionString, date);
            }

            LoadAnimalsIntoCellsByDate(date);
        }

        private void Cell_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var selectedCell = button?.Tag as Cell;

            if (selectedCell == null) return;

            if (selectedCell.Status == CellStatus.Free)
            {
                var selectWindow = new SelectActionWindow();
                selectWindow.Owner = this;
                selectWindow.ShowDialog();

                if (selectWindow.SelectedAction == "Booking")
                {
                    var bookingForm = new BookingWindow(connectionString, selectedCell.Number);
                    bookingForm.Owner = this;
                    bookingForm.ShowDialog();
                    CheckAvailability_Click(null, null);
                }

                else if (selectWindow.SelectedAction == "Settlement")
                {
                    System.Windows.MessageBox.Show("Выбранная клетка: " + selectedCell.Number);

                    var settlementWindow = new Settlement(connectionString, bookingData, selectedCell.Number);
                    settlementWindow.Owner = this;
                    settlementWindow.ShowDialog(); 


                    RefreshCells(DateTime.Now.Date);
                }
            }
            else if (selectedCell.Status == CellStatus.Booked)
            {
                // Показываем окно с информацией о брони
                var booking = GetBookingInfo(selectedCell.Number, BookingDatePicker.SelectedDate ?? DateTime.Today);
                if (booking != null)
                {
                    var window = new BookingInfoWindow(connectionString, booking);
                    window.Owner = this;
                    window.ShowDialog();

                    CheckAvailability_Click(null, null); 
                }
            }
            else if (selectedCell.Status == CellStatus.Occupied && selectedCell.Occupant != null)
            {
                var infoWindow = new CellInfoWindow();
                infoWindow.LoadAnimalInfo(selectedCell.Occupant.Id, connectionString);
                infoWindow.Owner = this;
                infoWindow.ShowDialog();
            }
            
        } 
            private BookingInfoWindow.FullBooking GetBookingInfo(int cellNumber, DateTime date)
        {
            try
            {
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                var cmd = new NpgsqlCommand(@"
            SELECT owner_name, pet_name, breed, size, weight, age, cell_number, start_date, end_date,type_animal
            FROM full_bookings
            WHERE cell_number = @cell
            AND @date BETWEEN start_date AND end_date
            LIMIT 1;", conn);

                cmd.Parameters.AddWithValue("@cell", cellNumber);
                cmd.Parameters.AddWithValue("@date", date);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new BookingInfoWindow.FullBooking
                    {
                        OwnerName = reader["owner_name"].ToString(),
                        PetName = reader["pet_name"].ToString(),
                        Breed = reader["breed"].ToString(),
                        Size = reader["size"].ToString(),
                        Weight = (decimal)Convert.ToDouble(reader["weight"]),
                        Age = Convert.ToInt32(reader["age"]),
                        StartDate = Convert.ToDateTime(reader["start_date"]),
                        EndDate = Convert.ToDateTime(reader["end_date"]),
                        Type_animal = reader["type_animal"].ToString(),
                    };
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка получения информации о бронировании: " + ex.Message);
            }

            return null;
        }

        public class BookingInfo
        {
            public int AnimalId { get; set; }
            public int Сell_number { get; set; }
            
            public string OwnerName { get; set; }
            public string PetName { get; set; }
            public string Breed { get; set; }
            public string Size { get; set; }
            public double Weight { get; set; }
            public int Age { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new MainWindow();
            menu.Show();
            this.Close();
        }
    }
}