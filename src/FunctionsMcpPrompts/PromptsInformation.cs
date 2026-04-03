namespace FunctionsMcpPrompts;

internal sealed class PromptsInformation
{
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
}
