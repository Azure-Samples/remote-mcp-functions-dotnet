using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using static FunctionsSnippetTool.ToolsInformation;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.EnableMcpToolMetadata();

// Demonstrate how you can define tool properties without requiring
// input bindings:
builder
    .ConfigureMcpTool(GetSnippetToolName)
    .WithProperty(SnippetNamePropertyName, StringPropertyType, SnippetNamePropertyDescription);

// Example of configuring complex types for the order processing tool
builder
    .ConfigureMcpTool("process_order")
    .WithProperty("order-items", ArrayPropertyType, "List of order items, each containing item ID, quantity, and price")
    .WithProperty("customer-name", StringPropertyType, "Name of the customer placing the order")
    .WithProperty("is-urgent", BooleanPropertyType, "Whether this order should be processed urgently")
    .WithProperty("discount-percent", NumberPropertyType, "Discount percentage to apply (0-100)");

builder.Build().Run();
