using System;
using System.Data;
using System.Windows;
using MySql.Data.MySqlClient;

namespace pizzeria
{
    public partial class EditOrderWindow : Window
    {
        private int orderId;
        private string currentStatus;
        private string orderType;

        private readonly string[] statuses = new string[]
        {
            "Замовлення прийнято",
            "Готується",
            "Готове",
            "Видано",
            "В доставці",
            "Доставлено"
        };


        public EditOrderWindow(int orderId, string currentStatus)
        {
            InitializeComponent();
            this.orderId = orderId;
            this.currentStatus = currentStatus;

            LoadOrderTypeAndSetStatuses(); 
        }

        private void LoadOrderTypeAndSetStatuses()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT order_type FROM orders WHERE order_id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", orderId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                orderType = reader.GetString("order_type");

                                if (orderType == "Доставка")
                                {
                                    StatusComboBox.ItemsSource = new string[]
                                    {
                                "Замовлення прийнято",
                                "Готується",
                                "Готове",
                                "В доставці",
                                "Доставлено"
                                    };
                                }
                                else if (orderType == "Самовивіз")
                                {
                                    StatusComboBox.ItemsSource = new string[]
                                    {
                                "Замовлення прийнято",
                                "Готується",
                                "Готове",
                                "Видано"
                                    };
                                }

                                StatusComboBox.SelectedItem = currentStatus;
                            }
                            else
                            {
                                MessageBox.Show("Не вдалося знайти тип замовлення.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                                this.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при завантаженні типу замовлення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string newStatus = StatusComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(newStatus))
            {
                MessageBox.Show("Оберіть статус.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newStatus == currentStatus)
            {
                this.DialogResult = false;
                return;
            }

            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string query = "UPDATE orders SET order_status = @status WHERE order_id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", newStatus);
                        cmd.Parameters.AddWithValue("@id", orderId);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Статус успішно оновлено.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при оновленні статусу: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
