using System;
using System.Windows;
using MySql.Data.MySqlClient;

namespace pizzeria
{
    public partial class AddClientWindow : Window
    {
        public AddClientWindow(string initialFullName)
        {
            InitializeComponent();
            FullNameTextBox.Text = initialFullName;  
        }

        public AddClientWindow() : this("")
        {
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = FullNameTextBox.Text.Trim();
            string phone = PhoneTextBox.Text.Trim();

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("Будь ласка, заповніть усі обов’язкові поля.");
                return;
            }

            if (phone.Length > 10 || !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\d{1,10}$"))
            {
                MessageBox.Show("Номер телефону повинен містити максимум 10 цифр і не містити інших символів.");
                return;
            }

            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open(); 

                    string query = "INSERT INTO client_ (client_full_name, client_phone_number, order_count) VALUES (@fullName, @phone, 0)";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@fullName", fullName);
                        cmd.Parameters.AddWithValue("@phone", phone);

                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            MessageBox.Show("Клієнта успішно додано.");
                            DialogResult = true;
                            Close();
                        }
                        else
                        {
                            MessageBox.Show("Помилка додавання клієнта.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка: " + ex.Message);
            }
        }
    }
}
