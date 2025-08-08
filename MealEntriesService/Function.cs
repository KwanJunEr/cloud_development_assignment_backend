using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.DTO;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.Extensions.NETCore.Setup;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MealEntriesService;

public class Function
{
    private readonly IServiceProvider _serviceProvider;
    private const string SNS_TOPIC_ARN = "arn:aws:sns:us-east-1:306656959951:MealEntry";


    public Function()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Use AWS SDK's built-in DI registration - this handles the virtual static method issue automatically
        services.AddAWSService<IAmazonSimpleNotificationService>();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation($"Request Path: {request.Path}");
            context.Logger.LogInformation($"Request Method: {request.HttpMethod}");
            context.Logger.LogInformation($"Path Parameters: {JsonSerializer.Serialize(request.PathParameters)}");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var snsClient = scope.ServiceProvider.GetRequiredService<IAmazonSimpleNotificationService>();

            // ? FIXED: Call the actual request processor
            return await ProcessRequest(request, dbContext, snsClient, context);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Function Handler Error: {ex.Message}");
            context.Logger.LogError($"Stack Trace: {ex.StackTrace}");
            return ErrorResponse(500, "Internal server error", ex.Message);
        }
    }
    private async Task<APIGatewayProxyResponse> ProcessRequest(APIGatewayProxyRequest request,
        AppDbContext dbContext,
        IAmazonSimpleNotificationService snsClient,
        ILambdaContext context)
    {
        var path = request.Path.ToLower();
        var method = request.HttpMethod.ToUpper();

        //Create Meal Entry
        if (path == "/mealentry" && method == "POST")
            return await CreateMealEntry(request, dbContext, snsClient, context);

        if (path.StartsWith("/mealentries/user/") && method == "GET")
            return await GetMealEntriesByUserId(request, dbContext, context);

        //For Getting Patient Info
        if(path.StartsWith("/mealentry/patient/") && method == "GET")
            return await GetMealEntriesByPatientId(path, dbContext);

        //For Deleting Meal Entries
        if (path.StartsWith("/mealentry/") && method == "DELETE" && IsDeleteByIdPattern(path))
            return await DeleteMealEntry(path, dbContext);

        //For updating 
        if (path.StartsWith("/mealentry/") && method == "PUT" && IsUpdateByIdPattern(path))
            return await UpdateMealEntry(request, path, dbContext, snsClient, context);

        if (method == "OPTIONS")
            return CorsResponse();

        return ErrorResponse(404, "Endpoint not found");
    }

    public async Task<APIGatewayProxyResponse> CreateMealEntry(APIGatewayProxyRequest request, AppDbContext dbContext, IAmazonSimpleNotificationService snsClient, ILambdaContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Body))
                return ErrorResponse(400, "Meal entry data is required");

            var dto = JsonSerializer.Deserialize<MealEntryDto>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null)
                return ErrorResponse(400, "Invalid meal entry data");

            // Validate required fields
            if (dto.UserId == 0)
                return ErrorResponse(400, "UserId is required");

            if (string.IsNullOrEmpty(dto.FoodItem))
                return ErrorResponse(400, "FoodItem is required");

            if (string.IsNullOrEmpty(dto.MealType))
                return ErrorResponse(400, "MealType is required");

            var userExists = await dbContext.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
                return ErrorResponse(404, $"User with Id {dto.UserId} does not exist.");

            var mealEntry = new MealEntry
            {
                UserId = dto.UserId,
                EntryDate = dto.EntryDate,
                MealType = dto.MealType,
                FoodItem = dto.FoodItem,
                Portion = dto.Portion,
                Notes = dto.Notes,
                ImageUrl = dto.ImageUrl
            };

            dbContext.MealEntries.Add(mealEntry);
            await dbContext.SaveChangesAsync();

            // Send SNS notification
            await SendMealEntryNotification(mealEntry, dbContext, snsClient, context);

            return Created(new
            {
                message = "Meal entry saved successfully.",
                mealEntry
            });
        }
        catch (Exception ex)
        {
            return ErrorResponse(500, "An error occurred", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> UpdateMealEntry(APIGatewayProxyRequest request, string path, AppDbContext dbContext, IAmazonSimpleNotificationService snsClient,ILambdaContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Body))
                return ErrorResponse(400, "Meal entry update data is required");

            var dto = JsonSerializer.Deserialize<MealEntryDto>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null)
                return ErrorResponse(400, "Invalid meal entry update data");

            var id = ExtractId(path, 2);

            var existingEntry = await dbContext.MealEntries.FindAsync(id);
            if (existingEntry == null)
                return ErrorResponse(404, "Meal entry not found");

            // Update fields
            existingEntry.MealType = dto.MealType ?? existingEntry.MealType;
            existingEntry.FoodItem = dto.FoodItem ?? existingEntry.FoodItem;
            existingEntry.EntryDate = dto.EntryDate != default ? dto.EntryDate : existingEntry.EntryDate;
            existingEntry.Portion = dto.Portion ?? existingEntry.Portion;
            existingEntry.Notes = dto.Notes ?? existingEntry.Notes;
            existingEntry.ImageUrl = dto.ImageUrl ?? existingEntry.ImageUrl;

            await dbContext.SaveChangesAsync();
            await SendMealEntryUpdateNotification(existingEntry, dbContext, snsClient, context);

            return Ok(new
            {
                message = "Meal entry updated successfully.",
                mealEntry = existingEntry
            });
        }
        catch (Exception ex)
        {
            return ErrorResponse(500, "An error occurred during update", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> GetMealEntriesByUserId(APIGatewayProxyRequest request, AppDbContext dbContext, ILambdaContext context)
    {
        try
        {
            // FIXED: Use pathParameters first, fallback to path parsing
            int userId = ExtractUserId(request, context);

            context.Logger.LogInformation($"Extracted User ID: {userId}");

            var userExists = await dbContext.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return ErrorResponse(404, $"User with Id {userId} does not exist.");

            var meals = await dbContext.MealEntries
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.EntryDate)
                .ToListAsync();

            context.Logger.LogInformation($"Found {meals.Count} meal entries for user {userId}");
            return Ok(meals);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"GetMealEntriesByUserId Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> GetMealEntriesByPatientId(string path, AppDbContext dbContext)
    {
        try
        {
            var patientId = ExtractId(path, 3);
            var fiveDaysAgo = DateTime.Today.AddDays(-5);

            var mealEntries = await dbContext.MealEntries
                .Where(m => m.UserId == patientId && m.EntryDate >= fiveDaysAgo)
                .OrderByDescending(m => m.EntryDate)
                .ToListAsync();

            return Ok(mealEntries);
        }
        catch (Exception ex)
        {
            return ErrorResponse(500, "Internal server error", ex.Message);
        }
    }


    private async Task<APIGatewayProxyResponse> DeleteMealEntry(string path, AppDbContext dbContext)
    {
        try
        {
            var id = ExtractId(path, 2);

            var mealEntry = await dbContext.MealEntries.FindAsync(id);
            if (mealEntry == null)
                return ErrorResponse(404, "Meal entry not found");

            dbContext.MealEntries.Remove(mealEntry);
            await dbContext.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return ErrorResponse(500, "An error occurred", ex.Message);
        }
    }

    private int ExtractId(string path, int index)
    {
        var parts = path.Split('/');
        return int.Parse(parts[index]);
    }

    // FIXED: New method that handles both pathParameters and path parsing
    private int ExtractUserId(APIGatewayProxyRequest request, ILambdaContext context)
    {
        // Method 1: Try to get from pathParameters (preferred)
        if (request.PathParameters != null && request.PathParameters.ContainsKey("userId"))
        {
            context.Logger.LogInformation($"Using pathParameters: {request.PathParameters["userId"]}");
            if (int.TryParse(request.PathParameters["userId"], out int userIdFromParam))
                return userIdFromParam;
        }

        // Method 2: Fallback to parsing the path manually
        context.Logger.LogInformation("PathParameters not available, parsing path manually");
        var parts = request.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && int.TryParse(parts[^1], out int userIdFromPath))
            return userIdFromPath;

        throw new ArgumentException($"Could not extract userId from path: {request.Path} or pathParameters: {JsonSerializer.Serialize(request.PathParameters)}");
    }


    private async Task SendMealEntryNotification(MealEntry mealEntry, AppDbContext dbContext, IAmazonSimpleNotificationService snsClient,
        ILambdaContext context)
    {
        try
        {
            // Get user information for better notification
            var user = await dbContext.Users.FindAsync(mealEntry.UserId);
            var userName = (user?.FirstName + user?.LastName) ?? $"User {mealEntry.UserId}";

            var message = $@"
New Meal Entry Created

User: {userName} (ID: {mealEntry.UserId})
Date: {mealEntry.EntryDate:yyyy-MM-dd HH:mm}
Meal Type: {mealEntry.MealType}
Food Item: {mealEntry.FoodItem}
Portion: {mealEntry.Portion ?? "Not specified"}
Notes: {mealEntry.Notes ?? "No notes"}
{(string.IsNullOrEmpty(mealEntry.ImageUrl) ? "" : $"Image: {mealEntry.ImageUrl}")}

This is an automated notification from the Diabetes Care System.
            ".Trim();

            var publishRequest = new PublishRequest
            {
                TopicArn = SNS_TOPIC_ARN,
                Message = message,
                Subject = $"New Meal Entry - {userName}"
            };


            await snsClient.PublishAsync(publishRequest);
            context.Logger.LogInformation($"SNS notification sent successfully for meal entry");
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the meal entry creation
            Console.WriteLine($"Error sending SNS notification: {ex.Message}");
        }
    }

    private async Task SendMealEntryUpdateNotification(
    MealEntry mealEntry,
    AppDbContext dbContext,
    IAmazonSimpleNotificationService snsClient,
    ILambdaContext context)
    {
        try
        {
            var user = await dbContext.Users.FindAsync(mealEntry.UserId);
            var userName = (user?.FirstName + user?.LastName) ?? $"User {mealEntry.UserId}";

            var message = $@"
Meal Entry Updated

User: {userName} (ID: {mealEntry.UserId})
Date: {mealEntry.EntryDate:yyyy-MM-dd HH:mm}
Meal Type: {mealEntry.MealType}
Food Item: {mealEntry.FoodItem}
Portion: {mealEntry.Portion ?? "Not specified"}
Notes: {mealEntry.Notes ?? "No notes"}
{(string.IsNullOrEmpty(mealEntry.ImageUrl) ? "" : $"Image: {mealEntry.ImageUrl}")}

This is an automated notification from the Diabetes Care System.
        ".Trim();

            var publishRequest = new PublishRequest
            {
                TopicArn = SNS_TOPIC_ARN,
                Message = message,
                Subject = $"Meal Entry Updated - {userName}"
            };

            await snsClient.PublishAsync(publishRequest);
            context.Logger.LogInformation("SNS update notification sent successfully.");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error sending SNS update notification: {ex.Message}");
        }
    }


    private bool IsDeleteByIdPattern(string path)
    {
        var parts = path.Split('/');
        return parts.Length == 3 && int.TryParse(parts[2], out _);
    }

    private bool IsUpdateByIdPattern(string path)
    {
        var parts = path.Split('/');
        return parts.Length == 3 && int.TryParse(parts[2], out _);
    }

    private APIGatewayProxyResponse Ok(object data) => new()
    {
        StatusCode = 200,
        Body = JsonSerializer.Serialize(data),
        Headers = CorsHeaders()
    };

    private APIGatewayProxyResponse Created(object data) => new()
    {
        StatusCode = 201,
        Body = JsonSerializer.Serialize(data),
        Headers = CorsHeaders()
    };

    private APIGatewayProxyResponse NoContent() => new()
    {
        StatusCode = 204,
        Body = "",
        Headers = CorsHeaders()
    };

    private APIGatewayProxyResponse ErrorResponse(int statusCode, string message, string? details = null) => new()
    {
        StatusCode = statusCode,
        Body = JsonSerializer.Serialize(new { error = message, details }),
        Headers = CorsHeaders()
    };

    private APIGatewayProxyResponse CorsResponse() => new()
    {
        StatusCode = 200,
        Body = "",
        Headers = CorsHeaders()
    };

    private Dictionary<string, string> CorsHeaders() => new()
    {
        { "Content-Type", "application/json" },
        { "Access-Control-Allow-Origin", "*" },
        { "Access-Control-Allow-Headers", "*" },
        { "Access-Control-Allow-Methods", "*" }
    };
}