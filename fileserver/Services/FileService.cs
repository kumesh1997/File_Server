using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using fileserver.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace fileserver.Services
{
    public class FileService
    {
        private readonly DynamoDBContext _dynamoDbContext;

        public FileService()
        {
            // Instance of ConnectToBynamoDB 
            _dynamoDbContext = new DynamoDBContext(new AmazonDynamoDBClient());
        }


        public async Task<APIGatewayHttpApiV2ProxyResponse> FileServiceFunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
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
            if (httpMethod == "GET" && request.Body == null && request.PathParameters == null)
            {
                return await GetAllFiles(request);
            }
            // Handle unsupported or unrecognized HTTP methods
            return new APIGatewayHttpApiV2ProxyResponse { StatusCode = 400 };
        }

        private async Task<APIGatewayHttpApiV2ProxyResponse> GetAllFiles(APIGatewayHttpApiV2ProxyRequest request)
        {
            try
            {
                // List of Help Desk Requests
                var requestsList = await _dynamoDbContext.ScanAsync<FileDetails>(default).GetRemainingAsync();
                if (requestsList != null && requestsList.Count > 0)
                {
                    var filteredList = requestsList.Where(v => v.Trash != true).ToList();
                    return new APIGatewayHttpApiV2ProxyResponse()
                    {
                        // The counted value of each category is included in the response body 
                        Body = JsonSerializer.Serialize(filteredList),
                        StatusCode = 200
                    };
                }
                return BadResponse("No File Found !!!!");
            }
            catch (Exception ex)
            {
                return new APIGatewayHttpApiV2ProxyResponse()
                {
                    // The counted value of each category is included in the response body 
                    Body = $"Exception {ex.Message}",
                    StatusCode = 200
                };
            }
        }

        // OK Response
        private static APIGatewayHttpApiV2ProxyResponse OkResponse() =>
            new APIGatewayHttpApiV2ProxyResponse()
            {
                StatusCode = 200
            };

        // Bad Response
        private static APIGatewayHttpApiV2ProxyResponse BadResponse(string message)
        {
            return new APIGatewayHttpApiV2ProxyResponse()
            {
                Body = message,
                StatusCode = 404
            };
        }
    }
}
