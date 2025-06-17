using Npgsql;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using static Зоогостиница_диплом_.Settlement_vet;

namespace Зоогостиница_диплом_
{
    public partial class Settlement_vet : Window
    {
        private ObservableCollection<Animal> animals = new();
        private Animal selectedAnimal;
        private string connectionString = "Host=localhost;Port=5434;Username=postgres;Password=1234;Database=diplom";
        private int animalId;
        private object bookingId;

        public Settlement_vet()
        {
            InitializeComponent();
            LoadAnimals();
            AnimalsListBox.ItemsSource = animals;
            LoadVeterinaryServices();
        }

        public class Animal
        {
            public int Id { get; set; }
            public string Nickname { get; set; }
            public string AnimalTypeId { get; set; }
            public string Owner { get; set; }
            public int Age { get; set; }
            public double Weight { get; set; }
            public double Temperature { get; set; }
        }

        private void LoadAnimals()
        {
            animals.Clear();

            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand("SELECT a.id AS animal_id,    a.nickname,    at.type_name AS animal_type, o.first_name || ' ' || o.last_name || ' ' || middle_name AS owner_full_name, a.age,  a.weight,    a.temperature FROM public.animals a LEFT JOIN  public.animal_types at ON a.animal_type_id = at.id LEFT JOIN public.owners o ON a.owner_id = o.id;", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                animals.Add(new Animal
                {
                    Id = reader.GetInt32(0),
                    Nickname = reader.GetString(1),
                    AnimalTypeId = reader.GetString(2),
                    Owner = reader.GetString(3),
                    Age = reader.GetInt32(4),
                    Weight = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                    Temperature = reader.IsDBNull(6) ? 0 : reader.GetDouble(6)
                });
            }
        }

        private void AnimalsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            selectedAnimal = AnimalsListBox.SelectedItem as Animal;
            if (selectedAnimal != null)
            {
                animalId = selectedAnimal.Id; // Добавьте эту строку
                AnimalTypeTextBox.Text = selectedAnimal.AnimalTypeId;
                NicknameTextBox.Text = selectedAnimal.Nickname;
                OwnerTextBox.Text = selectedAnimal.Owner;
                AgeTextBox.Text = selectedAnimal.Age.ToString();
                WeightTextBox.Text = selectedAnimal.Weight.ToString("0.##");
                TemperatureTextBox.Text = selectedAnimal.Temperature.ToString("0.##");
            }
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedAnimal == null) return;

            try
            {
                selectedAnimal.Weight = double.TryParse(WeightTextBox.Text, out var w) ? w : 0;
                selectedAnimal.Temperature = double.TryParse(TemperatureTextBox.Text, out var t) ? t : 0;

                bool isAllowed = AllowedCheckBox.IsChecked ?? false;

                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                var cmd = new NpgsqlCommand(@"
            UPDATE animals SET 
                weight = @weight,
                temperature = @temperature,
                rabies_vaccinated = @rabies,
                rabies_vaccinated_date = @rabies_date,
                viral_diseases = @viruses,
                viral_diseases_date = @viruses_date,
                tick_treatment = @ticks,
                tick_treatment_date = @ticks_date,
                allowed = @allowed
            WHERE id = @id", conn);

                cmd.Parameters.AddWithValue("@weight", selectedAnimal.Weight);
                cmd.Parameters.AddWithValue("@temperature", selectedAnimal.Temperature);
                cmd.Parameters.AddWithValue("@rabies", VaccinationCheckBox.IsChecked ?? false);
                cmd.Parameters.AddWithValue("@rabies_date", (object)VaccinationDatePicker.SelectedDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@viruses", ViralDiseasesCheckBox.IsChecked ?? false);
                cmd.Parameters.AddWithValue("@viruses_date", (object)ViralDiseasesDatePicker.SelectedDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ticks", TickTreatmentCheckBox.IsChecked ?? false);
                cmd.Parameters.AddWithValue("@ticks_date", (object)TickTreatmentDatePicker.SelectedDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@allowed", isAllowed);
                cmd.Parameters.AddWithValue("@id", selectedAnimal.Id);

                cmd.ExecuteNonQuery();

                MessageBox.Show("Данные сохранены.");

                if (isAllowed)
                {

                    if (AllowedCheckBox.IsChecked == true)
                    {
                        int bookingId = GetBookingIdByAnimalId(animalId);
                        int ownerId = GetOwnerIdByAnimalId(animalId);
                        int specialistId = 1; // ты можешь получить его из логина, сессии, и т.п.

                        foreach (ServiceItem selectedService in ServicesListBox.SelectedItems)
                        {
                            var cmd2 = new NpgsqlCommand(@"
                        INSERT INTO service_record (veterinary_service_id, date, specialist_id, animal_id, booking_id)
                        VALUES (@serviceId, CURRENT_DATE, @specialistId, @animalId, @bookingId)", conn);

                            cmd2.Parameters.AddWithValue("@serviceId", selectedService.Id);  // Заменить cmd на cmd2
                            cmd2.Parameters.AddWithValue("@specialistId", specialistId);
                            cmd2.Parameters.AddWithValue("@animalId", animalId);
                            cmd2.Parameters.AddWithValue("@bookingId", bookingId);

                            cmd2.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Отклоняем заселение: обновляем статус брони или заселения
                        var updateCmd = new NpgsqlCommand("UPDATE bookings SET status = 'отклонено' WHERE id = @bookingId", conn);
                        updateCmd.Parameters.AddWithValue("@bookingId", bookingId);
                        updateCmd.ExecuteNonQuery();

                        MessageBox.Show("Животное не допущено к заселению. Бронирование отменено.");
                    }
                    // Обновить список
                    LoadAnimals();
                    AnimalsListBox.SelectedItem = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message);
            }
        }

        private void LoadVeterinaryServices()
        {
            try
            {
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                var cmd = new NpgsqlCommand("SELECT id, name FROM veterinary_services", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ServicesListBox.Items.Add(new ServiceItem
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки услуг: " + ex.Message);
            }
        }

        private class ServiceItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }

        private int GetBookingIdByAnimalId(int animalId)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand(@"
        SELECT id FROM bookings
        WHERE animal_id = @id AND CURRENT_DATE BETWEEN start_date AND end_date
        ORDER BY start_date DESC LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@id", animalId);

            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : -1;
        }

        private int GetOwnerIdByAnimalId(int animalId)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand("SELECT owner_id FROM animals WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", animalId);

            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : -1;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }
    }
}
