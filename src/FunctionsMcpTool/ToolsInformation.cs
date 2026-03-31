namespace FunctionsMcpTool;

internal sealed class ToolsInformation
{
    // Echo tool (properties configured in Program.cs via ConfigureMcpTool)
    public const string EchoToolName = "echo_message";
    public const string EchoToolDescription =
        "Echoes back the provided message. Demonstrates defining tool properties in Program.cs.";
    public const string EchoMessagePropertyName = "message";
    public const string EchoMessagePropertyDescription = "The message to echo back.";

    // Hello tool
    public const string HelloToolName = "hello_tool";
    public const string HelloToolDescription =
        "Simple hello world MCP Tool that responds with a hello message.";

    // Snippet tools
    public const string GetSnippetToolName = "get_snippet";
    public const string GetSnippetToolDescription =
        "Gets a code snippet from your snippet collection.";
    public const string GetSnippetWithMetadataToolName = "get_snippet_with_metadata";
    public const string GetSnippetWithMetadataToolDescription =
        "Gets a code snippet with structured metadata.";
    public const string SaveSnippetToolName = "save_snippet";
    public const string SaveSnippetToolDescription =
        "Saves a code snippet into your snippet collection.";
    public const string SnippetNamePropertyName = "Name";
    public const string SnippetNamePropertyDescription = "The name of the snippet.";
    public const string BatchSaveSnippetsToolName = "batch_save_snippets";
    public const string BatchSaveSnippetsToolDescription =
        "Saves multiple code snippets at once into your snippet collection.";
    public const string SnippetItemsPropertyName = "snippet_items";
    public const string SnippetItemsPropertyDescription =
        "Array of snippets to save, each as an object with a single property where the key is the snippet name and the value is the content. Example: [{\"hello\": \"console.log('hi')\"}, {\"bye\": \"console.log('bye')\"}]";

    // QR code tool
    public const string GenerateQrCodeToolName = "generate_qr_code";
    public const string GenerateQrCodeToolDescription =
        "Generates a QR code image from the provided text.";
    public const string QrCodeTextPropertyName = "text";
    public const string QrCodeTextPropertyDescription = "The text or URL to encode as a QR code.";

    // Badge tool
    public const string GenerateBadgeToolName = "generate_badge";
    public const string GenerateBadgeToolDescription =
        "Generates an SVG status badge with a label and value.";
    public const string BadgeLabelPropertyName = "label";
    public const string BadgeLabelPropertyDescription = "The label on the left side of the badge (e.g., 'build', 'version').";
    public const string BadgeValuePropertyName = "value";
    public const string BadgeValuePropertyDescription = "The value on the right side of the badge (e.g., 'passing', '1.0.0').";
    public const string BadgeColorPropertyName = "color";
    public const string BadgeColorPropertyDescription = "Hex color for the value background (e.g., '#4CAF50' for green). Defaults to green.";

    // Website preview tool
    public const string GetWebsitePreviewToolName = "get_website_preview";
    public const string GetWebsitePreviewToolDescription =
        "Fetches a website's title and description, returning a text summary and resource link.";
    public const string WebsiteUrlPropertyName = "url";
    public const string WebsiteUrlPropertyDescription = "The URL of the website to preview.";
}
