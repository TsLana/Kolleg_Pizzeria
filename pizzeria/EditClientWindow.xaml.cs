using System;
using System.Windows;
using MySql.Data.MySqlClient;

namespace pizzeria
{
    public partial class EditClientWindow : Window
    {
        private int _clientId;

        public EditClientWindow(int clientId)
        {
            InitializeComponent();
            _clientId = clientId;
            LoadClientData();
        }

        private void LoadClientData()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();  

                    string query = "SELECT client_full_name, client_phone_number FROM client_ WHERE client_id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _clientId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                FullNameTextBox.Text = reader.GetString("client_full_name");
                                PhoneTextBox.Text = reader.GetString("client_phone_number");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження даних клієнта: " + ex.Message);
            }
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

                    string query = "UPDATE client_ SET client_full_name = @fullName, client_phone_number = @phone WHERE client_id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@fullName", fullName);
                        cmd.Parameters.AddWithValue("@phone", phone);
                        cmd.Parameters.AddWithValue("@id", _clientId);

                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            MessageBox.Show("Дані клієнта оновлено.");
                            DialogResult = true;
                            Close();
                        }
                        else
                        {
                            MessageBox.Show("Помилка оновлення клієнта.");
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
