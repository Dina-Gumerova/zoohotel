using DocumentFormat.OpenXml.Spreadsheet;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ZooHotel;

namespace Зоогостиница_диплом_
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private string connectionString = "Server=localhost;Port=5434;Username=postgres;Password=1234;Database=diplom";

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            Registration RegistrationWindow = new Registration();
            RegistrationWindow.Show();
            this.Close();
        }
        private void OpenDirectorWindow(string username, string role)
        {
            
            DirectorWindow Director = new DirectorWindow(); 
            Director.Show();
            this.Close();
        }
        private void OpenMenegerWindow(string username, string role)
        {
            Booking MenegerWindow = new Booking(); 
            MenegerWindow.Show();
            this.Close();
        }
        private void OpenUserWindow(string username, string role)
        {
            ClientBooking clientWindow = new ClientBooking(username, role);
            clientWindow.Show();
            this.Close();
        }
        private void OpenVeterinarianWindow(string username, string role)
        {
            Settlement_vet veterinar = new Settlement_vet(); 
            veterinar.Show();
            this.Close();
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {

            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;

            var userInfo = GetUserInfo(login, password);
            if (userInfo.Role != null)
            {
                if (userInfo.Role == "директор")
                {
                    OpenDirectorWindow(userInfo.Name, userInfo.Role);
                }
                else if (userInfo.Role == "ветеринар")
                {
                  OpenVeterinarianWindow(userInfo.Name, userInfo.Role);
                }
                else if (userInfo.Role == "менеджер")
                {
                    OpenMenegerWindow(userInfo.Name, userInfo.Role);
                }
                else
                {
                   OpenUserWindow(userInfo.Name, userInfo.Role); 
                }
            }
            else
            {
                MessageTextBlock.Text = "Неверный логин или пароль.";
                MessageTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private (int Id, string Name, string Role) GetUserInfo(string login, string password)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    // Сначала проверяем сотрудника
                    string employeeQuery = "SELECT employee_id, username, role FROM employee WHERE username = @login AND password = @password";

                    using (var employeeCmd = new NpgsqlCommand(employeeQuery, conn))
                    {
                        employeeCmd.Parameters.AddWithValue("login", login);
                        employeeCmd.Parameters.AddWithValue("password", password);

                        using (var reader = employeeCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = Convert.ToInt32(reader["employee_id"]);
                                string username = reader["username"].ToString();
                                string role = reader["role"].ToString();
                                return (userId, username, role);
                            }
                        }
                    }

                    // Если не сотрудник — проверяем среди клиентов (owners)
                    string ownerQuery = "SELECT id, first_name, last_name FROM owners WHERE login = @login AND password = @password";

                    using (var ownerCmd = new NpgsqlCommand(ownerQuery, conn))
                    {
                        ownerCmd.Parameters.AddWithValue("login", login);
                        ownerCmd.Parameters.AddWithValue("password", password);

                        using (var reader = ownerCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = Convert.ToInt32(reader["id"]);
                                string fullName = reader["first_name"].ToString() + " " + reader["last_name"].ToString();
                                return (userId, fullName, "пользователь"); // Обозначим роль как "пользователь"
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}");
            }

            return (0, null, null);
        }

    }
}
