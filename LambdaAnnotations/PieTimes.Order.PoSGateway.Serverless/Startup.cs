using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PieTimes.Order.PoSGateway.Serverless;

[Amazon.Lambda.Annotations.LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        Tracing.RegisterForAllServices();
        services.AddSingleton<ILogger>(Logger.Create<Functions>());
        services.AddSingleton<IDynamoDBContext>(new DynamoDBContext(new AmazonDynamoDBClient(new AmazonDynamoDBConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast2 })));
    }
}
