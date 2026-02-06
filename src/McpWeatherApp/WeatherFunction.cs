using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated;

public class WeatherFunction
{
    private readonly ILogger<WeatherFunction> _logger;
    private readonly WeatherService _weatherService;

    private const string ToolMetadata = """
        {
            "ui": {
                "resourceUri": "ui://weather/index.html"
            }
        }
        """;

    private const string ResourceMetadata = """
        {
            "ui": {
                "prefersBorder": true
            }
        }
        """;

    public WeatherFunction(ILogger<WeatherFunction> logger)
    {
        _logger = logger;
        _weatherService = new WeatherService(WeatherService.CreateDefaultClient());
    }

    [Function(nameof(GetWeatherWidget))]
    public string GetWeatherWidget(
        [McpResourceTrigger(
            "ui://weather/index.html",
            "Weather Widget",
            MimeType = "text/html;profile=mcp-app",
            Description = "Interactive weather display for MCP Apps")]
        [McpMetadata(ResourceMetadata)]
            ResourceInvocationContext context)
    {
        var file = Path.Combine(AppContext.BaseDirectory, "app", "dist", "index.html");
        return File.ReadAllText(file);
    }

    [Function(nameof(GetWeather))]
    public async Task<object> GetWeather(
        [McpToolTrigger(nameof(GetWeather), "Returns current weather for a location via Open-Meteo.")]
        [McpMetadata(ToolMetadata)]
            ToolInvocationContext context,
        [McpToolProperty("location", "City name to check weather for (e.g., Seattle, New York, Miami)")]
            string location)
    {
        try
        {
            var result = await _weatherService.GetCurrentWeatherAsync(location);
            
            if (result is WeatherResult weather)
            {
                _logger.LogInformation("Weather fetched for {Location}: {TempC}°C", weather.Location, weather.TemperatureC);
            }
            else if (result is WeatherError error)
            {
                _logger.LogWarning("Weather error for {Location}: {Error}", error.Location, error.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get weather for {Location}", location);
            return new WeatherError(location ?? "Unknown", $"Unable to fetch weather: {ex.Message}", "api.open-meteo.com");
        }
    }
}
