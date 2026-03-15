using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MySql.Data.MySqlClient;

namespace pizzeria
{
    public partial class AddOrderWindow : Window
    {
        public ObservableCollection<DishItem> SelectedDishes { get; set; } = new ObservableCollection<DishItem>();
        public ObservableCollection<MenuItem> MenuItems { get; set; } = new ObservableCollection<MenuItem>();
        public ObservableCollection<ClientItem> Clients { get; set; } = new ObservableCollection<ClientItem>();

        public AddOrderWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadMenuItems();
            InitializeTimeComboBox();
            SelectedDishes.CollectionChanged += (s, e) => UpdateTotalPrice();
        }

        private int? foundClientId = null;
        private int clientOrderCount = 0;

        private void InitializeTimeComboBox()
        {
            HourComboBox.Items.Clear();
            MinuteComboBox.Items.Clear();

            for (int hour = 8; hour <= 22; hour++)
            {
                HourComboBox.Items.Add(hour.ToString("D2"));
            }

            for (int minute = 0; minute < 60; minute++)
            {
                MinuteComboBox.Items.Add(minute.ToString("D2"));
            }
        }


        private void ClientNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string fullName = ClientNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(fullName))
            {
                AddClientButton.Visibility = Visibility.Collapsed;
                foundClientId = null;
                clientOrderCount = 0;
                PhoneTextBox.Text = string.Empty;
                UpdateDiscountDisplay(0, 0);
                return;
            }

            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT client_id, order_count, client_phone_number FROM client_ WHERE client_full_name = @fullName";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@fullName", fullName);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                foundClientId = reader.GetInt32("client_id");
                                clientOrderCount = reader.GetInt32("order_count");
                                string phoneNumber = reader.IsDBNull(reader.GetOrdinal("client_phone_number")) ? "" : reader.GetString("client_phone_number");
                                PhoneTextBox.Text = phoneNumber;
                                AddClientButton.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                foundClientId = null;
                                clientOrderCount = 0;
                                PhoneTextBox.Text = string.Empty;
                                AddClientButton.Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка перевірки клієнта: " + ex.Message);
                foundClientId = null;
                clientOrderCount = 0;
                PhoneTextBox.Text = string.Empty;
                AddClientButton.Visibility = Visibility.Collapsed;
            }

            decimal currentTotal = ParsePriceFromTextBlock(TotalPriceTextBlock.Text);
            UpdateDiscountDisplay(clientOrderCount, currentTotal);
        }



        private void LoadMenuItems()
        {
            try
            {
                var conn = Database.GetConnection();
                conn.Open();
                var query = "SELECT dish_id, dish_name, dish_price, servings_per_day FROM menu";
                var cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    MenuItems.Add(new MenuItem
                    {
                        DishId = reader.GetInt32("dish_id"),
                        DishName = reader.GetString("dish_name"),
                        DishPrice = reader.GetDecimal("dish_price"),
                        ServingsPerDay = reader.GetInt32("servings_per_day")
                    });
                }
                reader.Close();
                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при завантаженні меню: " + ex.Message);
            }
        }

        private void AddDishButton_Click(object sender, RoutedEventArgs e)
        {
            var dish = new DishItem
            {
                UpdateTotalPriceCallback = UpdateTotalPrice
            };
            DishItem.MenuItems = MenuItems;
            SelectedDishes.Add(dish);
        }

        private void AddClientButton_Click(object sender, RoutedEventArgs e)
        {
            string initialName = ClientNameTextBox.Text.Trim();

            var addClientWindow = new AddClientWindow(initialName);
            if (addClientWindow.ShowDialog() == true)
            {
                ClientNameTextBox_LostFocus(null, null);
            }
        }


        private void RemoveDishButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DishItem dish)
            {
                SelectedDishes.Remove(dish);
            }
        }

        private void OrderTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrderTypeComboBox.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Content.ToString() == "Доставка")
            {
                AddressPanel.Visibility = Visibility.Visible;
                PhonePanel.Visibility = Visibility.Visible;
            }
            else
            {
                AddressPanel.Visibility = Visibility.Collapsed;
                AddressTextBox.Text = string.Empty;

                PhonePanel.Visibility = Visibility.Collapsed;
                PhoneTextBox.Text = string.Empty;
            }
        }



        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is DishItem dish)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                    return;

                if (!int.TryParse(textBox.Text, out int enteredQuantity))
                {
                    textBox.Background = Brushes.MistyRose;
                    ToolTipService.SetToolTip(textBox, "Введіть ціле число більше за 0.");
                    return;
                }

                if (enteredQuantity <= 0)
                {
                    MessageBox.Show("Кількість не може бути 0 або від’ємною.",
                                    "Некоректне значення",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);

                    dish.Quantity = 1;
                    textBox.Text = "1";

                    textBox.Background = Brushes.White;
                    ToolTipService.SetToolTip(textBox, null);

                    UpdateTotalPrice();
                    return;
                }

                textBox.Background = Brushes.White;
                ToolTipService.SetToolTip(textBox, null);

                var menuItem = MenuItems.FirstOrDefault(m => m.DishId == dish.DishId);
                if (menuItem == null) return;

                if (OrderDatePicker.SelectedDate == null) return;

                int alreadyOrdered = GetOrderedQuantityForDate(menuItem.DishId, OrderDatePicker.SelectedDate.Value);
                int available = menuItem.ServingsPerDay - alreadyOrdered;

                if (enteredQuantity > available)
                {
                    MessageBox.Show($"Максимальна кількість для страви \"{menuItem.DishName}\" на обрану дату: {available}.",
                                    "Обмеження порцій",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);

                    dish.Quantity = available > 0 ? available : 1;
                    textBox.Text = dish.Quantity.ToString();
                }
                else
                {
                    dish.Quantity = enteredQuantity;
                }

                UpdateTotalPrice();
            }
        }




        private int GetOrderedQuantityForDate(int dishId, DateTime date)
        {
            int total = 0;

            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();

                    string query = @"
                SELECT SUM(om.order_menu_count) AS total 
                FROM orders o
                JOIN order_menu om ON o.order_id = om.order_id
                WHERE om.dish_id = @dishId AND DATE(o.order_date) = @date";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@dishId", dishId);
                        cmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));

                        var result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            total = Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка підрахунку замовлених порцій: " + ex.Message);
            }

            return total;
        }

        private void UpdateDiscountDisplay(int orderCount, decimal total)
        {
            bool isRegularClient = orderCount >= 5; 

            if (isRegularClient)
            {
                DiscountedPanel.Visibility = Visibility.Visible;
                DefaultPanel.Visibility = Visibility.Collapsed;

                decimal discountAmount = Math.Round(total * 0.05m, 2); 
                decimal discountedTotal = total - discountAmount;

                DiscountTextBlock.Text = $"Ваша знижка: -₴{discountAmount:F2}";
                DiscountedTotalTextBlock.Text = $"До оплати: ₴{discountedTotal:F2}";
                TotalPriceTextBlock.Text = $"₴{total:F2}"; 
            }
            else
            {
                DiscountedPanel.Visibility = Visibility.Collapsed;
                DefaultPanel.Visibility = Visibility.Visible;

                TotalPriceTextBlock_Default.Text = $"₴{total:F2}";
            }
        }


        private decimal ParsePriceFromTextBlock(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            string cleaned = text.Replace("₴", "").Trim();
            if (decimal.TryParse(cleaned, out decimal result))
                return result;
            return 0;
        }


        private void UpdateTotalPrice()
        {
            decimal total = 0;
            foreach (var dish in SelectedDishes)
            {
                var menuItem = MenuItems.FirstOrDefault(m => m.DishId == dish.DishId);
                if (menuItem != null && dish.Quantity > 0)
                {
                    dish.DishPrice = menuItem.DishPrice;
                    total += menuItem.DishPrice * dish.Quantity;
                }
            }
            TotalPriceTextBlock.Text = $"₴{total:F2}";

            UpdateDiscountDisplay(clientOrderCount, total);
        }


        private void SaveOrderButton_Click(object sender, RoutedEventArgs e)
        {
            string phone = PhoneTextBox.Text;

            string clientName = ClientNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(clientName))
            {
                MessageBox.Show("Будь ласка, введіть Прізвище та ім я клієнта.");
                ClientNameTextBox.Focus();
                return;
            }
            if (foundClientId == null)
            {
                var result = MessageBox.Show($"Клієнта \"{clientName}\" не знайдено в базі.\n" +
                    "Бажаєте додати нового клієнта?", "Клієнт не знайдений", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    AddClientButton_Click(null, null);
                    return; 
                }
                else
                {
                    ClientNameTextBox.Focus();
                    return;
                }
            }

            if (OrderDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Будь ласка, виберіть дату замовлення.");
                OrderDatePicker.Focus();
                return;
            }

            if (HourComboBox.SelectedItem == null || MinuteComboBox.SelectedItem == null)
            {
                MessageBox.Show("Будь ласка, виберіть час замовлення.");
                if (HourComboBox.SelectedItem == null) HourComboBox.Focus();
                else MinuteComboBox.Focus();
                return;
            }

            if (OrderTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Будь ласка, виберіть тип замовлення.");
                OrderTypeComboBox.Focus();
                return;
            }

            if (!Regex.IsMatch(phone, @"^\d{1,10}$"))
            {
                MessageBox.Show("Номер телефону повинен містити тільки цифри і максимум 10 символів.");
                return;
            }


            if ((OrderTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Доставка")
            {
                if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
                {
                    MessageBox.Show("Будь ласка, введіть адресу доставки.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
                {
                    MessageBox.Show("Будь ласка, введіть номер телефону клієнта.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (SelectedDishes.Count == 0)
            {
                MessageBox.Show("Будь ласка, додайте хоча б одну страву до замовлення.");
                return;
            }
            if (SelectedDishes.Any(d => d.DishId == 0))
            {
                MessageBox.Show("Будь ласка, оберіть страву зі списку.");
                return;
            }
            if (SelectedDishes.Any(d => string.IsNullOrWhiteSpace(d.Quantity.ToString())))
            {
                MessageBox.Show("Будь ласка, вкажіть кількість для кожної страви.");
                return;
            }
            if (SelectedDishes.Any(d => d.Quantity <= 0))
            {
                MessageBox.Show("Кількість кожної страви має бути більше 0.");
                return;
            }

            DateTime orderDate = OrderDatePicker.SelectedDate.Value;
            string orderTime = $"{HourComboBox.SelectedItem}:{MinuteComboBox.SelectedItem}:00";
            string orderType = ((ComboBoxItem)OrderTypeComboBox.SelectedItem).Content.ToString();
            string address = orderType == "Доставка" ? AddressTextBox.Text.Trim() : null;
            string status = "Замовлення прийнято"; 

            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();

                    var insertOrderCmd = new MySqlCommand(@"
                        INSERT INTO orders (client_id, order_date, order_time, order_type, order_status, order_address)
                        VALUES (@clientId, @date, @time, @type, @status, @address)", conn);
                    insertOrderCmd.Parameters.AddWithValue("@clientId", foundClientId);
                    insertOrderCmd.Parameters.AddWithValue("@date", orderDate);
                    insertOrderCmd.Parameters.AddWithValue("@time", orderTime);
                    insertOrderCmd.Parameters.AddWithValue("@type", orderType);
                    insertOrderCmd.Parameters.AddWithValue("@status", status);
                    insertOrderCmd.Parameters.AddWithValue("@address", (object)address ?? DBNull.Value);
                    insertOrderCmd.ExecuteNonQuery();

                    long orderId = insertOrderCmd.LastInsertedId;

                    foreach (var dish in SelectedDishes)
                    {
                        var insertItemCmd = new MySqlCommand(@"
                            INSERT INTO order_menu (order_id, dish_id, order_menu_count)
                            VALUES (@orderId, @dishId, @count)", conn);
                        insertItemCmd.Parameters.AddWithValue("@orderId", orderId);
                        insertItemCmd.Parameters.AddWithValue("@dishId", dish.DishId);
                        insertItemCmd.Parameters.AddWithValue("@count", dish.Quantity);
                        insertItemCmd.ExecuteNonQuery();
                    }

                    var updateClientCmd = new MySqlCommand(@"
                        UPDATE client_ SET order_count = order_count + 1
                        WHERE client_id = @clientId", conn);
                    updateClientCmd.Parameters.AddWithValue("@clientId", foundClientId);
                    updateClientCmd.ExecuteNonQuery();

                    MessageBox.Show("Замовлення успішно додано!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка збереження замовлення: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class MenuItem
    {
        public int DishId { get; set; }
        public string DishName { get; set; }
        public decimal DishPrice { get; set; }
        public int ServingsPerDay { get; set; }
    }

    public class DishItem : INotifyPropertyChanged
    {
        public int DishId
        {
            get => dishId;
            set
            {
                dishId = value;
                UpdatePrice();
                OnPropertyChanged(nameof(DishId));
            }
        }
        private int dishId;

        private int quantity;
        public int Quantity
        {
            get => quantity;
            set
            {
                quantity = value;
                UpdateTotalPriceCallback?.Invoke(); 
                OnPropertyChanged(nameof(Quantity));
            }
        }

        private decimal dishPrice;
        public decimal DishPrice
        {
            get => dishPrice;
            set
            {
                dishPrice = value;
                OnPropertyChanged(nameof(DishPrice));
            }
        }

        public static ObservableCollection<MenuItem> MenuItems { get; set; }

        public Action UpdateTotalPriceCallback { get; set; }

        private void UpdatePrice()
        {
            var menuItem = MenuItems?.FirstOrDefault(m => m.DishId == DishId);
            if (menuItem != null)
            {
                DishPrice = menuItem.DishPrice;
                UpdateTotalPriceCallback?.Invoke(); 
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


    public class ClientItem
    {
        public int ClientId { get; set; }
        public string FullName { get; set; }
    }
}