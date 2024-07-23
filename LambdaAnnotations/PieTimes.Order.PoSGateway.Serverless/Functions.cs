using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;
using PieTimes.Model;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using PieTimes.Enum;
using PieTimes.Helper;
using Amazon.Runtime.Internal;
using Amazon.DynamoDBv2.DataModel;
using OrderDTO = PieTimes.Model.DTO.Order;
using OrderEntity = PieTimes.Order.DynamoDb.Tables.Order;
using System.Text.Json.Serialization;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PieTimes.Order.PoSGateway.Serverless;

/// <summary>
/// A collection of sample Lambda functions that provide a REST api for doing simple math calculations. 
/// </summary>
public class Functions
{
    private ILogger _logger;
    private IDynamoDBContext _context;

    public Functions(ILogger logger, IDynamoDBContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Root route that provides information about the other requests that can be made.
    /// </summary>
    /// <returns>API descriptions.</returns>
    [LambdaFunction(ResourceName = "PoSGatewayDefaultServerlessFunction")]
    [RestApi(LambdaHttpMethod.Get, "/")]
    public string Default(ILambdaContext context)
    {
        var docs = @"Welcome to the Order Gateway for PieTimes!";

        context.Logger.LogInformation("Entered default gateway");

        return docs;
    }

    [Tracing]
    [Logging(ClearState = true, LogEvent = true)]
    [LambdaFunction(ResourceName = "PoSGatewaySubmitOrderServerlessFunction")]
    [RestApi(LambdaHttpMethod.Post, "/SubmitOrder")]
    public async Task<APIGatewayProxyResponse> SubmitOrder([FromBody] Request<OrderDTO> request, ILambdaContext context)
    {
        return await SubmitOrderToBus(request);
    }

    [LambdaFunction(ResourceName = "PoSGatewayGetOrderServerlessFunction")]
    [RestApi(LambdaHttpMethod.Get, "/Order/{orderId}")]
    public async Task<APIGatewayProxyResponse> GetOrder(string orderId)
    {
        var orderEntity = await _context.LoadAsync<OrderEntity>(orderId);

        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = $"Order: {JsonSerializer.Serialize(orderEntity)}",
            Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
        };
    }

    public async Task<APIGatewayProxyResponse> SubmitOrderToBus(Request<OrderDTO> order)
    {
        Tracing.AddAnnotation(nameof(order.Payload.OrderID), order.Payload.OrderID.ToString());
        Logger.AppendKey(nameof(order.Payload.OrderID), order.Payload.OrderID.ToString());
        await GlobalBusHelper.Publish(_logger, order, BusMessageType.OrderCreation);
        Logger.RemoveKeys(nameof(order.Payload.OrderID));

        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = "Your order has been added!",
            Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
        };
    }
}