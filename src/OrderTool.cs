using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static FunctionsSnippetTool.ToolsInformation;

namespace FunctionsSnippetTool;

public class OrderTool(ILogger<OrderTool> logger)
{
    public class OrderItem
    {
        public string ItemId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class OrderSummary
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<OrderItem> Items { get; set; } = new();
        public bool IsUrgent { get; set; }
    }

    [Function(nameof(ProcessOrder))]
    public string ProcessOrder(
        [McpToolTrigger("process_order", "Process an order with multiple items")]
            ToolInvocationContext context,
        [McpToolProperty("order-items", ArrayPropertyType, "List of order items, each containing item ID, quantity, and price")]
            List<OrderItem> orderItems,
        [McpToolProperty("customer-name", StringPropertyType, "Name of the customer placing the order")]
            string customerName,
        [McpToolProperty("is-urgent", BooleanPropertyType, "Whether this order should be processed urgently")]
            bool isUrgent,
        [McpToolProperty("discount-percent", NumberPropertyType, "Discount percentage to apply (0-100)")]
            decimal discountPercent = 0
    )
    {
        logger.LogInformation("Processing order for customer: {CustomerName}", customerName);
        
        var totalAmount = orderItems.Sum(item => item.Price * item.Quantity);
        var discountAmount = totalAmount * (discountPercent / 100);
        var finalAmount = totalAmount - discountAmount;

        var orderSummary = new OrderSummary
        {
            OrderId = Guid.NewGuid().ToString("N")[..8],
            TotalAmount = finalAmount,
            Items = orderItems,
            IsUrgent = isUrgent
        };

        var result = JsonSerializer.Serialize(orderSummary, new JsonSerializerOptions { WriteIndented = true });
        
        logger.LogInformation("Order processed successfully. Order ID: {OrderId}", orderSummary.OrderId);
        return result;
    }

    [Function(nameof(ValidateOrderData))]
    public string ValidateOrderData(
        [McpToolTrigger("validate_order", "Validate order data structure")]
            ToolInvocationContext context,
        [McpToolProperty("order-data", ObjectPropertyType, "Complete order data object containing all order information")]
            OrderSummary orderData
    )
    {
        logger.LogInformation("Validating order data for Order ID: {OrderId}", orderData.OrderId);
        
        var validationResults = new List<string>();
        
        if (string.IsNullOrEmpty(orderData.OrderId))
            validationResults.Add("Order ID is required");
            
        if (orderData.TotalAmount <= 0)
            validationResults.Add("Total amount must be greater than zero");
            
        if (orderData.Items == null || orderData.Items.Count == 0)
            validationResults.Add("At least one order item is required");
        else
        {
            for (int i = 0; i < orderData.Items.Count; i++)
            {
                var item = orderData.Items[i];
                if (string.IsNullOrEmpty(item.ItemId))
                    validationResults.Add($"Item {i + 1}: Item ID is required");
                if (item.Quantity <= 0)
                    validationResults.Add($"Item {i + 1}: Quantity must be greater than zero");
                if (item.Price < 0)
                    validationResults.Add($"Item {i + 1}: Price cannot be negative");
            }
        }

        var isValid = validationResults.Count == 0;
        var result = new
        {
            IsValid = isValid,
            ValidationErrors = validationResults,
            OrderId = orderData.OrderId
        };

        logger.LogInformation("Order validation completed. Valid: {IsValid}", isValid);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}