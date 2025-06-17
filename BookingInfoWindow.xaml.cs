using System;
using System.Collections.Generic;
using System.Windows;
using Npgsql;

namespace Зоогостиница_диплом_
{
    public partial class BookingInfoWindow : Window
    {
        private readonly string connectionString;
        private readonly FullBooking booking;
        private readonly FullBooking full_bookings;

        public class FullBooking
        {
            public int BookingId { get; set; } 
            public string OwnerName { get; set; }
            public string PetName { get; set; }
            public string Breed { get; set; }
            public string Size { get; set; }
            public decimal Weight { get; set; }
            public int Age { get; set; }
            public int Сell_number { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Type_animal { get; internal set; }
        }

        public BookingInfoWindow(string connectionString, FullBooking booking)
        {
            InitializeComponent();
            this.connectionString = connectionString;
            this.booking = booking;

            ShowBookingDetails();
        }

        private void ShowBookingDetails()
        {
            OwnerText.Text = $"Владелец: {booking.OwnerName}";
            PetText.Text = $"Питомец: {booking.PetName}";
            TypeanimalText.Text = $"Вид питомца: {booking.Type_animal}";
            BreedText.Text = $"Порода: {booking.Breed}";
            SizeText.Text = $"Размер: {booking.Size}";
            WeightText.Text = $"Вес: {booking.Weight} кг";
            AgeText.Text = $"Возраст: {booking.Age} лет";
            DatesText.Text = $"С {booking.StartDate:dd.MM.yyyy} по {booking.EndDate:dd.MM.yyyy}";
        }

        private void Settle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settlementWindow = new Settlement(
                 connectionString: connectionString,
                 bookingData: null,
                 cellNumber: booking.Сell_number

             );
                settlementWindow.Owner = this;
                settlementWindow.ShowDialog();
                this.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка перехода к заселению: " + ex.Message);
            }
        }


        public static List<FullBooking> LoadFullBookings(string connectionString, DateTime date)
        {
            var bookings = new List<FullBooking>();

            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT id, owner_name, pet_name, breed, size, weight, age, cell_number, start_date, end_date,type_animal
                FROM full_bookings
                WHERE @date BETWEEN start_date AND end_date", conn);
            cmd.Parameters.AddWithValue("@date", date);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                bookings.Add(new FullBooking
                {
                    BookingId = reader.GetInt32(0),
                    OwnerName = reader.GetString(1),
                    PetName = reader.GetString(2),
                    Breed = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Size = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Weight = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                    Age = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                    Сell_number = reader.GetInt32(7),
                    StartDate = reader.GetDateTime(8),
                    EndDate = reader.GetDateTime(9),
                    Type_animal = reader.GetString(10)
                });
            }
            return bookings;
        }
    }
}
