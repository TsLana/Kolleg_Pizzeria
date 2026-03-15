using System;
using System.Windows;
using MySql.Data.MySqlClient;

namespace pizzeria
{
    public partial class LoginWindow : Window
    {
        private const string DefaultPassword = "pizzeria2025";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = FullNameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(fullName))
            {
                ErrorTextBlock.Text = "Введіть ПІБ.";
                return;
            }

            if (password != DefaultPassword)
            {
                ErrorTextBlock.Text = "Невірний пароль.";
                return;
            }

            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string query = @"
                        SELECT e.employee_full_name, p.position_name
                        FROM employee e
                        JOIN position_ p ON e.position_id = p.position_id
                        WHERE e.employee_full_name = @fullName";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@fullName", fullName);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string name = reader.GetString("employee_full_name");
                                string position = reader.GetString("position_name");

                                MainWindow mainWindow = new MainWindow(name, position);
                                mainWindow.Show();
                                this.Close();
                            }
                            else
                            {
                                ErrorTextBlock.Text = "Користувача не знайдено.";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка підключення до бази: " + ex.Message);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
