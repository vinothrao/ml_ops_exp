using HousePredictionAPI;
using HousePredictionAPI.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Net.Http;
using System.Text;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);


// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5173") // Your React app URL
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

// Configure rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));

builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();


builder.Services.AddDbContext<HousePredictionDBContext>(opt=>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("HouseDetailsDB")));

// Add HttpClient for Databricks API calls
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure rate limiting middleware
app.UseIpRateLimiting();

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();

async Task<string?> GetDatabricksToken(IConfiguration config, IHttpClientFactory httpClientFactory)
{
    var workspaceUrl = config["Databricks:WorkspaceUrl"];
    var clientId = config["Databricks:ClientId"];
    var clientSecret = config["Databricks:ClientSecret"];

    if (string.IsNullOrEmpty(workspaceUrl) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    {
        return null;
    }

    var tokenClient = httpClientFactory.CreateClient();
    var tokenUrl = $"{workspaceUrl}/oidc/v1/token";
    
    var tokenRequest = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("grant_type", "client_credentials"),
        new KeyValuePair<string, string>("client_id", clientId),
        new KeyValuePair<string, string>("client_secret", clientSecret),
        new KeyValuePair<string, string>("scope", "all-apis")
    });

    var tokenResponse = await tokenClient.PostAsync(tokenUrl, tokenRequest);
    if (!tokenResponse.IsSuccessStatusCode)
    {
        return null;
    }
    
    var tokenResult = await tokenResponse.Content.ReadAsStringAsync();
    var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenResult);
    return tokenData.TryGetProperty("access_token", out var tokenElement) ? tokenElement.GetString() : null;
}

async Task<bool> IsModelTrainingInProgress(IConfiguration config, IHttpClientFactory httpClientFactory, string accessToken)
{
    try
    {
        var workspaceUrl = config["Databricks:WorkspaceUrl"];
        var jobId = config["Databricks:TrainingJobId"];

        if (string.IsNullOrEmpty(workspaceUrl) || string.IsNullOrEmpty(jobId))
        {
            return false;
        }

        var client = httpClientFactory.CreateClient();
        var url = $"{workspaceUrl}/api/2.1/jobs/runs/list";
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }
        
        var result = await response.Content.ReadAsStringAsync();
        var runsData = JsonSerializer.Deserialize<JsonElement>(result);
        
        if (runsData.TryGetProperty("runs", out JsonElement runs))
        {
            foreach (var run in runs.EnumerateArray())
            {
                if (run.TryGetProperty("job_id", out var runJobId) && 
                    runJobId.GetInt64().ToString() == jobId &&
                    run.TryGetProperty("state", out var state) &&
                    state.TryGetProperty("life_cycle_state", out var lifecycleState))
                {
                    var stateString = lifecycleState.GetString();
                    if (stateString == "RUNNING" || stateString == "PENDING")
                    {
                        return true;
                    }
                    break;
                }
            }
        }
        
        return false;
    }
    catch (Exception exception)
    {
        
        return false; // If we can't check the status, assume it's not running
    }
}

app.MapPost("/api/predict", async ([FromBody] HouseDetails details, IConfiguration config, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        // Get Databricks token
        var accessToken = await GetDatabricksToken(config, httpClientFactory);
        if (accessToken == null)
        {
            return Results.Problem(
                title: "Configuration Error",
                detail: "Unable to authenticate with Databricks",
                statusCode: 500
            );
        }
        
        // Check if model training is in progress
        if (await IsModelTrainingInProgress(config, httpClientFactory, accessToken))
        {
            return Results.BadRequest(new { error = "Model retrain is in progress. Try after sometime" });
        }

        // Proceed with prediction
        var client = httpClientFactory.CreateClient();
        var url = "http://localhost:7071/api/PricePrediction";

        var content = new StringContent(
            JsonSerializer.Serialize(details),
            Encoding.UTF8,
            "application/json"
        );

        var response = await client.PostAsync(url, content);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            return Results.Ok(JsonSerializer.Deserialize<JsonElement>(result));
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            return Results.Problem(
                detail: error,
                title: "Failed to get price prediction",
                statusCode: (int)response.StatusCode
            );
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            title: "Internal server error",
            statusCode: 500
        );
    }
})
.WithName("predict");

app.MapPost("/api/details", async ([FromBody] HouseDetails details,HousePredictionDBContext context) =>
{
    context.HouseDetails.Add(details);
    await context.SaveChangesAsync();
    return Results.Created();
});

app.MapGet("/api/model/train", async (IConfiguration config, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var workspaceUrl = config["Databricks:WorkspaceUrl"];
        var jobId = config["Databricks:TrainingJobId"];

        if (string.IsNullOrEmpty(workspaceUrl) || string.IsNullOrEmpty(jobId))
        {
            return Results.BadRequest("Databricks configuration is missing");
        }

        // Get the access token using the shared method
        var accessToken = await GetDatabricksToken(config, httpClientFactory);
        if (accessToken == null)
        {
            return Results.Problem(
                title: "Authentication Failed",
                detail: "Failed to get Databricks access token",
                statusCode: 500
            );
        }
                // Check if model training is in progress
        if (await IsModelTrainingInProgress(config, httpClientFactory, accessToken))
        {
            return Results.BadRequest(new { error = "Model retrain is in progress. Try after sometime" });
        }


        // Trigger the job with the access token
        var client = httpClientFactory.CreateClient();
        var url = $"{workspaceUrl}/api/2.1/jobs/run-now";
        
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        var payload = new { job_id = jobId };
        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        var response = await client.PostAsync(url, content);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            return Results.Ok(new { message = "Model training job triggered successfully", details = result });
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            return Results.Problem(
                detail: error,
                title: "Failed to trigger training job",
                statusCode: (int)response.StatusCode
            );
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            title: "Internal server error",
            statusCode: 500
        );
    }
}).WithName("Model Retrain");

app.Run();

