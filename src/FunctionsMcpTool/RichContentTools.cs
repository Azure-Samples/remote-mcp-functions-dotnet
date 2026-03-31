using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using QRCoder;
using static FunctionsMcpTool.ToolsInformation;

namespace FunctionsMcpTool;

public class RichContentTools(ILogger<RichContentTools> logger)
{
    /// <summary>
    /// Demonstrates returning a single ImageContentBlock.
    /// Generates a QR code PNG and returns it as a base64-encoded image.
    /// </summary>
    [Function(nameof(GenerateQrCode))]
    public ImageContentBlock GenerateQrCode(
        [McpToolTrigger(GenerateQrCodeToolName, GenerateQrCodeToolDescription)]
            ToolInvocationContext context,
        [McpToolProperty(QrCodeTextPropertyName, QrCodeTextPropertyDescription, true)]
            string text
    )
    {
        logger.LogInformation("Generating QR code for text of length {Length}", text.Length);

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        byte[] pngBytes = qrCode.GetGraphic(10);

        return new ImageContentBlock
        {
            Data = Convert.ToBase64String(pngBytes),
            MimeType = "image/png"
        };
    }

    /// <summary>
    /// Demonstrates returning multiple content blocks (IList&lt;ContentBlock&gt;).
    /// Generates an SVG status badge and returns it alongside a text description.
    /// </summary>
    [Function(nameof(GenerateBadge))]
    public IList<ContentBlock> GenerateBadge(
        [McpToolTrigger(GenerateBadgeToolName, GenerateBadgeToolDescription)]
            ToolInvocationContext context,
        [McpToolProperty(BadgeLabelPropertyName, BadgeLabelPropertyDescription, true)]
            string label,
        [McpToolProperty(BadgeValuePropertyName, BadgeValuePropertyDescription, true)]
            string value,
        [McpToolProperty(BadgeColorPropertyName, BadgeColorPropertyDescription)]
            string color = "#4CAF50"
    )
    {
        logger.LogInformation("Generating badge: {Label} | {Value}", label, value);

        int labelWidth = label.Length * 7 + 12;
        int valueWidth = value.Length * 7 + 12;
        int totalWidth = labelWidth + valueWidth;

        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{totalWidth}" height="20">
              <rect width="{labelWidth}" height="20" fill="#555"/>
              <rect x="{labelWidth}" width="{valueWidth}" height="20" fill="{color}"/>
              <text x="{labelWidth / 2}" y="14" fill="#fff" text-anchor="middle"
                    font-family="Verdana,sans-serif" font-size="11">{label}</text>
              <text x="{labelWidth + valueWidth / 2}" y="14" fill="#fff" text-anchor="middle"
                    font-family="Verdana,sans-serif" font-size="11">{value}</text>
            </svg>
            """;

        return
        [
            new TextContentBlock { Text = $"Badge: {label} — {value}" },
            new ImageContentBlock
            {
                Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg)),
                MimeType = "image/svg+xml"
            }
        ];
    }

    /// <summary>
    /// Demonstrates returning TextContentBlock and ResourceLinkBlock together.
    /// Fetches basic metadata from a URL and returns it with a resource link.
    /// </summary>
    [Function(nameof(GetWebsitePreview))]
    public async Task<IList<ContentBlock>> GetWebsitePreview(
        [McpToolTrigger(GetWebsitePreviewToolName, GetWebsitePreviewToolDescription)]
            ToolInvocationContext context,
        [McpToolProperty(WebsiteUrlPropertyName, WebsiteUrlPropertyDescription, true)]
            string url
    )
    {
        logger.LogInformation("Fetching website preview for {Url}", url);

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MCPTool/1.0");

        string title = url;
        string description = "No description available.";

        try
        {
            var html = await httpClient.GetStringAsync(url);

            var titleMatch = System.Text.RegularExpressions.Regex.Match(
                html, @"<title[^>]*>(.*?)</title>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (titleMatch.Success)
            {
                title = System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value).Trim();
            }

            var descMatch = System.Text.RegularExpressions.Regex.Match(
                html, """<meta[^>]+name=["']description["'][^>]+content=["'](.*?)["']""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (descMatch.Success)
            {
                description = System.Net.WebUtility.HtmlDecode(descMatch.Groups[1].Value).Trim();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch metadata for {Url}", url);
            description = $"Could not fetch metadata: {ex.Message}";
        }

        return
        [
            new TextContentBlock { Text = $"{title}\n\n{description}" },
            new ResourceLinkBlock
            {
                Uri = url,
                Name = title,
                Description = description
            }
        ];
    }
}
