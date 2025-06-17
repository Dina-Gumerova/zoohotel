using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using System.Text;
using System.Windows;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Win32;
using Npgsql;
using Зоогостиница_диплом_;
using System.Linq;

namespace ZooHotel
{
    public partial class DirectorWindow : Window
    {
     
        private string connectionString = "Host=localhost; Port=5434;Username=postgres;Password=1234;Database=diplom";

        public DirectorWindow()
        {
            InitializeComponent();
            LoadBookings();
            LoadWorkSchedule();
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        private void LoadBookings()
        {
            try
            {
                var bookings = new List<BookingInfo>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                          SELECT
                b.id AS booking_id,
                a.nickname AS pet_name,
                ap.name AS cage_name,
                b.start_date,
                b.end_date,
                b.status,
                b.price
                FROM bookings b
                JOIN animals a ON b.animal_id = a.id
                JOIN accommodation_places ap ON b.accommodation_place_id = ap.id
                ORDER BY b.start_date;  ";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bookings.Add(new BookingInfo
                            {
                                Id = Convert.ToInt32(reader["booking_id"]),
                                CellNumber = reader["cage_name"].ToString(),
                                Status = reader["status"].ToString(),
                                AnimalName = reader["pet_name"].ToString(),
                                DateFrom = Convert.ToDateTime(reader["start_date"]).ToShortDateString(),
                                DateTo = Convert.ToDateTime(reader["end_date"]).ToShortDateString(),
                                Price = reader["price"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["price"])
                            });
                        }
                    }
                }

