using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using fileserver.Services;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace fileserver;

public class Function
{
    private readonly FileUpload fileUpload;
    private readonly Authentication authentication;
    private readonly VerificationService verificationService;
    private readonly FileService fileService;
    public Function()
    {
        fileUpload = new FileUpload();
        authentication = new Authentication();
        verificationService = new VerificationService();
        fileService = new FileService();
    }
    public async Task<APIGatewayHttpApiV2ProxyResponse> FileUploadHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        return await fileUpload.UploadFunctionHandler(request, context);
    }
    public async Task<APIGatewayHttpApiV2ProxyResponse> AuthenticationHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        return await authentication.AuthenticationFunctionHandler(request, context);
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> VerificationHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        return await verificationService.VerificationFunctionHandler(request, context);
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> GetAllFileServiceHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        return await fileService.FileServiceFunctionHandler(request, context);
    }
}
