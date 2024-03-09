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
    private readonly LoginService loginService;
    private readonly SaveFileinDB saveFileinDB;
    private readonly StaredService staredService;
    private readonly TrashService trshService;
    private readonly GetAllStared getallstared;
    public Function()
    {
        fileUpload = new FileUpload();
        authentication = new Authentication();
        verificationService = new VerificationService();
        fileService = new FileService();
        loginService = new LoginService();
        saveFileinDB = new SaveFileinDB();
        staredService = new StaredService();
        trshService = new TrashService();
        getallstared = new GetAllStared();
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


    public async Task<APIGatewayHttpApiV2ProxyResponse> LoginServiceHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        return await loginService.SignInFunctionHandler(request, context);
    }


    public async Task<APIGatewayHttpApiV2ProxyResponse> SaveFiletoDBServiceHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        return await saveFileinDB.SaveFileFunctionHandler(request, context);
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> StaredServiceHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        return await staredService.StaredFunctionHandler(request, context);
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> TrashServiceHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        return await trshService.TrashedFunctionHandler(request, context);
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> GetAllTrashServiceHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        return await getallstared.GetAllStaredFunctionHandler(request, context);
    }
}
