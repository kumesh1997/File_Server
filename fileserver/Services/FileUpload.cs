using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using fileserver.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileserver.Services
{
    public class FileUpload
    {
        private readonly IAmazonS3 _s3Client;
        private readonly DynamoDBContext _dynamoDbContext;
        public FileUpload()
        {
            _s3Client = new AmazonS3Client(RegionEndpoint.USEast1);
            _dynamoDbContext = new DynamoDBContext(new AmazonDynamoDBClient());
        }

        public async Task<APIGatewayHttpApiV2ProxyResponse> UploadFunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
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
            if (httpMethod == "POST")
            {
                return await HandleUploadRequest(request);
            }
            // Handle unsupported or unrecognized HTTP methods
            return new APIGatewayHttpApiV2ProxyResponse { StatusCode = 400 };
        }

        // Upload File
        private async Task<APIGatewayHttpApiV2ProxyResponse> HandleUploadRequest(APIGatewayHttpApiV2ProxyRequest request)
        {
            try
            {
                // Check if the request contains a file
                if (request.IsBase64Encoded && !string.IsNullOrEmpty(request.Body))
                {
                    // Decode base64 content
                    //byte[] fileBytes = Convert.FromBase64String(request.Body);
                    // Convert the request body to a byte array
                    byte[] fileBytes = Encoding.UTF8.GetBytes(request.Body);

                    // Get other details such as file name, S3 bucket name, etc., from the request or headers
                    string fileName = request.QueryStringParameters?["fileName"];
                    string bucketName = "cloud-file-server-bucket";
                    string s3Key = "fileName"; // S3 key where the file will be stored

                    // Upload the file to S3
                    using (MemoryStream stream = new MemoryStream(fileBytes))
                    {
                        var fileTransferUtility = new TransferUtility(_s3Client);
                        await fileTransferUtility.UploadAsync(stream, bucketName, s3Key);
                    }

                    // Get the S3 object URL
                    var objectUrl = GenerateS3ObjectUrl(bucketName, s3Key);

                    //// Save in DB
                    //FileDetails fd = new FileDetails();
                    //fd.FileName = fileName;
                    //fd.FileExtension = "html";
                    //fd.CreatedDate = Date.Cu;
                    //fd.LastModifiedDate = "";
                    //fd.FileURL = "";
                    //fd.UserId = "";

                    // Construct a response
                    var response = new APIGatewayHttpApiV2ProxyResponse
                    {
                        StatusCode = 200,
                        Body = $"File {objectUrl} uploaded successfully",
                        Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
                    };

                    return response;
                }
                else
                {
                    return new APIGatewayHttpApiV2ProxyResponse
                    {
                        StatusCode = 400,
                        Body = "File issue"
                    };
                }
            }
            catch (Exception ex)
            {
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 400,
                    Body = ex.StackTrace
                };
            }
        }

     

        private string GenerateS3ObjectUrl(string bucketName, string key)
        {
            var config = new AmazonS3Config
            {
                ServiceURL = "https://s3.amazonaws.com" // Update with your S3 service URL
                                                        // You can also use the default service URL: "https://s3.amazonaws.com"
            };

            using (var s3Client = new AmazonS3Client(config))
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    Expires = DateTime.Now.AddHours(1) // Set the expiration time for the URL
                };

                var url = s3Client.GetPreSignedURL(request);
                return url;
            }
        }

        // Extract Key
        private string ExtractKeyFromPresignedUrl(string preSignedUrl)
        {
            //Sample pre-signed URL format: https://your-bucket-name.s3.amazonaws.com/your-object-key?AWSAccessKeyId=ACCESS_KEY_ID&Expires=EXPIRATION_TIMESTAMP&Signature=SIGNATURE
            Uri uri = new Uri(preSignedUrl);

            // Extract the path (object key) from the URL
            string path = uri.AbsolutePath;

            // Remove the leading slash from the path
            string key = path.TrimStart('/');

            return key;
        }
    }
}
