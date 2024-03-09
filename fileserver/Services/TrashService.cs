using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using fileserver.DTO;
using fileserver.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace fileserver.Services
{
    public class TrashService
    {
        private readonly DynamoDBContext _dynamoDbContext;

        public TrashService()
        {
            // Instance of ConnectToBynamoDB 
            _dynamoDbContext = new DynamoDBContext(new AmazonDynamoDBClient());
        }


        public async Task<APIGatewayHttpApiV2ProxyResponse> TrashedFunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
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
            if (httpMethod == "POST" && request.Body != null && request.PathParameters == null)
            {
                return await HandleAddTrashRequest(request);
            }
            else if (httpMethod == "POST" && request.Body != null && request.PathParameters != null)
            {
                return await HandleGetTrashRequest(request);
            }
            else if (httpMethod == "PUT" && request.Body != null && request.PathParameters == null)
            {
                return await HandleUnTrashRequest(request);
            }
            // Handle unsupported or unrecognized HTTP methods
            return new APIGatewayHttpApiV2ProxyResponse { StatusCode = 400 };
        }

        private async Task<APIGatewayHttpApiV2ProxyResponse> HandleAddTrashRequest(APIGatewayHttpApiV2ProxyRequest request)
        {
            try
            {
                var file = JsonSerializer.Deserialize<StaredRequestDto>(request.Body);
                var existingFile = await _dynamoDbContext.LoadAsync<FileDetails>(file.Id);
                existingFile.Trash = true;
                await _dynamoDbContext.SaveAsync(existingFile);
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 200,
                    Body = $" {file.Id} File Trashed !!!"
                };
            }
            catch (Exception ex)
            {
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 402,
                    Body = $"File Not Trashed !!! {ex.Message}"
                };
            }
        }


        private async Task<APIGatewayHttpApiV2ProxyResponse> HandleGetTrashRequest(APIGatewayHttpApiV2ProxyRequest request)
        {
            try
            {
                // Get the Array of Help Desk Requests
                var trashdto = JsonSerializer.Deserialize<GetTrashDTO>(request.Body);
                request.PathParameters.TryGetValue("Id", out var Id);
                var Files = await _dynamoDbContext.ScanAsync<FileDetails>(default).GetRemainingAsync();
                var StaredFiles = Files.Where(v => v.Trash == true && v.UserId == trashdto.Id).ToList();

                return new APIGatewayHttpApiV2ProxyResponse()
                {
                    Body = JsonSerializer.Serialize(StaredFiles),
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 500,
                    Body = $"No Trashed Files !!! {ex.Message}"
                };
            }
        }

        private async Task<APIGatewayHttpApiV2ProxyResponse> HandleUnTrashRequest(APIGatewayHttpApiV2ProxyRequest request)
        {
            try
            {
                var file = JsonSerializer.Deserialize<StaredRequestDto>(request.Body);
                var existingFile = await _dynamoDbContext.LoadAsync<FileDetails>(file.Id);
                existingFile.Trash = false;
                await _dynamoDbContext.SaveAsync(existingFile);
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 200,
                    Body = $" {file.Id} File Un-Trashed !!!"
                };
            }
            catch (Exception ex)
            {
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 402,
                    Body = $"File Not Un-Trashed !!! {ex.Message}"
                };
            }
        }
    }
}
