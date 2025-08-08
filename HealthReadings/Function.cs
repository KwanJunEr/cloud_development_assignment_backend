using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.DTO;
using cloud_development_assignment_backend.Business;
using cloud_development_assignment_backend.Data;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace HealthReadings;

public class Function
{
    private readonly IServiceProvider _serviceProvider;

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

        services.AddScoped<HealhtLogBusinessLogic>();

        // Use AWS SDK's built-in DI registration - this handles the virtual static method issue automatically
        services.AddAWSService<IAmazonSimpleNotificationService>();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var businessLogic = scope.ServiceProvider.GetRequiredService<HealhtLogBusinessLogic>();
            var snsService = scope.ServiceProvider.GetRequiredService<IAmazonSimpleNotificationService>();

            return await ProcessRequest(request, dbContext, businessLogic, snsService, context);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error in FunctionHandler: {ex.Message}");
            return ErrorResponse(500, "Internal server error", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> ProcessRequest(
        APIGatewayProxyRequest request,
        AppDbContext dbContext,
        HealhtLogBusinessLogic businessLogic,
        IAmazonSimpleNotificationService snsService,
        ILambdaContext context)
    {
        var path = request.Path.ToLower();
        var method = request.HttpMethod.ToUpper();

        context.Logger.LogInformation($"Processing {method} request to {path}");

        // Routing
        if (path == "/healthreadings" && method == "POST")
            return await CreateReading(request, dbContext, businessLogic, snsService);

        if (path.StartsWith("/healthreadings/user/") && method == "GET" && IsUserReadingsPattern(path))
            return await GetUserReadings(path, dbContext);

        if (path.StartsWith("/healthreadings/user/") && path.EndsWith("/summary") && method == "GET")
            return await GetUserReadingsSummary(path, dbContext);

        if (path.StartsWith("/healthreadings/family-patient/") && method == "GET")
            return await GetUserReadingsByFamily(path, dbContext);

        if (method == "OPTIONS")
            return CorsResponse();

        return ErrorResponse(404, "Endpoint not found");
    }

    // --- Endpoint Implementations ---

    private async Task<APIGatewayProxyResponse> CreateReading(
        APIGatewayProxyRequest request,
        AppDbContext dbContext,
        HealhtLogBusinessLogic businessLogic,
        IAmazonSimpleNotificationService snsService)
    {
        if (string.IsNullOrEmpty(request.Body))
            return ErrorResponse(400, "Health reading data is required");

        try
        {
            var dto = JsonSerializer.Deserialize<HealthReadingDto>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null)
                return ErrorResponse(400, "Invalid health reading data");

            // Validate required fields
            if (dto.UserId <= 0)
                return ErrorResponse(400, "Valid UserId is required");

            if (string.IsNullOrEmpty(dto.Date))
                return ErrorResponse(400, "Date is required");

            if (string.IsNullOrEmpty(dto.Time))
                return ErrorResponse(400, "Time is required");

            var reading = new HealthReading
            {
                Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                UserId = dto.UserId,
                Date = DateTime.Parse(dto.Date),
                Time = TimeSpan.Parse(dto.Time),
                Timestamp = DateTime.Parse($"{dto.Date}T{dto.Time}"),
                BloodSugar = dto.BloodSugar,
                InsulinDosage = dto.InsulinDosage,
                BodyWeight = dto.BodyWeight,
                SystolicBP = dto.SystolicBP,
                DiastolicBP = dto.DiastolicBP,
                HeartRate = dto.HeartRate,
                MealContext = dto.MealContext,
                Notes = dto.Notes,
                ImageUrl = dto.ImageUrl
            };

            string status = businessLogic.EvaluateStatus(reading);
            reading.Status = status;

            dbContext.HealthReadings.Add(reading);
            await dbContext.SaveChangesAsync();

            // Send SNS notification
            await SendNotification(snsService, reading, status);

            return Ok(new
            {
                message = "Reading saved successfully.",
                status,
                readingId = reading.Id
            });
        }
        catch (FormatException ex)
        {
            return ErrorResponse(400, "Invalid date or time format", ex.Message);
        }
        catch (JsonException ex)
        {
            return ErrorResponse(400, "Invalid JSON format", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> GetUserReadings(string path, AppDbContext dbContext)
    {
        try
        {
            var userId = ExtractUserId(path);
            var readings = await dbContext.HealthReadings
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();

            var dtoList = readings.Select(r => new HealthReadingOutputDto
            {
                Id = r.Id,
                UserId = r.UserId,
                Date = r.Date.ToString("yyyy-MM-dd"),
                Time = r.Time.ToString(@"hh\:mm"),
                Timestamp = r.Timestamp,
                BloodSugar = r.BloodSugar,
                InsulinDosage = r.InsulinDosage,
                BodyWeight = r.BodyWeight,
                SystolicBP = r.SystolicBP,
                DiastolicBP = r.DiastolicBP,
                HeartRate = r.HeartRate,
                MealContext = r.MealContext,
                Notes = r.Notes,
                Status = r.Status,
                ImageUrl = r.ImageUrl
            }).ToList();

            return Ok(dtoList);
        }
        catch (FormatException)
        {
            return ErrorResponse(400, "Invalid user ID format");
        }
    }

    private async Task<APIGatewayProxyResponse> GetUserReadingsSummary(string path, AppDbContext dbContext)
    {
        try
        {
            var userId = ExtractUserIdFromSummaryPath(path);
            var readings = await dbContext.HealthReadings
                .Where(r => r.UserId == userId)
                .ToListAsync();

            if (!readings.Any())
            {
                return Ok(new { message = "No readings found for user.", summary = (object?)null });
            }

            var summary = new
            {
                BloodSugar = new
                {
                    Average = readings.Average(r => r.BloodSugar),
                    Min = readings.Min(r => r.BloodSugar),
                    Max = readings.Max(r => r.BloodSugar)
                },
                InsulinDosage = new
                {
                    Average = readings.Average(r => r.InsulinDosage),
                    Min = readings.Min(r => r.InsulinDosage),
                    Max = readings.Max(r => r.InsulinDosage)
                },
                BodyWeight = new
                {
                    Average = readings.Where(r => r.BodyWeight.HasValue).Any() ? readings.Where(r => r.BodyWeight.HasValue).Average(r => r.BodyWeight.Value) : (double?)null,
                    Min = readings.Where(r => r.BodyWeight.HasValue).Any() ? readings.Where(r => r.BodyWeight.HasValue).Min(r => r.BodyWeight.Value) : (double?)null,
                    Max = readings.Where(r => r.BodyWeight.HasValue).Any() ? readings.Where(r => r.BodyWeight.HasValue).Max(r => r.BodyWeight.Value) : (double?)null
                },
                SystolicBP = new
                {
                    Average = readings.Where(r => r.SystolicBP.HasValue).Any() ? readings.Where(r => r.SystolicBP.HasValue).Average(r => r.SystolicBP.Value) : (double?)null,
                    Min = readings.Where(r => r.SystolicBP.HasValue).Any() ? readings.Where(r => r.SystolicBP.HasValue).Min(r => r.SystolicBP.Value) : (int?)null,
                    Max = readings.Where(r => r.SystolicBP.HasValue).Any() ? readings.Where(r => r.SystolicBP.HasValue).Max(r => r.SystolicBP.Value) : (int?)null
                },
                DiastolicBP = new
                {
                    Average = readings.Where(r => r.DiastolicBP.HasValue).Any() ? readings.Where(r => r.DiastolicBP.HasValue).Average(r => r.DiastolicBP.Value) : (double?)null,
                    Min = readings.Where(r => r.DiastolicBP.HasValue).Any() ? readings.Where(r => r.DiastolicBP.HasValue).Min(r => r.DiastolicBP.Value) : (int?)null,
                    Max = readings.Where(r => r.DiastolicBP.HasValue).Any() ? readings.Where(r => r.DiastolicBP.HasValue).Max(r => r.DiastolicBP.Value) : (int?)null
                },
                HeartRate = new
                {
                    Average = readings.Where(r => r.HeartRate.HasValue).Any() ? readings.Where(r => r.HeartRate.HasValue).Average(r => r.HeartRate.Value) : (double?)null,
                    Min = readings.Where(r => r.HeartRate.HasValue).Any() ? readings.Where(r => r.HeartRate.HasValue).Min(r => r.HeartRate.Value) : (int?)null,
                    Max = readings.Where(r => r.HeartRate.HasValue).Any() ? readings.Where(r => r.HeartRate.HasValue).Max(r => r.HeartRate.Value) : (int?)null
                }
            };

            return Ok(new { summary });
        }
        catch (FormatException)
        {
            return ErrorResponse(400, "Invalid user ID format");
        }
    }

    private async Task<APIGatewayProxyResponse> GetUserReadingsByFamily(string path, AppDbContext dbContext)
    {
        try
        {
            var familyId = ExtractFamilyId(path);
            var familyUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == familyId);

            if (familyUser == null)
            {
                return ErrorResponse(404, $"Family user with ID {familyId} not found.");
            }

            // Find the associated PatientID
            var patientId = familyUser.PatientId;

            if (patientId == null)
            {
                return ErrorResponse(404, "This family user does not have an associated patient.");
            }

            // Get readings for the patient
            var readings = await dbContext.HealthReadings
                .Where(r => r.UserId == patientId)
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();

            var dtoList = readings.Select(r => new HealthReadingOutputDto
            {
                Id = r.Id,
                UserId = r.UserId,
                Date = r.Date.ToString("yyyy-MM-dd"),
                Time = r.Time.ToString(@"hh\:mm"),
                Timestamp = r.Timestamp,
                BloodSugar = r.BloodSugar,
                InsulinDosage = r.InsulinDosage,
                BodyWeight = r.BodyWeight,
                SystolicBP = r.SystolicBP,
                DiastolicBP = r.DiastolicBP,
                HeartRate = r.HeartRate,
                MealContext = r.MealContext,
                Notes = r.Notes,
                Status = r.Status,
                ImageUrl = r.ImageUrl
            }).ToList();

            return Ok(dtoList);
        }
        catch (FormatException)
        {
            return ErrorResponse(400, "Invalid family ID format");
        }
    }

    // --- SNS Email Notification ---

    private const string SNS_TOPIC_ARN = "arn:aws:sns:us-east-1:306656959951:HealthReadingStatus";

    private async Task SendNotification(IAmazonSimpleNotificationService snsService, HealthReading reading, string status)
    {
        try
        {
            var topicArn = SNS_TOPIC_ARN;

            // Create email message with formatted content
            var message = $@"
New Health Reading Created
User ID: {reading.UserId}
Reading ID: {reading.Id}
Date: {reading.Timestamp:yyyy-MM-dd HH:mm}
Status: {status}

Health Metrics:
- Blood Sugar: {reading.BloodSugar} mg/dL
- Insulin Dosage: {reading.InsulinDosage} units
- Body Weight: {reading.BodyWeight?.ToString() ?? "Not recorded"} kg
- Blood Pressure: {reading.SystolicBP?.ToString() ?? "N/A"}/{reading.DiastolicBP?.ToString() ?? "N/A"} mmHg
- Heart Rate: {reading.HeartRate?.ToString() ?? "Not recorded"} bpm
- Meal Context: {reading.MealContext ?? "Not specified"}
- Notes: {reading.Notes ?? "No notes"}
{(string.IsNullOrEmpty(reading.ImageUrl) ? "" : $"Image: {reading.ImageUrl}")}

This is an automated notification from the Health Monitoring System.
            ".Trim();

            var publishRequest = new PublishRequest
            {
                TopicArn = topicArn,
                Message = message,
                Subject = "New Health Reading Alert"
            };

            await snsService.PublishAsync(publishRequest);

            Console.WriteLine($"SNS notification sent successfully for health reading");
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the main operation
            Console.WriteLine($"Error sending SNS notification: {ex.Message}");
        }
    }

    // --- Helper Methods ---

    private int ExtractUserId(string path)
    {
        // Extract from paths like "/healthreadings/user/123"
        var parts = path.Split('/');
        if (parts.Length < 4 || !int.TryParse(parts[3], out int userId))
            throw new FormatException("Invalid user ID in path");
        return userId;
    }

    private int ExtractUserIdFromSummaryPath(string path)
    {
        // Extract from paths like "/healthreadings/user/123/summary"
        var parts = path.Split('/');
        if (parts.Length < 4 || !int.TryParse(parts[3], out int userId))
            throw new FormatException("Invalid user ID in summary path");
        return userId;
    }

    private int ExtractFamilyId(string path)
    {
        // Extract from paths like "/healthreadings/family-patient/123"
        var parts = path.Split('/');
        if (parts.Length < 4 || !int.TryParse(parts[3], out int familyId))
            throw new FormatException("Invalid family ID in path");
        return familyId;
    }

    private bool IsUserReadingsPattern(string path)
    {
        // Check if path matches "/healthreadings/user/{id}" pattern
        var parts = path.Split('/');
        return parts.Length == 4 && parts[1] == "healthreadings" && parts[2] == "user" && int.TryParse(parts[3], out _);
    }

    private APIGatewayProxyResponse Ok(object data) => new()
    {
        StatusCode = 200,
        Body = JsonSerializer.Serialize(data),
        Headers = CorsHeaders(),
        IsBase64Encoded = false
    };

    private APIGatewayProxyResponse Created(object data) => new()
    {
        StatusCode = 201,
        Body = JsonSerializer.Serialize(data),
        Headers = CorsHeaders(),
        IsBase64Encoded = false
    };

    private APIGatewayProxyResponse NoContent() => new()
    {
        StatusCode = 204,
        Body = "",
        Headers = CorsHeaders(),
        IsBase64Encoded = false
    };

    private APIGatewayProxyResponse ErrorResponse(int statusCode, string message, string? details = null) => new()
    {
        StatusCode = statusCode,
        Body = JsonSerializer.Serialize(new { error = message, details }),
        Headers = CorsHeaders(),
        IsBase64Encoded = false
    };

    private APIGatewayProxyResponse CorsResponse() => new()
    {
        StatusCode = 200,
        Body = "",
        Headers = CorsHeaders(),
        IsBase64Encoded = false
    };

    private Dictionary<string, string> CorsHeaders() => new()
    {
        { "Content-Type", "application/json" },
        { "Access-Control-Allow-Origin", "*" },
        { "Access-Control-Allow-Headers", "*" },
        { "Access-Control-Allow-Methods", "*" }
    };
}




