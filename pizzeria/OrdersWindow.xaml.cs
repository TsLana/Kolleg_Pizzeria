using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace pizzeria
{
    public partial class OrdersWindow : Window
    {
        private List<Order> allOrders = new List<Order>();
        private List<Order> SortOrders(List<Order> orders)
        {
            return orders
                .OrderBy(o => o.OrderStatus == "Доставлено" || o.OrderStatus == "Видано" ? 1 : 0)
                .ThenByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderTime)
                .ToList();
        }

        public OrdersWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при завантаженні замовлень:\n" + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void LoadOrders()
        {
            allOrders.Clear();  

            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var query = @"
                    SELECT o.order_id, o.order_date, o.order_time, o.client_id, c.client_full_name, 
                    o.order_type, o.order_status, o.order_address, c.client_phone_number,
                    SUM(m.dish_price * om.order_menu_count) AS total_amount
                    FROM orders o
                    JOIN client_ c ON o.client_id = c.client_id
                    JOIN order_menu om ON o.order_id = om.order_id
                    JOIN menu m ON om.dish_id = m.dish_id
                    GROUP BY o.order_id
                    ORDER BY 
                        CASE 
                            WHEN o.order_status IN ('Доставлено', 'Видано') THEN 1
                            ELSE 0
                            END ASC,
                            o.order_date DESC,
                            o.order_time DESC";
;

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        allOrders.Add(new Order
                        {
                            OrderId = reader.GetInt32("order_id"),
                            OrderDate = reader.GetDateTime("order_date"),
                            OrderTime = reader.GetTimeSpan("order_time"),
                            ClientId = reader.GetInt32("client_id"),
                            ClientName = reader.GetString("client_full_name"),
                            ClientPhone = reader.IsDBNull(reader.GetOrdinal("client_phone_number")) ? "" : reader.GetString("client_phone_number"),
                            OrderType = reader.GetString("order_type"),
                            OrderStatus = reader.GetString("order_status"),
                            ClientAddress = reader.IsDBNull(reader.GetOrdinal("order_address")) ? "—" : reader.GetString("order_address"),
                            TotalAmount = reader.GetDecimal("total_amount")
                        });
                    }
                }

                foreach (var order in allOrders)
                {
                    using (var cmdDetails = new MySqlCommand(@"
                        SELECT m.dish_name, om.order_menu_count, m.dish_price
                        FROM order_menu om
                        JOIN menu m ON om.dish_id = m.dish_id
                        WHERE om.order_id = @orderId", conn))
                    {
                        cmdDetails.Parameters.AddWithValue("@orderId", order.OrderId);
                        using (var readerDetails = cmdDetails.ExecuteReader())
                        {
                            List<string> detailsList = new List<string>();
                            while (readerDetails.Read())
                            {
                                string dishName = readerDetails.GetString("dish_name");
                                int count = readerDetails.GetInt32("order_menu_count");
                                decimal price = readerDetails.GetDecimal("dish_price");
                                detailsList.Add($"{dishName} ({count}×{price:0.00}₴)");
                            }
                            order.OrderDetails = string.Join(", ", detailsList);
                        }
                    }
                }
            }

            OrdersDataGrid.ItemsSource = null;
            OrdersDataGrid.ItemsSource = SortOrders(allOrders);
            TotalSumTextBlock.Text = $"Сума всіх замовлень: {allOrders.Sum(o => o.TotalAmount):0.00} грн";
        }


        private void BackToMain_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            AddOrderWindow addOrderWindow = new AddOrderWindow();
            if (addOrderWindow.ShowDialog() == true)
            {
                LoadOrders();
            }
        }


        private void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem is Order selectedOrder)
            {
                if (selectedOrder.OrderStatus != "Замовлення прийнято")
                {
                    MessageBox.Show("Замовлення можна видалити лише на етапі 'Замовлення прийнято'.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show("Ви впевнені, що хочете видалити це замовлення?", "Підтвердження", MessageBoxButton.YesNo);
                if (confirm == MessageBoxResult.Yes)
                {
                    using (var conn = Database.GetConnection())
                    {
                        conn.Open();

                        
                        var cmd1 = new MySqlCommand("DELETE FROM order_menu WHERE order_id = @id", conn);
                        cmd1.Parameters.AddWithValue("@id", selectedOrder.OrderId);
                        cmd1.ExecuteNonQuery();

                        
                        var cmd2 = new MySqlCommand("DELETE FROM orders WHERE order_id = @id", conn);
                        cmd2.Parameters.AddWithValue("@id", selectedOrder.OrderId);
                        cmd2.ExecuteNonQuery();

                        
                        var cmd3 = new MySqlCommand("UPDATE client_ SET order_count = GREATEST(order_count - 1, 0) WHERE client_id = @clientId", conn);
                        cmd3.Parameters.AddWithValue("@clientId", selectedOrder.ClientId);
                        cmd3.ExecuteNonQuery();
                    }

                    LoadOrders();
                }
            }
        }


        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            string selectedFilter = (FilterComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            List<Order> filtered = new List<Order>(allOrders);

            switch (selectedFilter)
            {
                case "Дата ↑": 
                    filtered = filtered.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderTime).ToList();
                    break;
                case "Дата ↓": 
                    filtered = filtered.OrderBy(o => o.OrderDate).ThenBy(o => o.OrderTime).ToList();
                    break;
                case "Сума ↑": 
                    filtered = filtered.OrderByDescending(o => o.TotalAmount).ToList();
                    break;
                case "Сума ↓":
                    filtered = filtered.OrderBy(o => o.TotalAmount).ToList();
                    break;
            }

            OrdersDataGrid.ItemsSource = filtered;
            TotalSumTextBlock.Text = $"Сума всіх замовлень: {filtered.Sum(o => o.TotalAmount):0.00} грн";

        }


        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim().ToLower();
            var filtered = allOrders.Where(o => o.ClientName.ToLower().Contains(searchText)).ToList();

            string selectedFilter = (FilterComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            switch (selectedFilter)
            {
                case "Дата ↑": 
                    filtered = filtered.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderTime).ToList();
                    break;
                case "Дата ↓": 
                    filtered = filtered.OrderBy(o => o.OrderDate).ThenBy(o => o.OrderTime).ToList();
                    break;
                case "Сума ↑": 
                    filtered = filtered.OrderByDescending(o => o.TotalAmount).ToList();
                    break;
                case "Сума ↓": 
                    filtered = filtered.OrderBy(o => o.TotalAmount).ToList();
                    break;
            }

            OrdersDataGrid.ItemsSource = filtered;
            TotalSumTextBlock.Text = $"Сума всіх замовлень: {filtered.Sum(o => o.TotalAmount):0.00} грн";

        }

        private void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Оберіть замовлення для редагування.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedOrder = OrdersDataGrid.SelectedItem as Order;
            if (selectedOrder == null)
                return;

            var editWindow = new EditOrderWindow(selectedOrder.OrderId, selectedOrder.OrderStatus);
            editWindow.Owner = this;
            bool? result = editWindow.ShowDialog();

            if (result == true)
            {
                LoadOrders();
            }
        }

    }

    public class Order
    {
        public int OrderId { get; set; }
        public int ClientId { get; set; }
        public DateTime OrderDate { get; set; }
        public TimeSpan OrderTime { get; set; }
        public string DateTimeFormatted => $"{OrderDate:dd.MM.yyyy} {OrderTime:hh\\:mm}";
        public string ClientName { get; set; }
        private string _clientPhone;
        public string ClientPhone
        {
            get => _clientPhone;
            set => _clientPhone = value;
        }
        public string PhoneForDisplay => OrderType == "Доставка" && !string.IsNullOrWhiteSpace(ClientPhone) ? ClientPhone: "—";
        public string OrderType { get; set; }
        public string OrderStatus { get; set; }
        public string ClientAddress { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderDetails { get; set; }  
    }
}
