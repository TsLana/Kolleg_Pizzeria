using System;
using System.Collections.Generic;
using System.Windows;
using MySql.Data.MySqlClient;

namespace pizzeria
{
    public partial class EmployeesWindow : Window
    {
        public EmployeesWindow()
        {
            InitializeComponent();
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT employee.employee_id, employee.employee_full_name, position_.position_name, position_.position_id
                        FROM employee
                        INNER JOIN position_ ON employee.position_id = position_.position_id
                        ORDER BY employee.employee_full_name";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var employees = new List<EmployeeViewModel>();

                        while (reader.Read())
                        {
                            employees.Add(new EmployeeViewModel
                            {
                                employee_id = reader.GetInt32("employee_id"),
                                employee_full_name = reader.GetString("employee_full_name"),
                                position_name = reader.GetString("position_name"),
                                position_id = reader.GetInt32("position_id")
                            });
                        }

                        EmployeesDataGrid.ItemsSource = employees;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження співробітників:\n" + ex.Message);
            }
        }

        private void BackToMain_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddEmployeeWindow();
            bool? result = addWindow.ShowDialog();
            if (result == true)
            {
                LoadEmployees();
            }
        }

        private void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeesDataGrid.SelectedItem is EmployeeViewModel selectedEmployee)
            {
                var editWindow = new EditEmployeeWindow(
                    selectedEmployee.employee_id,
                    selectedEmployee.employee_full_name,
                    selectedEmployee.position_id);

                bool? result = editWindow.ShowDialog();
                if (result == true)
                {
                    LoadEmployees();
                }
            }
            else
            {
                MessageBox.Show("Оберіть співробітника для редагування.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeesDataGrid.SelectedItem is EmployeeViewModel selectedEmployee)
            {
                var res = MessageBox.Show($"Ви дійсно хочете видалити співробітника '{selectedEmployee.employee_full_name}'?",
                    "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var conn = Database.GetConnection())
                        {
                            conn.Open();

                            string query = "DELETE FROM employee WHERE employee_id = @employee_id";
                            using (var cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@employee_id", selectedEmployee.employee_id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        LoadEmployees();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Помилка видалення співробітника:\n" + ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("Оберіть співробітника для видалення.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    public class EmployeeViewModel
    {
        public int employee_id { get; set; }
        public string employee_full_name { get; set; }
        public string position_name { get; set; }
        public int position_id { get; set; }
    }
}
