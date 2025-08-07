using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static FunctionsSnippetTool.ToolsInformation;

namespace FunctionsSnippetTool;

public class SnippetsTool(ILogger<SnippetsTool> logger)
{
    private const string BlobPath = "snippets/{mcptoolargs." + SnippetNamePropertyName + "}.json";

    public class SnippetInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    public class SnippetSearchCriteria
    {
        public List<string> Tags { get; set; } = new();
        public string NamePattern { get; set; } = string.Empty;
        public bool IncludeContent { get; set; } = true;
    }

    [Function(nameof(GetSnippet))]
    public object GetSnippet(
        [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)]
            ToolInvocationContext context,
        [BlobInput(BlobPath)] string snippetContent
    )
    {
        return snippetContent;
    }

    [Function(nameof(SaveSnippet))]
    [BlobOutput(BlobPath)]
    public string SaveSnippet(
        [McpToolTrigger(SaveSnippetToolName, SaveSnippetToolDescription)]
            ToolInvocationContext context,
        [McpToolProperty(SnippetNamePropertyName, StringPropertyType, SnippetNamePropertyDescription)]
            string name,
        [McpToolProperty(SnippetPropertyName, StringPropertyType, SnippetPropertyDescription)]
            string snippet
    )
    {
        return snippet;
    }

    [Function(nameof(BulkSaveSnippets))]
    public string BulkSaveSnippets(
        [McpToolTrigger("bulk_save_snippets", "Save multiple code snippets at once")]
            ToolInvocationContext context,
        [McpToolProperty("snippets", ArrayPropertyType, "Array of snippet objects containing name, content, description, and tags")]
            List<SnippetInfo> snippets,
        [McpToolProperty("overwrite-existing", BooleanPropertyType, "Whether to overwrite existing snippets with same names")]
            bool overwriteExisting = false
    )
    {
        logger.LogInformation("Bulk saving {Count} snippets", snippets.Count);
        
        var results = new List<object>();
        foreach (var snippet in snippets)
        {
            try
            {
                // In a real implementation, you'd save to blob storage
                // For demo purposes, we'll just return success status
                results.Add(new
                {
                    Name = snippet.Name,
                    Status = "Success",
                    Message = $"Snippet '{snippet.Name}' saved successfully"
                });
                
                logger.LogInformation("Saved snippet: {Name}", snippet.Name);
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    Name = snippet.Name,
                    Status = "Error", 
                    Message = ex.Message
                });
                logger.LogError(ex, "Failed to save snippet: {Name}", snippet.Name);
            }
        }

        var summary = new
        {
            TotalProcessed = snippets.Count,
            SuccessCount = results.Count(r => ((dynamic)r).Status == "Success"),
            Results = results
        };

        return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
    }

    [Function(nameof(SearchSnippets))]
    public string SearchSnippets(
        [McpToolTrigger("search_snippets", "Search for snippets using various criteria")]
            ToolInvocationContext context,
        [McpToolProperty("search-criteria", ObjectPropertyType, "Search criteria object with tags, name pattern, and content inclusion options")]
            SnippetSearchCriteria searchCriteria
    )
    {
        logger.LogInformation("Searching snippets with criteria: tags={Tags}, pattern={Pattern}", 
            string.Join(",", searchCriteria.Tags), searchCriteria.NamePattern);

        // In a real implementation, you'd query blob storage
        // For demo purposes, return mock search results
        var mockResults = new List<SnippetInfo>
        {
            new() { Name = "hello-world", Content = "console.log('Hello World');", Description = "Basic hello world", Tags = ["javascript", "basic"] },
            new() { Name = "api-request", Content = "fetch('/api/data')", Description = "API request example", Tags = ["javascript", "api"] },
            new() { Name = "linq-query", Content = "items.Where(x => x.IsActive)", Description = "LINQ filtering", Tags = ["csharp", "linq"] }
        };

        // Apply search filters
        var filteredResults = mockResults.AsEnumerable();
        
        if (searchCriteria.Tags.Any())
        {
            filteredResults = filteredResults.Where(s => 
                searchCriteria.Tags.Any(tag => s.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
        }
        
        if (!string.IsNullOrEmpty(searchCriteria.NamePattern))
        {
            filteredResults = filteredResults.Where(s => 
                s.Name.Contains(searchCriteria.NamePattern, StringComparison.OrdinalIgnoreCase));
        }

        var results = filteredResults.Select(s => new
        {
            s.Name,
            s.Description,
            s.Tags,
            Content = searchCriteria.IncludeContent ? s.Content : null
        }).ToList();

        var searchResults = new
        {
            SearchCriteria = searchCriteria,
            ResultCount = results.Count,
            Results = results
        };

        logger.LogInformation("Search completed. Found {Count} matching snippets", results.Count);
        return JsonSerializer.Serialize(searchResults, new JsonSerializerOptions { WriteIndented = true });
    }
}
