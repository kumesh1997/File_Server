using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

namespace fileserver.Services
{
    public class DownloadService
    {
        private readonly DynamoDBContext _dynamoDbContext;

        public DownloadService()
        {
            // Instance of ConnectToBynamoDB 
            _dynamoDbContext = new DynamoDBContext(new AmazonDynamoDBClient());
        }


        public async Task<APIGatewayHttpApiV2ProxyResponse> DownloadFunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
        {
            string httpMethod = request.RequestContext.Http.Method.ToUpper();

            if (httpMethod == "OPTIONS")
            {
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 200,
                    Headers = new Dictionary<string, string>
                {
                    { "Access-Control-Allow-Origin", "http://localhost:3000" },
                    { "Access-Control-Allow-Headers", "Content-Type" },
                    { "Access-Control-Allow-Methods", "OPTIONS,POST,GET" },
                    { "Access-Control-Allow-Credentials", "true" },
                },
                };
            }
            if (httpMethod == "POST" && request.Body == null && request.PathParameters == null)
            {
                return await HandleDownloadRequest(request);
            }
            // Handle unsupported or unrecognized HTTP methods
            return new APIGatewayHttpApiV2ProxyResponse { StatusCode = 400 };
        }

        private async Task<APIGatewayHttpApiV2ProxyResponse> HandleDownloadRequest(APIGatewayHttpApiV2ProxyRequest request)
        {
            try
            {
                return null;
            }catch (Exception ex)
            {
                return null; 
            }
        }
    }
}
