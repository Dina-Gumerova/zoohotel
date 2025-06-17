using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;
using DocumentFormat.OpenXml.Packaging;
using System.Linq;
using static Зоогостиница_диплом_.Settlement_vet;
using static Зоогостиница_диплом_.BookingInfoWindow;

namespace Зоогостиница_диплом_
{

    public partial class Settlement : Window
    {
        private int _value = 0;
        private int _min = 0;
        private int _max = 100;

        // Строка подключения к базе данных PostgreSQL
        string connectionString = "Server=localhost;Port=5434;Username=postgres;Password=1234;Database=diplom";
        private int predefinedCellNumber;
        private int cellNumber;
        private string userRole;


        public string UserRole { get; }
        private int? _predefinedCellNumber;
        
        private BookingInfoWindow.FullBooking _booking;
    

        private FullBooking _bookingData;

        public Settlement(string connectionString, FullBooking bookingData = null, int? cellNumber = null)
        {
            InitializeComponent();
            this.connectionString = connectionString;
            this._bookingData = bookingData;

            this.connectionString=connectionString;
            this.booking=booking;
            try
            {
                LoadComboBoxes();
                LoadPolTypes();
                LoadOwners();

                if (cellNumber.HasValue)
                {
                    CellNumberTextBox.Text = cellNumber.Value.ToString();
                    CellNumberTextBox.IsEnabled = true;
                }

                if (_bookingData != null)
                {
                    FillBookingData();
                }

                NumberTextBox.Text = "0";
                Number2TextBox.Text = "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при инициализации окна заселения: " + ex.Message);
            }
        }

       

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_bookingData != null)
            {
                LoadComboBoxes();
                LoadPolTypes();
                LoadOwners();
            }
        }

        private void LoadAnimalData(int animalId)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
        SELECT a.nickname, a.breed, a.size, a.weight, a.age,
               o.first_name || ' ' || o.last_name AS owner_name
        FROM animals a
        JOIN owners o ON a.owner_id = o.id
        WHERE a.id = @id", conn);

            cmd.Parameters.AddWithValue("@id", animalId);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                animalnametextbox.Text = reader.GetString(0);
                breedtextbox.Text = reader.IsDBNull(1) ? "" : reader.GetString(1);
                SizeTextBox.Text = reader.IsDBNull(2) ? "" : reader.GetString(2);
                WeightTextBox.Text = reader.IsDBNull(3) ? "0" : reader.GetDouble(3).ToString("0.##");
                AgeTextBox.Text = reader.IsDBNull(4) ? "0" : reader.GetInt32(4).ToString();
                surNameTextBox.Text = reader.IsDBNull(5) ? "" : reader.GetString(5);
            }
        }
        private void FillBookingData()
        {
            if (_bookingData == null) return;

            animalnametextbox.Text = _bookingData.PetName;
            breedtextbox.Text = _bookingData.Breed;
            SizeTextBox.Text = _bookingData.Size;
            WeightTextBox.Text = _bookingData.Weight.ToString("0.##");
            AgeTextBox.Text = _bookingData.Age.ToString();
            CellNumberTextBox.Text = _bookingData.Сell_number.ToString();
            StartDatePicker.SelectedDate = _bookingData.StartDate;
            EndDatePicker.SelectedDate = _bookingData.EndDate;

            animalnametextbox.IsEnabled = false;
            breedtextbox.IsEnabled = false;
            SizeTextBox.IsEnabled = false;
            WeightTextBox.IsEnabled = false;
            AgeTextBox.IsEnabled = false;
        }

        private int _foodAmount = 0;
        private int _walksPerDay = 0;
        private string v;
        private BookingInfoWindow.FullBooking booking;
        private int animalId;

        // Загрузка владельцев из базы в ownerComboBox
        private void LoadOwners()
        {
            DataTable dt = new DataTable();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT id, last_name || ' ' || first_name || ' ' || middle_name AS full_name FROM owners ORDER BY last_name;", conn);
                var adapter = new NpgsqlDataAdapter(cmd);
                adapter.Fill(dt);
            }

            ownerComboBox.ItemsSource = dt.DefaultView;
            ownerComboBox.DisplayMemberPath = "full_name";
            ownerComboBox.SelectedValuePath = "id";

            if (dt.Rows.Count > 0)
                ownerComboBox.SelectedIndex = 0;
        }

        // Загрузка питомцев выбранного владельца в petsComboBox
        private void ownerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ownerComboBox.SelectedValue == null)
            {
                petsComboBox.ItemsSource = null;
                petsComboBox.SelectedIndex = -1;
                return;
            }

            if (int.TryParse(ownerComboBox.SelectedValue.ToString(), out int ownerId))
            {
                LoadPetsForOwner(ownerId);
                LoadOwnerDetails(ownerId);
            }
        }

        private void LoadOwnerDetails(int ownerId)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT first_name, last_name, middle_name, contact_info, passport_data FROM owners WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", ownerId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        NameTextBox.Text = reader["first_name"]?.ToString();
                        surNameTextBox.Text = reader["last_name"]?.ToString();
                        patronymicTextBox.Text = reader["middle_name"]?.ToString();
                        PhoneemailTextBox.Text = reader["contact_info"]?.ToString();
                        pasportTextBox.Text = reader["passport_data"]?.ToString();
                    }
                }
            }
        }
        private void LoadPetsForOwner(int ownerId)
        {
            DataTable dt = new DataTable();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT id, nickname FROM animals WHERE owner_id = @ownerId ORDER BY nickname;", conn);
                cmd.Parameters.AddWithValue("ownerId", ownerId);
                var adapter = new NpgsqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            petsComboBox.ItemsSource = dt.DefaultView;
            petsComboBox.DisplayMemberPath = "nickname";
            petsComboBox.SelectedValuePath = "id";
            if (dt.Rows.Count > 0)
                petsComboBox.SelectedIndex = 0;
            else
                petsComboBox.SelectedIndex = -1;
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
        private void LoadPolTypes()
        {
            var polTypes = new List<string> { "Мужской", "Женский" };
            polTypeComboBox.ItemsSource = polTypes;
            polTypeComboBox.SelectedIndex = 0; // по умолчанию "Мужской"
        }
    
        private DataTable LoadFeedingTypes()
        {
            DataTable dt = new DataTable();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT id, feeding_type FROM public.feeding_types;", conn);
                var adapter = new NpgsqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            return dt;
        }
        private void LoadComboBoxes()
        {
            var animalTypes = LoadAnimalTypes();
            animalTypeComboBox.ItemsSource = animalTypes.DefaultView;
            animalTypeComboBox.DisplayMemberPath = "type_name";
            animalTypeComboBox.SelectedValuePath = "id";
            if (animalTypes.Rows.Count > 0)
                animalTypeComboBox.SelectedIndex = 0;

            var feedingTypes = LoadFeedingTypes();
            feedingTypeComboBox.ItemsSource = feedingTypes.DefaultView;
            feedingTypeComboBox.DisplayMemberPath = "feeding_type";
            feedingTypeComboBox.SelectedValuePath = "id";

        }
        private void Increase_Click(object sender, RoutedEventArgs e)
        {
            if (_foodAmount < _max)
            {
                _foodAmount++;
                NumberTextBox.Text = _foodAmount.ToString();
            }
        }
        private void Increase_Click2(object sender, RoutedEventArgs e)
        {
            if (_walksPerDay < _max)
            {
                _walksPerDay++;
                Number2TextBox.Text = _walksPerDay.ToString();
            }
        }
        private void Decrease_Click(object sender, RoutedEventArgs e)
        {
            if (_value > _min)
            {
                _value--;
                NumberTextBox.Text = _value.ToString();
            }
        }
        private void NumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void Decrease_Click2(object sender, RoutedEventArgs e)
        {
            if (_value > _min)
            {
                _value--;
                Number2TextBox.Text = _value.ToString();
            }
        }
        private void NumberTextBox_PreviewTextInput2(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // === 1. Сохраняем владельца и животное ===

                string gender = (string)(polTypeComboBox.SelectedItem ?? "Мужской");

                string ownerFirstName = NameTextBox.Text.Trim();
                string ownerLastName = surNameTextBox.Text.Trim();
                string ownerMiddleName = patronymicTextBox.Text.Trim();
                string ownerPhone = PhoneemailTextBox.Text.Trim();
                string passport = pasportTextBox.Text.Trim();

                if (string.IsNullOrEmpty(ownerFirstName) || string.IsNullOrEmpty(ownerLastName))
                {
                    MessageBox.Show("Пожалуйста, заполните имя и фамилию владельца");
                    return;
                }

                DateTime birthDate = BirthdayDatePicker.SelectedDate ?? DateTime.Now;

                int ownerId;
                string login = (ownerFirstName + ownerLastName).ToLower();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    // Проверка владельца
                    var checkOwnerCmd = new NpgsqlCommand(@"
                SELECT id FROM owners 
                WHERE first_name = @first AND last_name = @last AND passport_data = @passport", conn);
                    checkOwnerCmd.Parameters.AddWithValue("first", ownerFirstName);
                    checkOwnerCmd.Parameters.AddWithValue("last", ownerLastName);
                    checkOwnerCmd.Parameters.AddWithValue("passport", passport);

                    var ownerResult = checkOwnerCmd.ExecuteScalar();
                    if (ownerResult != null)
                    {
                        ownerId = Convert.ToInt32(ownerResult);
                    }
                    else
                    {
                        var insertOwnerCmd = new NpgsqlCommand(@"
                    INSERT INTO owners (first_name, last_name, middle_name, contact_info, passport_data, login, password) 
                    VALUES (@first, @last, @middle, @contact, @passport, @login, @pass) RETURNING id;", conn);
                        insertOwnerCmd.Parameters.AddWithValue("first", ownerFirstName);
                        insertOwnerCmd.Parameters.AddWithValue("last", ownerLastName);
                        insertOwnerCmd.Parameters.AddWithValue("middle", ownerMiddleName);
                        insertOwnerCmd.Parameters.AddWithValue("contact", ownerPhone);
                        insertOwnerCmd.Parameters.AddWithValue("passport", passport);
                        insertOwnerCmd.Parameters.AddWithValue("login", login);
                        insertOwnerCmd.Parameters.AddWithValue("pass", "default");
                        ownerId = (int)insertOwnerCmd.ExecuteScalar();
                    }

                    // Данные животного
                    string nickname = animalnametextbox.Text.Trim();
                    string breed = breedtextbox.Text.Trim();
                    string size = SizeTextBox.Text.Trim();
                    double weight = double.TryParse(WeightTextBox.Text, out var w) ? w : 0;
                    int age = int.TryParse(AgeTextBox.Text, out var a) ? a : 0;
                    string color = OKRACtEXTBOX.Text.Trim();
                    int animalTypeId = (int)(animalTypeComboBox.SelectedValue ?? 1);
                    int feedingTypeId = (int)(feedingTypeComboBox.SelectedValue ?? 1);

                    int animalId;

                    var checkAnimalCmd = new NpgsqlCommand(@"
                SELECT id FROM animals WHERE nickname = @nickname AND owner_id = @ownerId", conn);
                    checkAnimalCmd.Parameters.AddWithValue("nickname", nickname);
                    checkAnimalCmd.Parameters.AddWithValue("ownerId", ownerId);

                    var animalResult = checkAnimalCmd.ExecuteScalar();
                    if (animalResult != null)
                    {
                        animalId = Convert.ToInt32(animalResult);
                    }
                    else
                    {
                        var insertAnimalCmd = new NpgsqlCommand(@"
                    INSERT INTO animals (nickname, breed, size, weight, age, color, gender, birth_date, animal_type_id, feeding_type_id, owner_id) 
                    VALUES (@nickname, @breed, @size, @weight, @age, @color, @gender, @birthDate, @animalTypeId, @feedingTypeId, @ownerId) 
                    RETURNING id;", conn);
                        insertAnimalCmd.Parameters.AddWithValue("nickname", nickname);
                        insertAnimalCmd.Parameters.AddWithValue("breed", breed);
                        insertAnimalCmd.Parameters.AddWithValue("size", size);
                        insertAnimalCmd.Parameters.AddWithValue("weight", weight);
                        insertAnimalCmd.Parameters.AddWithValue("age", age);
                        insertAnimalCmd.Parameters.AddWithValue("color", color);
                        insertAnimalCmd.Parameters.AddWithValue("gender", gender);
                        insertAnimalCmd.Parameters.AddWithValue("birthDate", birthDate);
                        insertAnimalCmd.Parameters.AddWithValue("animalTypeId", animalTypeId);
                        insertAnimalCmd.Parameters.AddWithValue("feedingTypeId", feedingTypeId);
                        insertAnimalCmd.Parameters.AddWithValue("ownerId", ownerId);
                        animalId = (int)insertAnimalCmd.ExecuteScalar();
                    }

                    // === 2. Вставка в bookings ===

                    int cellNumber = int.Parse(CellNumberTextBox.Text);
                    DateTime startDate = StartDatePicker.SelectedDate ?? DateTime.Now;
                    DateTime endDate = EndDatePicker.SelectedDate ?? DateTime.Now.AddDays(1);

                    var insertBookingCmd = new NpgsqlCommand(@"
                INSERT INTO bookings (animal_id, start_date, end_date, cell_number, status, price) 
                VALUES (@animalId, @start, @end, @cellId, @status, @price)", conn);
                    insertBookingCmd.Parameters.AddWithValue("animalId", animalId);
                    insertBookingCmd.Parameters.AddWithValue("start", startDate);
                    insertBookingCmd.Parameters.AddWithValue("end", endDate);
                    insertBookingCmd.Parameters.AddWithValue("cellId", cellNumber);
                    insertBookingCmd.Parameters.AddWithValue("status", "occupied");
                    insertBookingCmd.Parameters.AddWithValue("price", 100);
                    insertBookingCmd.ExecuteNonQuery();

                    // Удаление из full_bookings
                    if (_bookingData != null)
                    {
                        var deleteCmd = new NpgsqlCommand("DELETE FROM full_bookings WHERE id = @id", conn);
                        deleteCmd.Parameters.AddWithValue("id", _bookingData.BookingId);
                        deleteCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Животное успешно заселено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                
                LoadOwners();

                // 🔁 ВАЖНО: обновим Booking
                var bookingWindow = Application.Current.Windows.OfType<Booking>().FirstOrDefault();
                if (bookingWindow != null)
                {
                    bookingWindow.RefreshCells(DateTime.Now);
                }

                this.Close(); // Закроем Settlement после сохранения

            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при заселении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        private void GenerateContract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string templatePath = @"C:\Users\Asus\Downloads\Зоогостиница(диплом)\Dogovor-vremennogo-soderzhaniya-zhivotnogo.docx";
                string savePath = @"C:\Users\Asus\OneDrive\Рабочий стол\диплом\договор пример.docx";

                if (!File.Exists(templatePath))
                {
                    MessageBox.Show("Шаблон договора не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                File.Copy(templatePath, savePath, true); // копируем шаблон

                using (WordprocessingDocument doc = WordprocessingDocument.Open(savePath, true))
                {
                    string docText;

                    using (StreamReader sr = new StreamReader(doc.MainDocumentPart.GetStream()))
                    {
                        docText = sr.ReadToEnd();
                    }

                    // Замены для владельца
                    docText = docText.Replace("{{Фамилия_Владельца}}", surNameTextBox.Text);
                    docText = docText.Replace("{{Имя_Владельца}}", NameTextBox.Text);
                    docText = docText.Replace("{{Отчество_Владельца}}", patronymicTextBox.Text);
                    docText = docText.Replace("{{Паспорт_Владельца}}", pasportTextBox.Text);
                    docText = docText.Replace("{{Телефон_Владельца}}", PhoneemailTextBox.Text);

                    // Замены для животного
                    docText = docText.Replace("{{Вид_животного}}", animalTypeComboBox.Text);
                    docText = docText.Replace("{{Порода}}", breedtextbox.Text);
                    docText = docText.Replace("{{Пол_животного}}", polTypeComboBox.Text);
                    docText = docText.Replace("{{Кличка}}", animalnametextbox.Text);

                    // Возраст и дата рождения
                    if (BirthdayDatePicker.SelectedDate.HasValue)
                    {
                        DateTime birthDate = BirthdayDatePicker.SelectedDate.Value;
                        int age = DateTime.Now.Year - birthDate.Year;
                        if (DateTime.Now < birthDate.AddYears(age)) age--;
                        docText = docText.Replace("{{Возраст}}", age.ToString());
                        docText = docText.Replace("{{Дата_рождения}}", birthDate.ToShortDateString());
                    }
                    else
                    {
                        docText = docText.Replace("{{Возраст}}", "—");
                        docText = docText.Replace("{{Дата_рождения}}", "—");
                    }

                    // Размещение
                    docText = docText.Replace("{{Тип_питания}}", feedingTypeComboBox.Text);
                    docText = docText.Replace("{{Количество_прогулок}}", Number2TextBox.Text);
                    docText = docText.Replace("{{Количество_корма}}", NumberTextBox.Text);

                    // Дата договора
                    docText = docText.Replace("{{Дата_договора}}", DateTime.Now.ToShortDateString());

                    using (StreamWriter sw = new StreamWriter(doc.MainDocumentPart.GetStream(FileMode.Create)))
                    {
                        sw.Write(docText);
                    }
                    if (string.IsNullOrWhiteSpace(NameTextBox.Text) || string.IsNullOrWhiteSpace(animalnametextbox.Text))
                    {
                        MessageBox.Show("Пожалуйста, заполните имя владельца и кличку животного перед созданием договора.");
                        return;
                    }
                }

                MessageBox.Show("Договор успешно создан:\n" + savePath, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании договора: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void petsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)

        {
            if (petsComboBox.SelectedValue == null) return;

            if (int.TryParse(petsComboBox.SelectedValue.ToString(), out int petId))
            {
                LoadPetDetails(petId);
            }
        }

        private void LoadPetDetails(int petId)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                var cmd = new NpgsqlCommand(@" SELECT a.nickname, a.breed, a.color, a.gender, a.birth_date, a.age, a.animal_type_id, a.feeding_type_id, a.walks_per_day, a.daily_food_amount, b.accommodation_place_id,
    c.first_name, c.last_name, c.contact_info FROM animals a   LEFT JOIN bookings b ON b.animal_id = a.id   LEFT JOIN owners c ON c.id = a.owner_id  WHERE a.id = @petId  ORDER BY b.start_date DESC  LIMIT 1;", conn);
                cmd.Parameters.AddWithValue("petId", petId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Животное
                        animalnametextbox.Text = reader["nickname"]?.ToString();
                        breedtextbox.Text = reader["breed"]?.ToString();
                        OKRACtEXTBOX.Text = reader["color"]?.ToString();
                        polTypeComboBox.SelectedItem = reader["gender"]?.ToString();

                        if (DateTime.TryParse(reader["birth_date"]?.ToString(), out DateTime birthDate))
                            BirthdayDatePicker.SelectedDate = birthDate;

                        animalTypeComboBox.SelectedValue = Convert.ToInt32(reader["animal_type_id"]);
                        feedingTypeComboBox.SelectedValue = Convert.ToInt32(reader["feeding_type_id"]);


                        if (decimal.TryParse(reader["daily_food_amount"]?.ToString(), out decimal food))
                            NumberTextBox.Text = food.ToString("0.##");

                        if (int.TryParse(reader["walks_per_day"]?.ToString(), out int walks))
                            Number2TextBox.Text = walks.ToString();
                    }
                }
            }

            animalFormPanel.Visibility = Visibility.Visible;
        }
      


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new Booking();
            menu.Show();
            this.Close();
        }

        private void AddNewOwner_Click(object sender, RoutedEventArgs e)
        {
            ownerFormPanel.Visibility = Visibility.Visible;
            ownerComboBox.SelectedIndex = -1;
            ownerComboBox.Visibility = Visibility.Collapsed;

            // Очистка данных владельца
            surNameTextBox.Text = "";
            NameTextBox.Text = "";
            patronymicTextBox.Text = "";
            PhoneemailTextBox.Text = "";
            pasportTextBox.Text = "";

            // Очистка питомца
            petsComboBox.SelectedIndex = -1;
            animalnametextbox.Text = "";
            breedtextbox.Text = "";
            OKRACtEXTBOX.Text = "";
            polTypeComboBox.SelectedIndex = -1;
            BirthdayDatePicker.SelectedDate = null;
            animalTypeComboBox.SelectedIndex = -1;
            feedingTypeComboBox.SelectedIndex = -1;
            NumberTextBox.Text = "0";
            Number2TextBox.Text = "0";
            animalFormPanel.Visibility = Visibility.Visible;
        }

        private void AddNewPet_Click(object sender, RoutedEventArgs e)
        {
            animalFormPanel.Visibility = Visibility.Visible;
        }
        private void PhoneNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            if (regex.IsMatch(e.Text))
            {
                e.Handled = true;
                return;
            }

            TextBox textBox = sender as TextBox;
            string currentText = textBox.Text.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "");

            if (currentText.Length >= 12)
            {
                e.Handled = true;
                return;
            }

            if (char.IsDigit(e.Text, 0))
            {
                if (currentText.Length == 0)
                {
                    textBox.Text = "+7 (";
                }
                else if (currentText.Length == 5)
                {
                    textBox.Text += ") ";
                }
                else if (currentText.Length == 8)
                {
                    textBox.Text += "-";
                }
                else if (currentText.Length == 10)
                {
                    textBox.Text += "-";
                }

                textBox.Text += e.Text;
                textBox.SelectionStart = textBox.Text.Length;
                e.Handled = true;
            }
        }
    }
}
