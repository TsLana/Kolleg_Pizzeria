using MySql.Data.MySqlClient;
using System;
using System.Windows;
using System.Windows.Controls;

namespace pizzeria
{
    public partial class EditDishWindow : Window
    {
        private int DishId;

        public EditDishWindow(int dishId)
        {
            InitializeComponent();
            DishId = dishId;
            LoadCategories();
            LoadDishData();
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
        }

        private void LoadDishData()
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string query = "SELECT dish_name, dish_ingredients, dish_price, servings_per_day, dish_weight, category_id FROM menu WHERE dish_id = @id";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", DishId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            DishNameTextBox.Text = reader.GetString("dish_name");
                            IngredientsTextBox.Text = reader.GetString("dish_ingredients");
                            PriceTextBox.Text = reader.GetDecimal("dish_price").ToString();
                            ServingsTextBox.Text = reader.GetInt32("servings_per_day").ToString();
                            WeightTextBox.Text = reader.GetInt32("dish_weight").ToString();

                            int catId = reader.GetInt32("category_id");
                            foreach (ComboBoxItem item in CategoryComboBox.Items)
                            {
                                if ((int)item.Tag == catId)
                                {
                                    CategoryComboBox.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
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
                    string query = @"UPDATE menu SET 
                                    dish_name = @name,
                                    dish_ingredients = @ingredients,
                                    dish_price = @price,
                                    servings_per_day = @servings,
                                    dish_weight = @weight,
                                    category_id = @categoryId
                                    WHERE dish_id = @id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@ingredients", ingredients);
                        cmd.Parameters.AddWithValue("@price", price);
                        cmd.Parameters.AddWithValue("@servings", servings);
                        cmd.Parameters.AddWithValue("@weight", weight);
                        cmd.Parameters.AddWithValue("@categoryId", categoryId);
                        cmd.Parameters.AddWithValue("@id", DishId);

                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Страва успішно оновлена.");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при оновленні страви: " + ex.Message);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
