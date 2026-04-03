using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using static FunctionsMcpPrompts.PromptsInformation;

namespace FunctionsMcpPrompts;

public class PromptSamples(ILogger<PromptSamples> logger)
{
    /// <summary>
    /// Simple prompt with no arguments. Returns a static code review checklist.
    /// Demonstrates the basic McpPromptTrigger usage.
    /// </summary>
    [Function(nameof(CodeReviewChecklist))]
    public string CodeReviewChecklist(
        [McpPromptTrigger(CodeReviewPromptName, Description = CodeReviewPromptDescription)]
            PromptInvocationContext context)
    {
        logger.LogInformation("Code review checklist prompt invoked.");

        return """
            You are a senior software engineer performing a code review.
            Use the following checklist to evaluate the code:

            1. **Correctness** — Does the code do what it's supposed to?
            2. **Error Handling** — Are edge cases and failures handled?
            3. **Security** — Are there any vulnerabilities (injection, auth, secrets)?
            4. **Performance** — Are there obvious inefficiencies?
            5. **Readability** — Is the code clear and well-named?
            6. **Tests** — Are there adequate tests for the changes?

            Provide your feedback in a structured format with a severity level
            (critical, warning, suggestion) for each finding.
            """;
    }

    /// <summary>
    /// Prompt with arguments defined via McpPromptArgument input bindings.
    /// Generates a context-aware summarization prompt for a given topic and audience.
    /// </summary>
    [Function(nameof(SummarizeContent))]
    public string SummarizeContent(
        [McpPromptTrigger(SummarizePromptName, Description = SummarizePromptDescription)]
            PromptInvocationContext context,
        [McpPromptArgument("topic", "The topic or content to summarize.", isRequired: true)]
            string topic,
        [McpPromptArgument("audience", "Target audience (e.g., 'executive', 'developer', 'beginner').")]
            string? audience)
    {
        logger.LogInformation("Summarize prompt invoked for topic: {Topic}", topic);

        var audienceInstruction = audience is not null
            ? $"Tailor the summary for a **{audience}** audience."
            : "Write the summary for a general technical audience.";

        return $"""
            Summarize the following topic concisely and accurately:

            **Topic:** {topic}

            {audienceInstruction}

            Guidelines:
            - Start with a one-sentence overview.
            - Include 3–5 key points as bullet items.
            - End with a brief conclusion or recommendation.
            - Keep the total length under 300 words.
            """;
    }

    /// <summary>
    /// Prompt whose arguments are configured entirely in Program.cs using
    /// ConfigureMcpPrompt, rather than McpPromptArgument input binding attributes.
    /// </summary>
    [Function(nameof(GenerateDocumentation))]
    public string GenerateDocumentation(
        [McpPromptTrigger(GenerateDocsPromptName, Description = GenerateDocsPromptDescription)]
            PromptInvocationContext context)
    {
        var functionName = context.Arguments?.GetValueOrDefault("function_name") ?? "(unknown)";
        var style = context.Arguments?.GetValueOrDefault("style") ?? "concise";

        logger.LogInformation("Generate docs prompt invoked for function: {FunctionName}", functionName);

        return $"""
            Generate API documentation for the function named **{functionName}**.

            Documentation style: **{style}**

            Include the following sections:
            - **Description** — What the function does.
            - **Parameters** — List each parameter with its type and purpose.
            - **Return Value** — What the function returns.
            - **Example Usage** — A short code example showing how to call it.
            """;
    }
}
