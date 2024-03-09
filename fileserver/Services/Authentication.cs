using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using fileserver.DTO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace fileserver.Services
{
    public class Authentication
    {
        private readonly DynamoDBContext _dynamoDbContext;
        private readonly AmazonDynamoDBClient _amazonDynamoDBClient;
        private readonly IAmazonCognitoIdentityProvider _cognitoClient;
        private readonly string _userPoolId = "us-east-1_z1hTd7Wi7";
        private readonly string _clientId = "tj39nh2d1v563du99b6pllk23";
        private readonly string _clientSecret = "1q807qber9cpm1blu782jndeo6fg7mck6ncrpdbblq9c31bib6jk";
        private readonly RegionEndpoint _region;

        public Authentication()
        {
            _amazonDynamoDBClient = new AmazonDynamoDBClient();
            var region = RegionEndpoint.USEast1; // e.g., RegionEndpoint.USWest2
            _cognitoClient = new AmazonCognitoIdentityProviderClient(region);
        }

        public async Task<APIGatewayHttpApiV2ProxyResponse> AuthenticationFunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
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
                return await HandleUserRegistrationRequest(request);
            }
            else if (httpMethod == "GET" && request.Body == null && request.PathParameters != null)
            {
               // return await VerifyUser(request);
            }

            // Handle unsupported or unrecognized HTTP methods
            return new APIGatewayHttpApiV2ProxyResponse { StatusCode = 400 };
        }

        // Register User
        private async Task<APIGatewayHttpApiV2ProxyResponse> HandleUserRegistrationRequest(
          APIGatewayHttpApiV2ProxyRequest request)
        {
            var userDto = System.Text.Json.JsonSerializer.Deserialize<UserAuthenticationDTO>(request.Body);
            var document = new Document();
            try
            {
                var provider = new AmazonCognitoIdentityProviderClient(
                new BasicAWSCredentials(_clientId, _clientSecret), _region);
                
                // If there us a client secret, it should also be added here brfor provider
                var pool = new CognitoUserPool(_userPoolId, _clientId, provider);
               var userAttributes = new Dictionary<string, string>
                {
                    { "email", userDto.UserId },
                    // Add other user attributes as needed
                };

                try
                {
                    await pool.SignUpAsync(userDto.UserId, userDto.Password, userAttributes, null);
                    Console.WriteLine("User registered successfully.");
                }
                catch (Exception ex)
                {
                    return new APIGatewayHttpApiV2ProxyResponse()
                    {
                        Body = ex.Message,
                        StatusCode = 400
                    };
                }
                    // Save User in DynamoDB
                    document["UserId"] = userDto.UserId;
                    document["Password"] = userDto.Password;
                    document["Verified"] = "0";

                var table = Table.LoadTable(_amazonDynamoDBClient, "User");
                var res = table.PutItemAsync(document);
                if (res != null)
                {
                    return OkResponse();
                }
                return BadResponse("User was not registered !!!");
            }
            catch (Exception ex)
            {
                return BadResponse("User was not registered !!! " + ex.Message);
            }
        }

        

        // Sign In User
        private async Task<APIGatewayHttpApiV2ProxyResponse> SignInUser(
         APIGatewayHttpApiV2ProxyRequest request)
        {
            try
            {
                var userDto = System.Text.Json.JsonSerializer.Deserialize<SignInDTO>(request.Body);

                var provider = new AmazonCognitoIdentityProviderClient(new AmazonCognitoIdentityProviderConfig
                {
                    RegionEndpoint = _region
                });

                var authenticationRequest = new InitiateAuthRequest
                {
                    AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                    ClientId = _clientId,
                    AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", userDto.UserName },
                    { "PASSWORD", userDto.Password },
                    { "ROLE", "user" }
                }
                };

                var authenticationResponse = await provider.InitiateAuthAsync(authenticationRequest);

                if (authenticationResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    var accessToken = authenticationResponse.AuthenticationResult.AccessToken;
                    var refreshToken = authenticationResponse.AuthenticationResult.RefreshToken;

                    return new APIGatewayHttpApiV2ProxyResponse
                    {
                        StatusCode = 200,
                        Body = $"Sign-in successful. Access Token: {accessToken}, Refresh Token: {refreshToken}",
                    };
                }
                else
                {
                    return new APIGatewayHttpApiV2ProxyResponse
                    {
                        StatusCode = 400,
                        Body = "Sign-in failed.",
                    };
                }
            }
            catch (Exception ex)
            {
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 500,
                    Body = "Error signing in: " + ex.Message,
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
