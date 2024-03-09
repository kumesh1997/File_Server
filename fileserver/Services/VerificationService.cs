using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.CognitoIdentityProvider.Model;
using fileserver.DTO;
using Amazon.DynamoDBv2.Model;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;

namespace fileserver.Services
{
    public class VerificationService
    {
        private readonly DynamoDBContext _dynamoDbContext;
        private readonly AmazonDynamoDBClient _amazonDynamoDBClient;
        private readonly IAmazonCognitoIdentityProvider _cognitoClient;
        private readonly string _userPoolId = "us-east-1_z1hTd7Wi7";
        private readonly string _clientId = "tj39nh2d1v563du99b6pllk23";
        private readonly string _clientSecret = "1q807qber9cpm1blu782jndeo6fg7mck6ncrpdbblq9c31bib6jk";
        private readonly RegionEndpoint _region;

        public VerificationService()
        {
            _amazonDynamoDBClient = new AmazonDynamoDBClient();
            var region = RegionEndpoint.USEast1; // e.g., RegionEndpoint.USWest2
            _cognitoClient = new AmazonCognitoIdentityProviderClient(region);
        }

        public async Task<APIGatewayHttpApiV2ProxyResponse> VerificationFunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
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
                 return await VerifyUser(request);
            }

            // Handle unsupported or unrecognized HTTP methods
            return new APIGatewayHttpApiV2ProxyResponse { StatusCode = 400 };
        }

        // Verify the Email Address
        private async Task<APIGatewayHttpApiV2ProxyResponse> VerifyUser(
         APIGatewayHttpApiV2ProxyRequest request)
        {
            try
            {
                var userDto = System.Text.Json.JsonSerializer.Deserialize<ConfirmEmailDTO>(request.Body);

                var provider = new AmazonCognitoIdentityProviderClient(new AmazonCognitoIdentityProviderConfig
                {
                    RegionEndpoint = _region
                });

                var confirmRequest = new ConfirmSignUpRequest
                {
                    ClientId = _clientId,
                    Username = userDto.Email,
                    ConfirmationCode = userDto.Code
                };
                var confirmResponse = await provider.ConfirmSignUpAsync(confirmRequest);
                // Check if confirmation was successful
                if (confirmResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    // User registration confirmed successfully. Now, obtain tokens.
                    var authenticationRequest = new InitiateAuthRequest
                    {
                        AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                        ClientId = _clientId,
                        AuthParameters = new Dictionary<string, string>
                    {
                        { "USERNAME", userDto.Email },
                        { "PASSWORD", userDto.Password },
                        { "ROLE", "user" }// Replace with the user's password,
                    }
                    };
                    var authenticationResponse = await provider.InitiateAuthAsync(authenticationRequest);


                    // Check if authentication was successful
                    if (authenticationResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var updateRequest = new UpdateItemRequest
                        {
                            TableName = "User",
                            Key = new Dictionary<string, AttributeValue>
                    {
                        { "UserId", new AttributeValue { S = userDto.Email } }
                    },
                            UpdateExpression = "SET Verified = :verified",
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":verified", new AttributeValue { S = "1" } }
                    }
                        };
                        // Perform the update operation
                        await _amazonDynamoDBClient.UpdateItemAsync(updateRequest);

                        var accessToken = authenticationResponse.AuthenticationResult.AccessToken;
                        var refreshToken = authenticationResponse.AuthenticationResult.RefreshToken;
                        // Parse the JWT token to extract claims
                        var handler = new JwtSecurityTokenHandler();
                        var token = handler.ReadJwtToken(accessToken);

                        // Retrieve email claim
                        //var emailClaim = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                        //if (string.IsNullOrEmpty(emailClaim))
                        //{
                        //    throw new Exception("Email claim not found in the JWT token.");
                        //}

                        // Retrieve the user's role (in this case, hardcoding it as "user")
                        var userRole = "user";

                        var key = new byte[256 / 8]; // 128 bits / 8 bits per byte
                        RandomNumberGenerator.Create().GetBytes(key);
       

                        // Create JWT token
                        var tokenDescriptor = new SecurityTokenDescriptor
                        {
                            Subject = new ClaimsIdentity(new[]
                            {
                                new Claim("email", userDto.Email), // Add email as a claim
                                new Claim("role", userRole) // Add role as a claim
                            }),
                            Expires = DateTime.UtcNow.AddHours(1), // Set token expiration
                            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                        };
                        var jwtToken = handler.CreateToken(tokenDescriptor);
                        var encodedJwtToken = handler.WriteToken(jwtToken);

                        return new APIGatewayHttpApiV2ProxyResponse
                        {
                            StatusCode = 200,
                            Body = System.Text.Json.JsonSerializer.Serialize($"User registration confirmed successfully. Access_Token: {encodedJwtToken}, Refresh_Token: {refreshToken}"),
                        };
                    }
                    else
                    {
                        return new APIGatewayHttpApiV2ProxyResponse
                        {
                            StatusCode = 400,
                            Body = "User authentication failed.",
                        };
                    }
                }
                else
                {
                    return new APIGatewayHttpApiV2ProxyResponse
                    {
                        StatusCode = 400,
                        Body = "User registration confirmation failed.",
                    };
                }
            }
            catch (Exception ex)
            {
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    Body = ex.Message,
                    StatusCode = 500
                };
            }
        }
    }
}
