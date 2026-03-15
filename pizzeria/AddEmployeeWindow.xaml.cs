using System.Windows;
using System.Data;
using MySql.Data.MySqlClient;

namespace pizzeria
{
    public partial class AddEmployeeWindow : Window
    {
        public AddEmployeeWindow()
        {
            InitializeComponent();
            LoadPositions();
        }

        private void LoadPositions()
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT position_id, position_name FROM position_", conn);
                var adapter = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                PositionComboBox.ItemsSource = dt.DefaultView;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = FullNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(fullName))
            {
                MessageBox.Show("Введіть прізвище та ім'я співробітника!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (PositionComboBox.SelectedValue == null)
            {
                MessageBox.Show("Оберіть посаду!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int positionId = (int)PositionComboBox.SelectedValue;

            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand(
                    "INSERT INTO employee (employee_full_name, position_id) VALUES (@fullName, @positionId)", conn);
                cmd.Parameters.AddWithValue("@fullName", fullName);
                cmd.Parameters.AddWithValue("@positionId", positionId);

                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Співробітника додано успішно!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
