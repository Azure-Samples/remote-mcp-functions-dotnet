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

    // Hello tool with auth, demonstrating On-Behalf-Of flow to call Microsoft Graph as the user
    public const string HelloToolWithAuthName = "hello_tool_with_auth";
    public const string HelloToolWithAuthDescription =
        "Responds to the user with a hello message using OBO auth.";    

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

    // Prompts
    public const string CodeReviewPromptName = "code_review_checklist";
    public const string CodeReviewPromptDescription =
        "Returns a structured code review checklist prompt for evaluating code changes.";
    public const string SummarizePromptName = "summarize_content";
    public const string SummarizePromptDescription =
        "Generates a summarization prompt tailored to a given topic and audience.";
    public const string GenerateDocsPromptName = "generate_documentation";
    public const string GenerateDocsPromptDescription =
        "Generates API documentation for a function. Arguments are configured in Program.cs.";
    public const string GenerateDocsFunctionNameArgName = "function_name";
    public const string GenerateDocsFunctionNameArgDescription =
        "The name of the function to document.";
    public const string GenerateDocsStyleArgName = "style";
    public const string GenerateDocsStyleArgDescription =
        "Documentation style: 'concise', 'detailed', or 'tutorial'.";

    // Resource templates
    public const string SnippetResourceTemplateUri = "snippet://{Name}";
    public const string SnippetResourceName = "Snippet";
    public const string SnippetResourceDescription =
        "Reads a code snippet by name from blob storage.";
    public const string ServerInfoResourceUri = "info://server";
    public const string ServerInfoResourceName = "ServerInfo";
    public const string ServerInfoResourceDescription =
        "Returns information about the MCP server, including version and runtime.";
}
