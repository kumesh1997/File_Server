﻿using Amazon.CognitoIdentityProvider;
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
using System.Text.Json;

namespace fileserver.Services
{
    public class LoginService
    {
        private readonly DynamoDBContext _dynamoDbContext;
        private readonly AmazonDynamoDBClient _amazonDynamoDBClient;

        private readonly IAmazonCognitoIdentityProvider _cognitoClient;
        private readonly string _userPoolId = "us-east-1_z1hTd7Wi7";
        private readonly string _clientId = "tj39nh2d1v563du99b6pllk23";
        private readonly string _clientSecret = "1q807qber9cpm1blu782jndeo6fg7mck6ncrpdbblq9c31bib6jk";
        private readonly RegionEndpoint _region = RegionEndpoint.USEast1;


        public LoginService()
        {
            _amazonDynamoDBClient = new AmazonDynamoDBClient();
            _cognitoClient = new AmazonCognitoIdentityProviderClient(_region);
        }



        public async Task<APIGatewayHttpApiV2ProxyResponse> SignInFunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
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
                return await SignInUser(request);
            }
            // Handle unsupported or unrecognized HTTP methods
            return new APIGatewayHttpApiV2ProxyResponse { StatusCode = 400 };
        }

        // Sign In User
        private async Task<APIGatewayHttpApiV2ProxyResponse> SignInUser(
         APIGatewayHttpApiV2ProxyRequest request)
        {
            try
            {
                var userDto = JsonSerializer.Deserialize<LoginDTO>(request.Body);

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
                    { "USERNAME", userDto.Email },
                    { "PASSWORD", userDto.Password }
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
    }
}