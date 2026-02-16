using System.Windows;
using System.Windows.Input;

namespace WeatherApplication
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Allow Enter key to trigger search
            CityTextBox.KeyDown += CityTextBox_KeyDown;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SearchCity();
        }

        private void CityTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchCity();
            }
        }

        private void SearchCity()
        {
            string city = CityTextBox.Text.Trim();

            if (string.IsNullOrEmpty(city))
            {
                MessageBox.Show("Please enter a city name.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Open the WeatherPage and pass the city
            WeatherPage weatherPage = new WeatherPage(city);
            weatherPage.Show();

            CityTextBox.Clear();
        }
    }
}