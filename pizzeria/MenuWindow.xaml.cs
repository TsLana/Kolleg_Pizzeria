using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace pizzeria
{
    public partial class MenuWindow : Window
    {
        private DataTable menuTable = new DataTable();
        private List<string> categories = new List<string>();

        public MenuWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCategories();
            LoadMenu();
        }

        private void LoadCategories()
        {
            categories.Clear();
            CategoryFilterComboBox.Items.Clear();
            CategoryFilterComboBox.Items.Add("Усі");

            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string query = "SELECT category_name FROM dish_category ORDER BY category_name";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string category = reader.GetString("category_name");
                        categories.Add(category);
                        CategoryFilterComboBox.Items.Add(category);
                    }
                }
            }

            CategoryFilterComboBox.SelectedIndex = 0;
        }

        private void LoadMenu(string filterCategory = null)
        {
            menuTable.Clear();

            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT m.dish_id, m.dish_name, m.dish_ingredients, m.dish_price, 
                           m.servings_per_day, m.dish_weight, dc.category_name
                    FROM menu m
                    JOIN dish_category dc ON m.category_id = dc.category_id";

                if (!string.IsNullOrEmpty(filterCategory) && filterCategory != "Усі")
                {
                    query += " WHERE dc.category_name = @category";
                }

                using (var cmd = new MySqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(filterCategory) && filterCategory != "Усі")
                        cmd.Parameters.AddWithValue("@category", filterCategory);

                    using (var adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(menuTable);
                        MenuDataGrid.ItemsSource = menuTable.DefaultView;
                    }
                }
            }
        }

        private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedCategory = CategoryFilterComboBox.SelectedItem as string;
            LoadMenu(selectedCategory);
        }

        private void BackToMain_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void AddDish_Click(object sender, RoutedEventArgs e)
        {
            AddDishWindow addDishWindow = new AddDishWindow();
            if (addDishWindow.ShowDialog() == true)
            {
                LoadMenu(CategoryFilterComboBox.SelectedItem.ToString());
            }
        }

        private void EditDish_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MenuDataGrid.SelectedItem == null)
                {
                    MessageBox.Show("Будь ласка, виберіть страву для редагування.");
                    return;
                }

                DataRowView rowView = MenuDataGrid.SelectedItem as DataRowView;
                if (rowView == null)
                {
                    MessageBox.Show("Неможливо отримати дані вибраної страви.");
                    return;
                }

                int dishId = Convert.ToInt32(rowView["dish_id"]);

                EditDishWindow editWindow = new EditDishWindow(dishId);
                if (editWindow.ShowDialog() == true)
                {
                    LoadMenu(CategoryFilterComboBox.SelectedItem?.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка: " + ex.Message);
            }
        }



        private void DeleteDish_Click(object sender, RoutedEventArgs e)
        {
            if (MenuDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Оберіть страву для видалення.");
                return;
            }

            DataRowView row = MenuDataGrid.SelectedItem as DataRowView;
            int dishId = Convert.ToInt32(row["dish_id"]);

            var result = MessageBox.Show("Ви впевнені, що хочете видалити цю страву?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string query = "DELETE FROM menu WHERE dish_id = @id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", dishId);
                        cmd.ExecuteNonQuery();
                    }
                }

                LoadMenu(CategoryFilterComboBox.SelectedItem.ToString());
            }
        }
    }
}
