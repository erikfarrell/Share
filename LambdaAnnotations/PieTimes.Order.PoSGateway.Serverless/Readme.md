# LambdaAnnotations README

- [LambdaAnnotations Project Documentation](./Documentation/LambdaAnnotations.md)

# Lambda Annotations Review

This is a review for US#925941 to examine Lambda Annotations' suitability for SLM and benefits it may give.

We'll also explore some different ways to structure repositories.

## Lambda Annotations - Features

### Amazon.Lambda.Annotations.LambdaStartup

The `[LambdaStartup]` attribute allows us to perform dependency injection in our lambda. I believe there are a few requirements:

- The `[LambdaFunction]` attribute needs to be applied to your function. This creates an entry in the YAML/JSON template specified in `aws-lambda-tools-defaults.json` automatically. You can perform modifications to the generated CFT, such as adding `AutoPublishAlias` or links to other resources.
- You need to use the generated version of the lambda - this will be what's automatically populated in the `Handler` property of your `AWS::Serverless::Function` by Lambda Annotations
- To my knowledge this only works with `sam build`/`sam deploy`

LambdaStartup allows you to stand up a Startup function to dependency inject your entire function in the typical Microsoft `Startup.cs` paradigm. For me, this is one of its most attractive features.

Example (Setup):

```c#
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
```

Example (Usage):

```c#
private ILogger _logger;
private IDynamoDBContext _context;

public Functions(ILogger logger, IDynamoDBContext context)
{
    _logger = logger;
    _context = context;
}
```

### LambdaFunction Attribute

The `[LambdaFunction]` attribute generates the lambda in the template provided by `aws-lambda-tools-defaults.json` - as mentioned prior, you can make modifications to this template, and by and large they won't be overwritten.

There are a number of properties on `[LambdaFunction]` - the one I tend to use most is `ResourceName`, which corresponds to the name your CloudFormation resource is generated as.

### RestApi/HttpApi Attribute

There are a couple of API options to generate with, RestApi or HttpApi, and corresponding attributes for each (`[HttpApi]` or `[RestApi]`). A guide for choosing between the two can be found here: https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-vs-rest.html - I believe HttpApi is a touch cheaper. I've used RestApi throughout.

Example uses:

```c#
[LambdaFunction(ResourceName = "PoSGatewayGetOrderServerlessFunction")]
[RestApi(LambdaHttpMethod.Get, "/Order/{orderId}")]
public async Task<APIGatewayProxyResponse> GetOrder(string orderId)
{

}

[LambdaFunction(ResourceName = "PoSGatewaySubmitOrderServerlessFunction")]
[RestApi(LambdaHttpMethod.Post, "/SubmitOrder")]
public async Task<APIGatewayProxyResponse> SubmitOrder([FromBody] Request<OrderDTO> request, ILambdaContext context)
{

}
```

Out of the box, RestApi is going to create an entire API gateway for you, using the HTTP method and pathing you specified in your attribute. You can customize this to point to your own `AWS::Serverless::Api` resource by using the `RestApiId` property in your CloudFormation template.

### Parameter Attributes

Several parameter attributes are mentioned in the [LambdaAnnotations Project Documentation](./Documentation/LambdaAnnotations.md)

One I'm using is `[FromBody]` to serialize in my lambda payload like so:

```c#
[LambdaFunction(ResourceName = "PoSGatewaySubmitOrderServerlessFunction")]
[RestApi(LambdaHttpMethod.Post, "/SubmitOrder")]
public async Task<APIGatewayProxyResponse> SubmitOrder([FromBody] Request<OrderDTO> request, ILambdaContext context)
{

}
```

After `[LambdaStartup]` this is probably my second favorite feature.

## Lambda Annotations - Limitations

- I do not believe there is a way to use the `[RestApi]` attribute for generating pathing cross-account. This limits its utility if you have your `AWS::Serverless::Api` in a different account from your `AWS::Serverless::Function`
  - You can simply omit `[RestApi]` (or `[HttpApi]`) and forego the generation of API gateway pathing - you get a little less utility out of annotations but you can still leverage `[LambdaStartup]`, `[LambdaFunction]`, `[FromBody]`, etc.
- You can only have one lambda annotations project per template. If another project generates lambda annotations functions to your template, it will overwrite all the generated lambdas from the previous project.

## Lambda Annotations - Additional Notes

- If you use `[RestApi]` and you have `AutoPublishAlias` on your lambda, your generated API is smart enough to use your alias.
- You can use lambda annotations on a non-gateway lambdas (with the obvious exception of the `[RestApi]`/`[HttpApi]` parameter). This might suggest some alternative organizational structures at your disposal - such as placing multiple lambda functions in the same project and stack as facades.
  - This could be a maintenance benefit, maintaining deployment/practical decoupling of lambdas but allowing them to ride alongside each other in projects and repositories
  - You get to leverage nice things like `[LambdaStartup]` as a cleaner injection philosophy than other available paradigms