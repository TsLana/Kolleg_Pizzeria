using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Windows;

namespace pizzeria
{
    public partial class MainWindow : Window
    {
        private string _employeeName;
        private string _position;

        public MainWindow(string employeeName, string position)
        {
            InitializeComponent();
            _employeeName = employeeName;
            _position = position;
            WelcomeTextBlock.Text = $"Ласкаво просимо, {_employeeName} ({_position})";
        }

        private void OrdersButton_Click(object sender, RoutedEventArgs e)
        {
            OrdersWindow ordersWindow = new OrdersWindow();
            ordersWindow.ShowDialog();
        }


        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            MenuWindow menuWindow = new MenuWindow();
            menuWindow.ShowDialog();
        }

        private void ClientsButton_Click(object sender, RoutedEventArgs e)
        {
            ClientsWindow clientsWindow = new ClientsWindow();
            clientsWindow.ShowDialog();
        }

        private void EmployeesButton_Click(object sender, RoutedEventArgs e)
        {
            EmployeesWindow employeesWindow = new EmployeesWindow();
            employeesWindow.ShowDialog();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}
