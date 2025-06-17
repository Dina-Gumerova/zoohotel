using System;
using System.Linq;
using System.Text;
using System.Windows;

namespace Зоогостиница_диплом_
{
    public partial class CellInfoWindow : Window
    {
        public CellInfoWindow()
        {

            InitializeComponent();
        }
        private string _connectionString;
        private int _animalId;

      
        private void Evict_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_animalId <= 0 || string.IsNullOrWhiteSpace(_connectionString))
                {
                    MessageBox.Show("Не удалось определить животное или подключение.");
                    return;
                }

                DateTime evictionDate = EvictionDatePicker.SelectedDate ?? DateTime.Now;

                using var conn = new Npgsql.NpgsqlConnection(_connectionString);
                conn.Open();

                // Завершаем бронирование — обнуляем дату окончания
                var cmd = new Npgsql.NpgsqlCommand(
                    @"UPDATE bookings 
              SET end_date = @evictionDate 
              WHERE animal_id = @animalId AND end_date > @evictionDate;", conn);

                cmd.Parameters.AddWithValue("evictionDate", evictionDate);
                cmd.Parameters.AddWithValue("animalId", _animalId);

                int rows = cmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    MessageBox.Show("Животное выселено досрочно.");

                    // Обновим окно Booking
                    var bookingWindow = Application.Current.Windows.OfType<Booking>().FirstOrDefault();
                    bookingWindow?.RefreshCells(DateTime.Now.Date);

                    this.Close();
                }
                else
                {
                    MessageBox.Show("Запись о бронировании не найдена или уже завершена.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка выселения: " + ex.Message);
            }
        }
        private void PayBooking_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_animalId <= 0 || string.IsNullOrWhiteSpace(_connectionString))
                {
                    MessageBox.Show("Не удалось определить животное или подключение.");
                    return;
                }

                // Получим ID бронирования для текущего животного
                using var conn = new Npgsql.NpgsqlConnection(_connectionString);
                conn.Open();

                var cmd = new Npgsql.NpgsqlCommand(@"
            SELECT id, animal_id 
            FROM bookings 
            WHERE animal_id = @animalId AND start_date <= @today AND end_date >= @today
            LIMIT 1", conn);

                cmd.Parameters.AddWithValue("animalId", _animalId);
                cmd.Parameters.AddWithValue("today", DateTime.Now.Date);

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    int bookingId = reader.GetInt32(0);
                    int ownerId = reader.GetInt32(1);

                    // Открываем окно оплаты
                    PaymentSummaryWindow.ShowSummaryFromSettlement(this, _connectionString, bookingId, ownerId);
                }
                else
                {
                    MessageBox.Show("Активное бронирование для этого животного не найдено.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии оплаты: " + ex.Message);
            }
        }

        // Метод для загрузки информации о животном по его Id
        public void LoadAnimalInfo(int animalId, string connectionString)
        {
            _connectionString = connectionString;
            _animalId = animalId;
            try
            {
                using (var conn = new Npgsql.NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                                                  SELECT 
                                                    a.nickname, 
                                                    a.owner_id, 
                                                    a.breed, 
                                                    a.size, 
                                                    a.weight, 
                                                    a.behavior_description, 
                                                    a.color, 
                                                    a.gender, 
                                                    a.birth_date, 
                                                    a.employee_id as animalemployee,
                                                    a.daily_food_amount, 
                                                    a.walks_per_day, 
                                                    ow.first_name || ' ' || ow.last_name AS owner_full_name, 
                                                    at.type_name AS animal_type, 
                                                    ft.feeding_type, 

                                                    booking_info.last_place,
                                                    booking_info.start_date,
                                                    booking_info.end_date,

                                                    CASE 
                                                        WHEN EXISTS (
                                                            SELECT 1
                                                            FROM bookings b 
                                                            WHERE b.animal_id = a.id AND b.start_date <= @date AND b.end_date >= @date
                                                        ) THEN 'Занят' 
                                                        ELSE 'Свободен' 
                                                    END AS current_status,

                                                    e.name || ' ' || e.surname || ' ' || e.patronymic AS employee_full_name

                                                FROM animals a
                                                LEFT JOIN owners ow ON ow.id = a.owner_id
                                                LEFT JOIN animal_types at ON at.id = a.animal_type_id
                                                LEFT JOIN feeding_types ft ON ft.id = a.feeding_type_id

                                                LEFT JOIN (
                                                    SELECT DISTINCT ON (b.animal_id) 
                                                           b.animal_id, 
                                                           b.start_date, 
                                                           b.end_date,
                                                           ap.name AS last_place 
                                                    FROM bookings b
                                                    JOIN accommodation_places ap ON ap.id = b.accommodation_place_id
                                                    WHERE b.start_date <= @date
                                                    ORDER BY b.animal_id, b.start_date DESC
                                                ) booking_info ON booking_info.animal_id = a.id

                                                LEFT JOIN employee e ON e.employee_id = a.employee_id 

                                                WHERE a.id = @animalId;
                                                    ";

                    var cmd = new Npgsql.NpgsqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@animalId", animalId); 
                    cmd.Parameters.AddWithValue("@date", DateTime.Now.Date);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var sb = new StringBuilder();  
                            sb.AppendLine($"Тип животного: {reader["animal_type"]}");
                            sb.AppendLine($"Кличка: {reader["nickname"]}"); 
                            sb.AppendLine($"Порода: {reader["breed"]}");
                            sb.AppendLine($"Размер: {reader["size"]}");      
                            sb.AppendLine($"Окрас: {reader["color"]}") ;
                            sb.AppendLine($"Вес: {reader["weight"]} кг");  
                            sb.AppendLine($"Пол: {reader["gender"]}");
                            sb.AppendLine($"Сотрудник: {reader["employee_full_name"]}");
                            sb.AppendLine($"Владелец: {reader["owner_full_name"]}");            
                            sb.AppendLine($"Описание поведения: {reader["behavior_description"]}");
                            sb.AppendLine($"Дата рождения: {Convert.ToDateTime(reader["birth_date"]).ToString("d")}");
                            sb.AppendLine($"Количество корма в день: {reader["daily_food_amount"]}");
                            sb.AppendLine($"Количество прогулок в день: {reader["walks_per_day"]}");
                            sb.AppendLine($"Тип питания: {reader["feeding_type"]}");
                            if (reader["last_place"] != DBNull.Value)
                            sb.AppendLine($"Последнее место проживания: {reader["last_place"]}");

                            if (reader["start_date"] != DBNull.Value && reader["end_date"] != DBNull.Value)
                            {
                                var start = Convert.ToDateTime(reader["start_date"]);
                                var end = Convert.ToDateTime(reader["end_date"]);
                                sb.AppendLine($"Период бронирования: с {start:dd.MM.yyyy} по {end:dd.MM.yyyy}");
                            }
                            else
                            {
                                sb.AppendLine("Нет активного бронирования.");
                            }

                            sb.AppendLine($"Статус клетки: {reader["current_status"]}");


                            InfoTextBox.Text = sb.ToString();
                        }
                        else
                        {
                            InfoTextBox.Text = "Информация о животном не найдена.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении информации: " + ex.Message);
            }
        }

    }
}