using System;
using System.Data;
using System.Windows;
using MySql.Data.MySqlClient;

namespace pizzeria
{
    public partial class ClientsWindow : Window
    {
        public ClientsWindow()
        {
            InitializeComponent();
            LoadClients();
        }

        private void LoadClients()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();  

                    string query = "SELECT client_id, client_full_name, client_phone_number, order_count FROM client_";
                    using (var adapter = new MySqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        ClientsDataGrid.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження клієнтів: " + ex.Message);
            }
        }

        private void AddClient_Click(object sender, RoutedEventArgs e)
        {
            AddClientWindow addWindow = new AddClientWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadClients();
            }
        }

        private void EditClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ClientsDataGrid.SelectedItem == null)
                {
                    MessageBox.Show("Будь ласка, виберіть клієнта для редагування.");
                    return;
                }

                DataRowView rowView = ClientsDataGrid.SelectedItem as DataRowView;
                if (rowView == null)
                {
                    MessageBox.Show("Неможливо отримати дані вибраного клієнта.");
                    return;
                }

                int clientId = Convert.ToInt32(rowView["client_id"]);

                EditClientWindow editWindow = new EditClientWindow(clientId);
                if (editWindow.ShowDialog() == true)
                {
                    LoadClients();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка: " + ex.Message);
            }
        }

        private void DeleteClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ClientsDataGrid.SelectedItem == null)
                {
                    MessageBox.Show("Будь ласка, виберіть клієнта для видалення.");
                    return;
                }

                DataRowView rowView = ClientsDataGrid.SelectedItem as DataRowView;
                if (rowView == null)
                {
                    MessageBox.Show("Неможливо отримати дані вибраного клієнта.");
                    return;
                }

                int clientId = Convert.ToInt32(rowView["client_id"]);

                var result = MessageBox.Show("Ви впевнені, що хочете видалити цього клієнта?", "Підтвердження видалення", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using (var conn = Database.GetConnection())
                    {
                        conn.Open();  

                        string query = "DELETE FROM client_ WHERE client_id = @clientId";
                        using (var cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@clientId", clientId);

                            int rows = cmd.ExecuteNonQuery();
                            if (rows > 0)
                            {
                                MessageBox.Show("Клієнта успішно видалено.");
                                LoadClients();
                            }
                            else
                            {
                                MessageBox.Show("Помилка при видаленні клієнта.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка: " + ex.Message);
            }
        }

        private void BackToMain_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
