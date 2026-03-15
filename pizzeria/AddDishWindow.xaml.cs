using MySql.Data.MySqlClient;
using System;
using System.Windows;
using System.Windows.Controls;

namespace pizzeria
{
    public partial class AddDishWindow : Window
    {
        public AddDishWindow()
        {
            InitializeComponent();
            LoadCategories();
        }

        private void LoadCategories()
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string query = "SELECT category_id, category_name FROM dish_category ORDER BY category_name";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ComboBoxItem item = new ComboBoxItem
                        {
                            Content = reader.GetString("category_name"),
                            Tag = reader.GetInt32("category_id")
                        };
                        CategoryComboBox.Items.Add(item);
                    }
                }
            }
            if (CategoryComboBox.Items.Count > 0)
                CategoryComboBox.SelectedIndex = 0;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name = DishNameTextBox.Text.Trim();
            string ingredients = IngredientsTextBox.Text.Trim();
            string priceText = PriceTextBox.Text.Trim();
            string servingsText = ServingsTextBox.Text.Trim();
            string weightText = WeightTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(priceText) || string.IsNullOrEmpty(servingsText) || CategoryComboBox.SelectedItem == null)
            {
                MessageBox.Show("Будь ласка, заповніть обов'язкові поля: Назва, Ціна, Кількість порцій, Категорія.");
                return;
            }

            if (!decimal.TryParse(priceText, out decimal price) || price < 0)
            {
                MessageBox.Show("Введіть коректну позитивну ціну.");
                return;
            }

            if (!int.TryParse(servingsText, out int servings) || servings < 0)
            {
                MessageBox.Show("Введіть коректну кількість порцій.");
                return;
            }

            int weight = 0;
            if (!string.IsNullOrEmpty(weightText) && (!int.TryParse(weightText, out weight) || weight < 0))
            {
                MessageBox.Show("Введіть коректну вагу (або залиште пустою).");
                return;
            }

            int categoryId = (int)((ComboBoxItem)CategoryComboBox.SelectedItem).Tag;

            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string query = @"INSERT INTO menu 
                                    (dish_name, dish_ingredients, dish_price, servings_per_day, dish_weight, category_id) 
                                     VALUES (@name, @ingredients, @price, @servings, @weight, @categoryId)";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@ingredients", ingredients);
                        cmd.Parameters.AddWithValue("@price", price);
                        cmd.Parameters.AddWithValue("@servings", servings);
                        cmd.Parameters.AddWithValue("@weight", weight);
                        cmd.Parameters.AddWithValue("@categoryId", categoryId);

                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Страва додана успішно.");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при додаванні страви: " + ex.Message);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
