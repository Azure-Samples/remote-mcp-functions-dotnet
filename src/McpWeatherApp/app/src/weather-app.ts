import { App } from "@modelcontextprotocol/ext-apps";

// DOM element helper
const el = (id: string) => document.getElementById(id)!;

// Weather data interface
interface WeatherData {
  Location?: string;
  location?: string;
  Condition?: string;
  condition?: string;
  TemperatureC?: number;
  temperatureC?: number;
  TemperatureF?: number;
  temperatureF?: number;
  HumidityPercent?: number;
  humidityPercent?: number;
  Wind?: string;
  wind?: string;
  WindKph?: number;
  windKph?: number;
  ReportedAtUtc?: string;
  reportedAtUtc?: string;
  Source?: string;
  source?: string;
  Error?: string;
}

function pickIcon(condition: string | undefined): string {
  const text = (condition || "").toLowerCase();
  if (text.includes("storm") || text.includes("thunder")) return "⛈️";
  if (text.includes("rain") || text.includes("shower")) return "🌧️";
  if (text.includes("snow")) return "❄️";
  if (text.includes("fog") || text.includes("mist")) return "🌫️";
  if (text.includes("cloud") || text.includes("overcast")) return "☁️";
  if (text.includes("sun") || text.includes("clear") || text.includes("mainly"))
    return "☀️";
  if (text.includes("wind")) return "🌬️";
  return "🌤️";
}

function applyTheme(theme: string | undefined): void {
  document.documentElement.dataset.theme = theme || "dark";
}

function render(data: WeatherData): void {
  // Check for error
  if (data.Error) {
    el("location").textContent = data.Location || "Error";
    el("condition").textContent = data.Error;
    el("weather-icon").textContent = "⚠️";
    el("temperature").textContent = "—";
    el("humidity").textContent = "—";
    el("wind").textContent = "—";
    el("footer").textContent = `Source: ${data.Source || "weather API"}`;
    return;
  }

  // Display weather
  el("location").textContent = data.location || data.Location || "Unknown";
  el("condition").textContent = data.condition || data.Condition || "Unknown";
  el("weather-icon").textContent = pickIcon(data.condition || data.Condition);

  // Temperature
  const tempC = data.temperatureC ?? data.TemperatureC;
  const tempF = data.temperatureF ?? data.TemperatureF;
  const tempText =
    tempF && tempC
      ? `${tempF}°F / ${tempC}°C`
      : tempF !== null && tempF !== undefined
        ? `${tempF}°F`
        : tempC !== null && tempC !== undefined
          ? `${tempC}°C`
          : "—";
  el("temperature").textContent = tempText;

  // Humidity
  const humidity = data.humidityPercent ?? data.HumidityPercent;
  el("humidity").textContent =
    humidity !== null && humidity !== undefined ? `${humidity}%` : "—";

  // Wind
  const wind = data.wind || data.Wind;
  const windKph = data.windKph ?? data.WindKph;
  el("wind").textContent = wind || (windKph ? `${windKph} km/h` : "—");

  // Footer
  const reportedAt = data.reportedAtUtc || data.ReportedAtUtc;
  const source = data.source || data.Source;
  if (reportedAt) {
    el("footer").textContent = `Reported ${reportedAt} · ${source || "weather"}`;
  } else {
    el("footer").textContent = `Source: ${source || "weather API"}`;
  }
}

function parseToolResultContent(content: Array<{ type: string; text?: string }> | undefined): WeatherData | null {
  if (!content || content.length === 0) {
    console.error("No content in tool result");
    return null;
  }

  // Find the first text content block
  const textBlock = content.find((c) => c.type === "text" && c.text);
  if (!textBlock || !textBlock.text) {
    console.error("No text content in tool result");
    return null;
  }

  // Parse the JSON string from the text content
  try {
    return JSON.parse(textBlock.text) as WeatherData;
  } catch (e) {
    console.error("Parse error:", e, textBlock.text);
    return null;
  }
}

// Create app instance
const app = new App({ name: "Weather Widget", version: "1.0.0" });

// Register handlers BEFORE connect (events may occur immediately after connect)

// Handle tool input (arguments passed to the tool)
app.ontoolinput = (params) => {
  console.log("Tool args:", params.arguments);
  app.sendLog({
    level: "info",
    data: `Received tool input: ${JSON.stringify(params.arguments)}`,
  });
};

// Handle tool results from the server
app.ontoolresult = (params) => {
  console.log("Tool result content:", params.content);
  const data = parseToolResultContent(params.content as Array<{ type: string; text?: string }>);
  if (data) {
    render(data);
  } else {
    el("condition").textContent = "Error parsing weather data";
  }
};

// Handle host context changes (theme, locale, etc.)
app.onhostcontextchanged = (ctx) => {
  if (ctx.theme) applyTheme(ctx.theme);
};

// Connect to host (auto-detects OpenAI vs MCP environment)
await app.connect();

// Apply initial theme from host context
applyTheme(app.getHostContext()?.theme);
el("footer").textContent = "Connected";
