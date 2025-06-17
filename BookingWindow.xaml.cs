using System;
using System.Data;
using System.Windows;
using Npgsql;

namespace Зоогостиница_диплом_
{
    public partial class BookingWindow : Window
    {
        private readonly string connectionString;
        private readonly int cellNumber;

        public BookingWindow(string connectionString, int cellNumber)
        {
            InitializeComponent();
            this.connectionString = connectionString;
            this.cellNumber = cellNumber;
            LoadComboBoxes();
            CellNumberTextBox.Text = cellNumber.ToString();
        }
        private void LoadComboBoxes()
        {
            var animalTypes = LoadAnimalTypes();
            animalTypeComboBox.ItemsSource = animalTypes.DefaultView;
            animalTypeComboBox.DisplayMemberPath = "type_name";
            animalTypeComboBox.SelectedValuePath = "id";
            if (animalTypes.Rows.Count > 0)
                animalTypeComboBox.SelectedIndex = 0;
        }
            private DataTable LoadAnimalTypes()
        {
            DataTable dt = new DataTable();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT id, type_name FROM public.animal_types;", conn);
                var adapter = new NpgsqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            return dt;
        }
        private void Reserve_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(OwnerNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(PetNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(BreedTextBox.Text) ||
                string.IsNullOrWhiteSpace(SizeTextBox.Text) ||
                !decimal.TryParse(WeightTextBox.Text, out decimal weight) ||
                !int.TryParse(AgeTextBox.Text, out int age) ||
                StartDatePicker.SelectedDate == null ||
                EndDatePicker.SelectedDate == null ||
                StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
            {
                MessageBox.Show("Пожалуйста, заполните все поля корректно.");
                return;
            }

            try
            {
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                var cmd = new NpgsqlCommand(@"
                    INSERT INTO public.full_bookings
                    (owner_name, pet_name, breed, size, weight, age, cell_number, start_date, end_date,type_animal)
                    VALUES
                    (@ownerName, @petName, @breed, @size, @weight, @age, @cell, @startDate, @endDate,@type_animal)", conn);

                cmd.Parameters.AddWithValue("@ownerName", OwnerNameTextBox.Text);
                cmd.Parameters.AddWithValue("@petName", PetNameTextBox.Text);
                cmd.Parameters.AddWithValue("@breed", BreedTextBox.Text);
                cmd.Parameters.AddWithValue("@size", SizeTextBox.Text);
                cmd.Parameters.AddWithValue("@weight", weight);
                cmd.Parameters.AddWithValue("@age", age);
                cmd.Parameters.AddWithValue("@cell", cellNumber);
                cmd.Parameters.AddWithValue("@startDate", StartDatePicker.SelectedDate.Value);
                cmd.Parameters.AddWithValue("@endDate", EndDatePicker.SelectedDate.Value);
                cmd.Parameters.AddWithValue("@type_animal", animalTypeComboBox.Text);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Бронирование успешно добавлено!");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при бронировании: {ex.Message}");
            }
        }
    }
}
