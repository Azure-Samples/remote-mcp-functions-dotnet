using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace TestAppIsolated;

public record WeatherResult(
    string Location,
    string Condition,
    int? TemperatureC,
    int? TemperatureF,
    int? HumidityPercent,
    int? WindKph,
    string Wind,
    string ReportedAtUtc,
    string Source);

public record WeatherError(string Location, string Error, string Source);

public class WeatherService
{
    private readonly HttpClient _weatherHttp;
    private readonly HttpClient _geocodeHttp;

    public WeatherService(HttpClient weatherHttp, HttpClient? geocodeHttp = null)
    {
        _weatherHttp = weatherHttp;
        _geocodeHttp = geocodeHttp ?? CreateGeocodingClient();
    }

    public static HttpClient CreateDefaultClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://api.open-meteo.com/")
        };
        // client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "mcp-weather-sample/1.0 (+https://example.com; contact=ops@example.com)");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static HttpClient CreateGeocodingClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://geocoding-api.open-meteo.com/")
        };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public async Task<object> GetCurrentWeatherAsync(string location)
    {
        var normalized = NormalizeLocation(location);
        
        var coords = await GeocodeAsync(normalized);
        if (coords is null)
        {
            return new WeatherError(normalized, "Could not find this location. Try a city, address, or zip code.", "open-meteo");
        }

        var (lat, lon, canonical) = coords.Value;

        var observation = await GetLatestObservationAsync(lat, lon);
        if (observation is null)
        {
            return new WeatherError(canonical, "Could not retrieve current observations.", "open-meteo");
        }

        return ParseObservation(observation.Value, canonical);
    }

    private static string NormalizeLocation(string location)
    {
        return string.IsNullOrWhiteSpace(location) 
            ? "Seattle, WA" 
            : location.Trim();
    }

    private async Task<(double Lat, double Lon, string Canonical)?> GeocodeAsync(string location)
    {
        // Open-Meteo geocoding (global, no key)
        var encoded = HttpUtility.UrlEncode(location);
        var url = $"v1/search?name={encoded}&count=1&language=en&format=json";

        var response = await _geocodeHttp.GetFromJsonAsync<JsonElement>(url);

        if (!response.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array ||
            results.GetArrayLength() == 0)
        {
            return null;
        }

        var match = results[0];
        var lat = match.GetProperty("latitude").GetDouble();
        var lon = match.GetProperty("longitude").GetDouble();
        var name = match.GetProperty("name").GetString() ?? location;
        var admin1 = match.TryGetProperty("admin1", out var adm1) ? adm1.GetString() : null;
        var country = match.TryGetProperty("country", out var ctry) ? ctry.GetString() : null;
        var canonical = string.Join(", ", new[] { name, admin1, country }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return (lat, lon, string.IsNullOrWhiteSpace(canonical) ? location : canonical);
    }

    private async Task<JsonElement?> GetLatestObservationAsync(double lat, double lon)
    {
        var url = $"v1/forecast?latitude={lat.ToString(CultureInfo.InvariantCulture)}&longitude={lon.ToString(CultureInfo.InvariantCulture)}&current=temperature_2m,relative_humidity_2m,wind_speed_10m,wind_direction_10m,weather_code";
        var observation = await _weatherHttp.GetFromJsonAsync<JsonElement>(url);
        if (observation.TryGetProperty("current", out var current))
        {
            return current;
        }
        return null;
    }

    private static WeatherResult ParseObservation(JsonElement props, string location)
    {
        var tempC = GetNullableDouble(props, "temperature_2m");
        var tempF = tempC.HasValue ? (int)Math.Round(tempC.Value * 1.8 + 32) : (int?)null;

        var condition = props.TryGetProperty("weather_code", out var codeProp) && codeProp.ValueKind == JsonValueKind.Number
            ? MapWeatherCode(codeProp.GetInt32())
            : "Unknown";

        var humidity = GetNullableDouble(props, "relative_humidity_2m");

        var wind = GetNullableDouble(props, "wind_speed_10m");
        var windKph = wind.HasValue ? (int)Math.Round(wind.Value) : (int?)null;

        var windDirDeg = GetNullableDouble(props, "wind_direction_10m");
        var windDir = windDirDeg.HasValue ? $" {DegToCardinal(windDirDeg.Value)}" : string.Empty;

        var reportedTime = props.TryGetProperty("time", out var tsProp) && tsProp.ValueKind == JsonValueKind.String
            ? DateTime.Parse(tsProp.GetString()!, CultureInfo.InvariantCulture)
            : DateTime.UtcNow;

        return new WeatherResult(
            Location: location,
            Condition: condition,
            TemperatureC: tempC.HasValue ? (int)Math.Round(tempC.Value) : null,
            TemperatureF: tempF,
            HumidityPercent: humidity.HasValue ? (int)Math.Round(humidity.Value) : null,
            WindKph: windKph,
            Wind: windKph.HasValue ? $"{windKph} km/h{windDir}" : "—",
            ReportedAtUtc: reportedTime.ToUniversalTime().ToString("u"),
            Source: "open-meteo"
        );
    }

    private static double? GetNullableDouble(JsonElement props, string propertyName)
    {
        if (props.TryGetProperty(propertyName, out var prop) &&
            prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetDouble();
        }
        return null;
    }

    private static string MapWeatherCode(int code)
    {
        return code switch
        {
            0 => "Clear sky",
            1 => "Mainly clear",
            2 => "Partly cloudy",
            3 => "Overcast",
            45 or 48 => "Fog",
            51 or 53 or 55 => "Drizzle",
            56 or 57 => "Freezing drizzle",
            61 or 63 or 65 => "Rain",
            66 or 67 => "Freezing rain",
            71 or 73 or 75 => "Snowfall",
            77 => "Snow grains",
            80 or 81 or 82 => "Rain showers",
            85 or 86 => "Snow showers",
            95 => "Thunderstorm",
            96 or 99 => "Thunderstorm with hail",
            _ => "Unknown"
        };
    }

    private static string DegToCardinal(double deg)
    {
        string[] dirs = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
        return dirs[(int)Math.Round(((deg % 360) / 22.5), MidpointRounding.AwayFromZero) % 16];
    }
}
