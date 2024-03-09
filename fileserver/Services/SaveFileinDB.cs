using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3.Transfer;
using System.Text.Json;
using fileserver.DTO;
using fileserver.Model;

namespace fileserver.Services
{
    public class SaveFileinDB
    {
        
        private readonly DynamoDBContext _dynamoDbContext;
       
        public SaveFileinDB()
        {
            _dynamoDbContext = new DynamoDBContext(new AmazonDynamoDBClient());
        }

        public async Task<APIGatewayHttpApiV2ProxyResponse> SaveFileFunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
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
                return await HandleSaveRequest(request);
            }
            // Handle unsupported or unrecognized HTTP methods
            return new APIGatewayHttpApiV2ProxyResponse { StatusCode = 400 };
        }

        private async Task<APIGatewayHttpApiV2ProxyResponse> HandleSaveRequest(APIGatewayHttpApiV2ProxyRequest request)
        {
            try
            {
                var file_request = JsonSerializer.Deserialize<SaveFileDTO>(request.Body);
                FileDetails fileDetails = new FileDetails();
                fileDetails.FileName = GenerateId();
                fileDetails.NameOfTheFile = file_request.FileName;
                fileDetails.FileExtension = file_request.FileExtension;
                fileDetails.UserId = file_request.UserId;
                fileDetails.ObjectURL = file_request.ObjectURL;
                fileDetails.CreatedDate = GetCurrentEpochValue();
                fileDetails.LastModifiedDate = GetCurrentEpochValue();
                fileDetails.Stared = false;
                fileDetails.Trash = false;

                await _dynamoDbContext.SaveAsync(fileDetails);
                return OkResponse();
            }
            catch(Exception ex) 
            {
                return BadResponse(ex.Message);
            }
        }

        private int GetCurrentEpochValue()
        {
            // Calculate the epoch value for the current date
            return (int)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
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

        // Autogenerate ID
        public string GenerateId()
        {
            Guid guid = Guid.NewGuid();
            string id = guid.ToString();
            return id;
        }

    }
}
