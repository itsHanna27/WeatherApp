using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace WeatherApplication
{
    /// <summary>
    /// Interaction logic for WeatherPage.xaml
    /// </summary>
    public partial class WeatherPage : Window
    {
        private readonly string _city;
        private static readonly HttpClient client = new HttpClient();

        public WeatherPage(string city)
        {
            InitializeComponent();
            _city = city;

            // Set User-Agent for Nominatim API (required)
            if (!client.DefaultRequestHeaders.Contains("User-Agent"))
            {
                client.DefaultRequestHeaders.Add("User-Agent", "WeatherApplication/1.0");
            }

            LoadWeatherAsync();
        }

        private async void LoadWeatherAsync()
        {
            try
            {
                await FetchWeatherAsync();

                // Show all the weather elements after data is loaded
                WeatherImage.Visibility = Visibility.Visible;
                TemperatureText.Visibility = Visibility.Visible;
                ConditionText.Visibility = Visibility.Visible;
                StatsPanel.Visibility = Visibility.Visible;
            }
            catch (Exception err)
            {
                MessageBox.Show($"Error loading weather: {err.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private async Task FetchWeatherAsync()
        {
            // coordinates for the city
            var (lat, lon, cityName) = await GetCoordinatesAsync(_city);

            // Update the location text with city name
            LocationText.Text = $"Currently in {cityName}";

            // fetch weather data
            string apiUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current_weather=true&hourly=relative_humidity_2m,precipitation_probability&timezone=auto";
            string json = await client.GetStringAsync(apiUrl);

            using JsonDocument doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var current = root.GetProperty("current_weather");

            // temperature
            double temp = current.GetProperty("temperature").GetDouble();
            TemperatureText.Text = $"{temp}°C";

            // wind speed
            double windSpeed = current.GetProperty("windspeed").GetDouble();
            WindText.Text = $"{windSpeed} km/h";

            // Check if it's night
            int isDay = current.GetProperty("is_day").GetInt32();
            bool isNight = (isDay == 0);

            // Weather condition
            int code = current.GetProperty("weathercode").GetInt32();
            string condition = "Unknown";
            string gifPath = "Assets/cloudy.gif";

            switch (code)
            {
                case 0:
                    condition = isNight ? "Clear Night" : "Clear";
                    gifPath = isNight ? "Assets/moon.gif" : "Assets/sun.gif";
                    break;
                case 1:
                    condition = isNight ? "Mainly Clear Night" : "Mainly Clear";
                    gifPath = isNight ? "Assets/moon.gif" : "Assets/sun.gif";
                    break;
                case 2:
                    condition = "Partly Cloudy";
                    gifPath = "Assets/cloudy.gif";
                    break;
                case 3:
                    condition = "Overcast";
                    gifPath = "Assets/cloudy.gif";
                    break;
                case 45:
                case 48:
                    condition = "Foggy";
                    gifPath = "Assets/cloudy.gif";
                    break;
                case 51:
                case 53:
                case 55:
                case 61:
                case 63:
                case 65:
                case 80:
                case 81:
                case 82:
                    condition = "Rain";
                    gifPath = "Assets/rain.gif";
                    break;
                case 71:
                case 73:
                case 75:
                case 77:
                case 85:
                case 86:
                    condition = "Snow";
                    gifPath = "Assets/snow.gif";
                    break;
                case 95:
                case 96:
                case 99:
                    condition = "Thunderstorm";
                    gifPath = "Assets/rain.gif";
                    break;
            }

            ConditionText.Text = condition;

            // Update GIF
            try
            {
                var image = new BitmapImage(new Uri(gifPath, UriKind.Relative));
                ImageBehavior.SetAnimatedSource(WeatherImage, image);
            }
            catch
            {
                // If GIF fails to load just continue
            }

            // get humidity and precipitation from hourly data
            try
            {
                var hourly = root.GetProperty("hourly");
                var humidity = hourly.GetProperty("relative_humidity_2m");
                var precipitation = hourly.GetProperty("precipitation_probability");

                if (humidity.GetArrayLength() > 0)
                {
                    HumidityText.Text = $"{humidity[0].GetInt32()}%";
                }

                if (precipitation.GetArrayLength() > 0)
                {
                    var precipValue = precipitation[0];
                    if (precipValue.ValueKind != JsonValueKind.Null)
                    {
                        PrecipitationText.Text = $"{precipValue.GetInt32()}%";
                    }
                    else
                    {
                        PrecipitationText.Text = "0%";
                    }
                }
            }
            catch
            {
                // Use defaults if hourly data is not available
                HumidityText.Text = "N/A";
                PrecipitationText.Text = "N/A";
            }
        }

        private async Task<(double lat, double lon, string cityName)> GetCoordinatesAsync(string city)
        {
            string url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(city)}&format=json&limit=1";

            string response = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            var results = doc.RootElement;

            if (results.GetArrayLength() == 0)
                throw new Exception("City not found. Please check the spelling and try again.");

            var first = results[0];
            double lat = double.Parse(first.GetProperty("lat").GetString());
            double lon = double.Parse(first.GetProperty("lon").GetString());
            string cityName = first.GetProperty("display_name").GetString().Split(',')[0]; 

            return (lat, lon, cityName);
        }
    }
}