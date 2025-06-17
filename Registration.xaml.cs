using System;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using System.Net;

namespace Зоогостиница_диплом_
{
    /// <summary>
    /// Логика взаимодействия для Registration.xaml
    /// </summary>
    public partial class Registration : Window
    {
        private string connectionString = "Server=localhost;Port=5434;Username=postgres;Password=1234;Database=diplom";


        public Registration()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;
            string confirmPassword = Password2Box.Password; // Получаем значение из второго поля
            string surname = surNameTextBox.Text;
            string name = NameTextBox.Text;
            string patronymic = patronymicTextBox.Text;
            string phone_number = PhoneTextBox.Text;
            string address = addressTextBox.Text;
            string passport = passportTextBox.Text;

            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword) ||
                string.IsNullOrWhiteSpace(surname) ||
                string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(patronymic) ||
                string.IsNullOrWhiteSpace(phone_number) ||
                string.IsNullOrWhiteSpace(address) ||
                string.IsNullOrWhiteSpace(passport) ||
                BirthdayDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return; // Выход из метода, если есть пустые поля
            }

            // Удаление лишних символов из номера телефона
            phone_number = phone_number.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "");

            // Добавление +7 в начале, если номер не начинается с +7
            if (!phone_number.StartsWith("+7"))
            {
                phone_number = "+7" + phone_number;
            }

            // Проверка на совпадение паролей
            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают! Пожалуйста, попробуйте еще раз.");
                return; // Выход из метода, если пароли не совпадают
            }

            if (AddUserToDatabase(username, password, surname, name, patronymic, phone_number, BirthdayDatePicker.SelectedDate, address, passport))
            {
                MessageBox.Show("Регистрация успешна!");
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Ошибка регистрации!");
            }
        }


        // Маска ввода номера телефона
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


        private void ShowPasswordCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            PasswordTextBox.Text = PasswordBox.Password;
            PasswordTextBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }

        private void ShowPasswordCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordBox.Password = PasswordTextBox.Text;
            PasswordTextBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
        }

        private void ShowPassword2CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Password2TextBox.Text = Password2Box.Password;
            Password2TextBox.Visibility = Visibility.Visible;
            Password2Box.Visibility = Visibility.Collapsed;
        }

        private void ShowPassword2CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Password2Box.Password = Password2TextBox.Text;
            Password2TextBox.Visibility = Visibility.Collapsed;
            Password2Box.Visibility = Visibility.Visible;
        }

        private void BirthdayDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BirthdayDatePicker.SelectedDate.HasValue)
            {
                DateTime birthDate = BirthdayDatePicker.SelectedDate.Value;
                int age = DateTime.Now.Year - birthDate.Year;

                // Проверка на день рождения, чтобы учесть, что еще не наступил год.
                if (DateTime.Now < birthDate.AddYears(age))
                {
                    age--;
                }

                // Проверка на допустимый возраст
                if (age < 18 || age > 80)
                {
                    AgeValidationMessage.Text = "Возраст должен быть от 18 до 80 лет.";
                }
                else
                {
                    AgeValidationMessage.Text = string.Empty; // Очистить сообщение, если возраст допустим
                }
            }
            else
            {
                AgeValidationMessage.Text = string.Empty; // Очистить сообщение, если дата не выбрана
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private bool AddUserToDatabase(string surname, string firstName, string patronymic, string username, string password, string phone_number, DateTime? birthday,string address, object passport)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO owners (id, last_name, first_name, middle_name,login, password, contact_info, birthday,address,passport_data) " +
                                   "VALUES (nextval('id'), @surname, @name, @patronymic, @username, @password, @phone_number, @birthday,@address,@passport_data)";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("surname", surname);
                        cmd.Parameters.AddWithValue("name", firstName);
                        cmd.Parameters.AddWithValue("patronymic", patronymic);
                        cmd.Parameters.AddWithValue("username", username);
                        cmd.Parameters.AddWithValue("password", password);
                        cmd.Parameters.AddWithValue("phone_number", phone_number);
                        cmd.Parameters.AddWithValue("birthday", birthday.HasValue ? (object)birthday.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("address", address);
                        cmd.Parameters.AddWithValue("passport_data", passport);
                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении к базе данных: {ex.Message}");
                return false;
            }
        }

      
    }
}