                BookingGrid.ItemsSource = bookings;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки бронирований:\n" + ex.Message);
            }
        }
        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var financeRecords = new List<FinanceRecord>();

                // Получаем выбранные даты
                var startDate = StartDatePicker.SelectedDate;
                var endDate = EndDatePicker.SelectedDate;

                if (startDate == null || endDate == null)
                {
                    MessageBox.Show("Пожалуйста, выберите обе даты (С и По).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                           string sql = @"
                                SELECT 
                                        p.id AS payment_id,
                                        o.first_name || ' ' || o.last_name AS owner_name,
                                        a.nickname as pet_name,           
                                        p.owner_id,
                                        p.booking_id,
                                        b.start_date,
                                        b.end_date,
                                        b.cell_number as cage_name,
                                        COALESCE(SUM(vs.cost), 0) AS service_total,
                                        b.price AS booking_price,
                                        COALESCE(SUM(vs.cost), 0) + COALESCE(b.price, 0) AS total_amount,
                                        p.method,
                                        p.paid_date
                                    FROM payments p
                                    LEFT JOIN owners o ON p.owner_id = o.id
                                    LEFT JOIN bookings b ON p.booking_id = b.id
                                    LEFT JOIN animals a ON b.animal_id = a.id
                                    LEFT JOIN service_record sr ON sr.animal_id = a.id
                                    LEFT JOIN veterinary_services vs ON sr.veterinary_service_id = vs.id
                                    GROUP BY p.id, o.first_name, o.last_name,a.nickname, p.owner_id, p.booking_id,b.cell_number, b.price, p.method,b.start_date,  b.end_date,p.paid_date
                                    ORDER BY p.id; ";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("start_date", startDate.Value);
                        cmd.Parameters.AddWithValue("end_date", endDate.Value);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                financeRecords.Add(new FinanceRecord
                                {
                                    Id = Convert.ToInt32(reader["payment_id"]),
                                    OwnerName = reader["owner_name"].ToString(),
                                    PetName = reader["pet_name"].ToString(),
                                    CageName = reader["cage_name"].ToString(),
                                    StartDate = Convert.ToDateTime(reader["start_date"]).ToShortDateString(),
                                    EndDate = Convert.ToDateTime(reader["end_date"]).ToShortDateString(),
                                    Price = Convert.ToDecimal(reader["total_amount"]),
                                    Paiddate = reader["paid_date"].ToString()
                                });
                            }

                        }
                    }
                }

                FinanceDataGrid.ItemsSource = financeRecords;

                // Подсчёт итоговой суммы
                decimal total = 0;
                foreach (var rec in financeRecords)
                    total += rec.Price;

                TotalTextBlock.Text = $"Общая сумма: {total:C}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при формировании финансового отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Класс для отображения данных в таблице
        public class FinanceRecord
        {
            public int Id { get; set; }
            public string OwnerName { get; set; }
            public string PetName { get; set; }
            public string CageName { get; set; }
            public string StartDate { get; set; }
            public string EndDate { get; set; }
            public decimal Price { get; set; }
            public string Paiddate { get; set; }
        }

        private void ExportFinanceToExcel_Click(object sender, RoutedEventArgs e)
        {
            
            var data = FinanceDataGrid.ItemsSource as IEnumerable<DirectorWindow.FinanceRecord>;
            if (data == null)
            {
                MessageBox.Show("Нет данных для экспорта.");
                return;
            }

            

            var dialog = new SaveFileDialog
            {
                Filter = "Excel файл (*.xlsx)|*.xlsx",
                FileName = "Финансовый_отчет.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("Финансовый отчет");

                        // Заголовки
                        ws.Cell(1, 1).Value = "ID бронирования";
                        ws.Cell(1, 2).Value = "Клетка";
                        ws.Cell(1, 3).Value = "Имя питомца";
                        ws.Cell(1, 4).Value = "Дата начала";
                        ws.Cell(1, 5).Value = "Дата окончания";
                        ws.Cell(1, 6).Value = "Цена";
                        ws.Cell(1, 7).Value = "Статус";

                        int row = 2;
                        foreach (var record in data)
                        {
                            ws.Cell(row, 1).Value = record.Id;
                            ws.Cell(row, 2).Value = record.CageName;
                            ws.Cell(row, 3).Value = record.PetName;
                            ws.Cell(row, 4).Value = record.StartDate;
                            ws.Cell(row, 5).Value = record.EndDate;
                            ws.Cell(row, 6).Value = record.Price;
                            ws.Cell(row, 7).Value = record.Paiddate;
                            row++;
                        }

                        ws.Columns().AdjustToContents();
                        workbook.SaveAs(dialog.FileName);

                        MessageBox.Show("Отчёт успешно экспортирован в Excel.", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при экспорте в Excel:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void LoadSchedule_Click(object sender, RoutedEventArgs e)
        {
            var from = ScheduleStartPicker.SelectedDate;
            var to = ScheduleEndPicker.SelectedDate;

            LoadWorkSchedule(from, to); 
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"SELECT status, COUNT(*) as count FROM bookings GROUP BY status";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            StringBuilder report = new StringBuilder();
                            report.AppendLine("Статус бронирования : Количество");
                            report.AppendLine("-------------------------------");

                            while (reader.Read())
                            {
                                report.AppendLine($"{reader["status"]} : {reader["count"]}");
                            }

                            // Записываем в TextBlock
                            ScheduleTextBlock.Text = report.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке графика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void LoadWorkSchedule(DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var schedules = new List<WorkScheduleInfo>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                SELECT e.name AS employee_name, 
                       ws.work_date, 
                       ws.shift_type,
                       ws.start_time, 
                       ws.end_time, 
                       ws.salary
                FROM work_schedule ws
                JOIN employee e ON ws.id = e.employee_id
                WHERE (@from IS NULL OR ws.work_date >= @from)
                  AND (@to IS NULL OR ws.work_date <= @to)
                ORDER BY ws.work_date, e.name;
            ";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("from", NpgsqlTypes.NpgsqlDbType.Date).Value = from ?? (object)DBNull.Value;
                        cmd.Parameters.Add("to", NpgsqlTypes.NpgsqlDbType.Date).Value = to ?? (object)DBNull.Value;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                schedules.Add(new WorkScheduleInfo
                                {
                                    EmployeeName = reader["employee_name"].ToString(),
                                    WorkDate = Convert.ToDateTime(reader["work_date"]).ToShortDateString(),
                                    ShiftType = reader["shift_type"].ToString(),
                                    StartTime = reader["start_time"]?.ToString(),
                                    EndTime = reader["end_time"]?.ToString(),
                                    Salary = Convert.ToDecimal(reader["salary"])
                                });
                            }
                        }
                    }

                }
            

                WorkScheduleGrid.ItemsSource = schedules;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке графика работы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExportWorkScheduleToExcel_Click(object sender, RoutedEventArgs e)
        {
            var data = WorkScheduleGrid.ItemsSource as IEnumerable<WorkScheduleInfo>;
            if (data == null || !data.Any())
            {
                MessageBox.Show("Нет данных для экспорта.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel файл (*.xlsx)|*.xlsx",
                FileName = "График_работы.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("График работы");

                        ws.Cell(1, 1).Value = "Сотрудник";
                        ws.Cell(1, 2).Value = "Дата";
                        ws.Cell(1, 3).Value = "Тип смены";
                        ws.Cell(1, 4).Value = "Начало";
                        ws.Cell(1, 5).Value = "Конец";
                        ws.Cell(1, 6).Value = "Зарплата";

                        int row = 2;
                        foreach (var record in data)
                        {
                            ws.Cell(row, 1).Value = record.EmployeeName;
                            ws.Cell(row, 2).Value = record.WorkDate;
                            ws.Cell(row, 3).Value = record.ShiftType;
                            ws.Cell(row, 4).Value = record.StartTime;
                            ws.Cell(row, 5).Value = record.EndTime;
                            ws.Cell(row, 6).Value = record.Salary;
                            row++;
                        }

                        ws.Columns().AdjustToContents();
                        workbook.SaveAs(dialog.FileName);
                    }

                    MessageBox.Show("График успешно экспортирован.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при экспорте: " + ex.Message);
                }
            }
        }
        private void SaveBookingChanges_Click(object sender, RoutedEventArgs e)
        {
            var editedBookings = BookingGrid.ItemsSource as IEnumerable<BookingInfo>;
            if (editedBookings == null) return;

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    foreach (var booking in editedBookings)
                    {
                        var cmd = new NpgsqlCommand(
                            @"UPDATE bookings SET price = @price, status = @status WHERE id = @id", conn);
                        cmd.Parameters.AddWithValue("price", booking.Price);
                        cmd.Parameters.AddWithValue("status", booking.Status);
                        cmd.Parameters.AddWithValue("id", booking.Id);

                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Изменения успешно сохранены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class WorkScheduleInfo
        {
            public string EmployeeName { get; set; }
            public string WorkDate { get; set; }
            public string ShiftType { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public decimal Salary { get; set; }
        }

        public class BookingInfo
        {
            public int Id { get; set; }  // Добавим для идентификации
            public string CellNumber { get; set; }
            public string Status { get; set; }
            public string ClientName { get; set; }
            public string AnimalName { get; set; }
            public string DateFrom { get; set; }
            public string DateTo { get; set; }
            public decimal Price { get; set; }  // Добавим цену
        }





    }
}
