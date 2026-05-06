import { App } from "@modelcontextprotocol/ext-apps";

// ── Helpers ──────────────────────────────────────────────────────────
const el = (id: string) => document.getElementById(id)!;

// ── Types ────────────────────────────────────────────────────────────
interface MetricItem {
  Label?: string;
  label?: string;
  Value?: number;
  value?: number;
}

interface DashboardData {
  // Core info (PascalCase from C# serializer)
  Status?: string;
  Runtime?: string;
  Timestamp?: string;
  Uptime?: string;
  Environment?: string;
  MemoryMB?: number;
  // Chart data
  ChartTitle?: string;
  Metrics?: MetricItem[];
}

// ── Rendering ────────────────────────────────────────────────────────

function renderTiles(data: DashboardData): void {
  const tilesContainer = el("tiles");
  tilesContainer.innerHTML = "";

  const tiles: { label: string; value: string }[] = [
    { label: "Status", value: data.Status ?? "—" },
    { label: "Runtime", value: data.Runtime ?? "—" },
    { label: "Environment", value: data.Environment ?? "—" },
    { label: "Memory", value: data.MemoryMB != null ? `${data.MemoryMB} MB` : "—" },
    { label: "Uptime", value: data.Uptime ?? "—" },
  ];

  for (const t of tiles) {
    const tile = document.createElement("div");
    tile.className = "tile";
    tile.innerHTML = `<h3>${t.label}</h3><div class="value">${t.value}</div>`;
    tilesContainer.appendChild(tile);
  }
}

function renderChart(data: DashboardData): void {
  const section = el("chart-section");
  const chart = el("bar-chart");
  const title = el("chart-title");

  if (!data.Metrics || data.Metrics.length === 0) {
    section.style.display = "none";
    return;
  }

  section.style.display = "";
  title.textContent = data.ChartTitle ?? "Metrics";
  chart.innerHTML = "";

  const val = (m: MetricItem) => m.Value ?? m.value ?? 0;
  const lbl = (m: MetricItem) => m.Label ?? m.label ?? "";
  const maxVal = Math.max(...data.Metrics.map(val), 1);

  for (const metric of data.Metrics) {
    const v = val(metric);
    const pct = (v / maxVal) * 100;
    const group = document.createElement("div");
    group.className = "bar-group";
    group.innerHTML = `
      <span class="bar-value">${v}</span>
      <div class="bar" style="height: ${Math.max(pct, 2)}%"></div>
      <span class="bar-label">${lbl(metric)}</span>`;
    chart.appendChild(group);
  }
}

function render(data: DashboardData): void {
  el("waiting").style.display = "none";
  el("status-dot").classList.toggle("online", data.Status === "Online");
  renderTiles(data);
  renderChart(data);

  if (data.Timestamp) {
    el("footer").textContent = `Last updated: ${data.Timestamp}`;
  }
}

// ── Parse tool result content blocks ─────────────────────────────────

function parseToolResult(
  content: Array<{ type: string; text?: string }> | undefined
): DashboardData | null {
  if (!content || content.length === 0) return null;
  const textBlock = content.find((c) => c.type === "text" && c.text);
  if (!textBlock?.text) return null;
  try {
    return JSON.parse(textBlock.text) as DashboardData;
  } catch (e) {
    console.error("Dashboard parse error:", e);
    return null;
  }
}

// ── MCP App lifecycle ────────────────────────────────────────────────

const app = new App({ name: "Snippet Dashboard", version: "1.0.0" });

app.ontoolinput = (params) => {
  console.log("Tool input:", params.arguments);
  app.sendLog({ level: "info", data: `Tool input: ${JSON.stringify(params.arguments)}` });
};

app.ontoolresult = (params) => {
  console.log("Tool result:", params.content);
  const data = parseToolResult(
    params.content as Array<{ type: string; text?: string }>
  );
  if (data) {
    render(data);
  } else {
    el("waiting").textContent = "Error parsing dashboard data";
  }
};

app.onhostcontextchanged = (ctx) => {
  if (ctx.theme) {
    document.documentElement.dataset.theme = ctx.theme;
  }
};

await app.connect();

const theme = app.getHostContext()?.theme;
if (theme) document.documentElement.dataset.theme = theme;

el("footer").textContent = "Connected — invoke the tool to see live data";
