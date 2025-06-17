
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using Npgsql;

namespace Зоогостиница_диплом_
{
    public partial class PaymentSummaryWindow : Window
    {
        private readonly string _connectionString;
        private readonly int _bookingId;
        private readonly int _ownerId;

        public PaymentSummaryWindow(string connectionString, int bookingId, int ownerId)
        {
            InitializeComponent();
            _connectionString = connectionString;
            _bookingId = bookingId;
            _ownerId = ownerId;
            LoadPaymentDetails();
        }
        public static void ShowSummaryFromSettlement(Window owner, string connectionString, int bookingId, int ownerId)
{
    var window = new PaymentSummaryWindow(connectionString, bookingId, ownerId);
    window.Owner = owner;
    window.ShowDialog();
}

        private void LoadPaymentDetails()
        {
            decimal stayCost = 0;
            decimal servicesTotal = 0;
            List<string> services = new();

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                // Получаем даты и животное из брони
                var cmdBooking = new NpgsqlCommand(@"
                    SELECT start_date, end_date
                    FROM bookings WHERE id = @id", conn);
                cmdBooking.Parameters.AddWithValue("@id", _bookingId);

                DateTime startDate, endDate;
                using (var reader = cmdBooking.ExecuteReader())
                {
                    if (!reader.Read()) return;
                    startDate = reader.GetDateTime(0);
                    endDate = reader.GetDateTime(1);
                }

                int days = (endDate - startDate).Days + 1;
                stayCost = days switch
                {
                    <= 1 => 150,
                    <= 7 => 1000,
                    _ => 1000 + (days - 7) * 150
                };

                // Получаем услуги по брони
                var cmdServices = new NpgsqlCommand(@"
                    SELECT vs.name, vs.cost
                    FROM service_record sr
                    JOIN veterinary_services vs ON vs.id = sr.veterinary_service_id
                    WHERE sr.booking_id = @id", conn);
                cmdServices.Parameters.AddWithValue("@id", _bookingId);

                using var reader2 = cmdServices.ExecuteReader();
                while (reader2.Read())
                {
                    var name = reader2.GetString(0);
                    var cost = reader2.GetDecimal(1);
                    services.Add($"{name} - {cost} ₽");
                    servicesTotal += cost;
                }

                var total = stayCost + servicesTotal;
                StayCostText.Text = $"Проживание: {stayCost} ₽";
                ServicesList.ItemsSource = services;
                TotalText.Text = $"Итого к оплате: {total} ₽";

                // Проверка оплаты
                var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM payments WHERE booking_id = @id", conn);
                checkCmd.Parameters.AddWithValue("@id", _bookingId);
                var alreadyPaid = (long)checkCmd.ExecuteScalar() > 0;

                if (alreadyPaid)
                {
                    PayButton.IsEnabled = false;
                    PayButton.Content = "Уже оплачено";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки оплаты: " + ex.Message);
            }
        }

        private void PayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var total = TotalText.Text.Split(':').Last().Replace("₽", "").Trim();
                var amount = decimal.Parse(total);

                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    INSERT INTO payments (booking_id, owner_id, total, method)
                    VALUES (@booking, @owner, @amount, 'Наличные')", conn);
                cmd.Parameters.AddWithValue("@booking", _bookingId);
                cmd.Parameters.AddWithValue("@owner", _ownerId);
                cmd.Parameters.AddWithValue("@amount", amount);

                cmd.ExecuteNonQuery();
                MessageBox.Show("Оплата успешно проведена!");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка оплаты: " + ex.Message);
            }
        }
    }
}
