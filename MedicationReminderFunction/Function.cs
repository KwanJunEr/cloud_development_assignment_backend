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

namespace MedicationReminderFunction;

public class Function
{
    private readonly IServiceProvider _serviceProvider;
    private const string SNS_TOPIC_ARN = "arn:aws:sns:us-east-1:306656959951:MedicalReminder";

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

        // Use AWS SDK's built-in DI registration
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

        // Create Medication Reminder
        if (path == "/medicationreminder" && method == "POST")
            return await CreateMedicationReminder(request, dbContext, snsClient, context);

        // Get upcoming reminders by user
        if (path.StartsWith("/medicationreminders/user/") && path.EndsWith("/upcoming") && method == "GET")
            return await GetUpcomingRemindersByUser(request, dbContext, context);

        // Update medication reminder
        if (path.StartsWith("/medicationreminder/") && method == "PUT" && IsUpdateByIdPattern(path))
            return await UpdateMedicationReminder(request, path, dbContext, snsClient, context);

        // Delete medication reminder
        if (path.StartsWith("/medicationreminder/") && method == "DELETE" && IsDeleteByIdPattern(path))
            return await DeleteMedicationReminder(path, dbContext);

        if (method == "OPTIONS")
            return CorsResponse();

        return ErrorResponse(404, "Endpoint not found");
    }

    private async Task<APIGatewayProxyResponse> CreateMedicationReminder(APIGatewayProxyRequest request,
        AppDbContext dbContext,
        IAmazonSimpleNotificationService snsClient,
        ILambdaContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Body))
                return ErrorResponse(400, "Medication reminder data is required");

            var dto = JsonSerializer.Deserialize<MedicationReminderDto>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null)
                return ErrorResponse(400, "Invalid medication reminder data");

            // Validate required fields
            if (dto.UserId == 0)
                return ErrorResponse(400, "UserId is required");

            if (string.IsNullOrEmpty(dto.MedicationName))
                return ErrorResponse(400, "MedicationName is required");

            // Check if the user exists
            var userExists = await dbContext.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
                return ErrorResponse(400, $"User with ID {dto.UserId} does not exist.");

            var reminder = new MedicationReminder
            {
                UserId = dto.UserId,
                MedicationName = dto.MedicationName,
                Description = dto.Description,
                Dosage = dto.Dosage,
                ReminderDate = dto.ReminderDate,
                ReminderDue = dto.ReminderDue,
                Notes = dto.Notes,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.MedicationReminders.Add(reminder);
            await dbContext.SaveChangesAsync();

            // Send SNS notification
            await SendMedicationReminderNotification(reminder, dbContext, snsClient, context, "created");

            return Created(new
            {
                message = "Medication Reminder created successfully",
                reminder
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"CreateMedicationReminder Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while creating the medication reminder", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> GetUpcomingRemindersByUser(APIGatewayProxyRequest request,
        AppDbContext dbContext,
        ILambdaContext context)
    {
        try
        {
            int userId = ExtractUserIdFromPath(request.Path);
            context.Logger.LogInformation($"Extracted User ID: {userId}");

            // Validate user exists
            var userExists = await dbContext.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return ErrorResponse(404, $"User with ID {userId} does not exist.");

            // Get today's date
            var today = DateTime.UtcNow.Date;

            // Query reminders
            var reminders = await dbContext.MedicationReminders
                .Where(r => r.UserId == userId && r.ReminderDate >= today)
                .OrderBy(r => r.ReminderDate)
                .Select(r => new MedicationReminderDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    MedicationName = r.MedicationName,
                    Description = r.Description,
                    Dosage = r.Dosage,
                    ReminderDate = r.ReminderDate,
                    ReminderDue = r.ReminderDue,
                    Notes = r.Notes,
                    Status = r.Status,
                })
                .ToListAsync();

            context.Logger.LogInformation($"Found {reminders.Count} upcoming reminders for user {userId}");
            return Ok(reminders);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"GetUpcomingRemindersByUser Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while retrieving reminders", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> UpdateMedicationReminder(APIGatewayProxyRequest request,
        string path,
        AppDbContext dbContext,
        IAmazonSimpleNotificationService snsClient,
        ILambdaContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Body))
                return ErrorResponse(400, "Medication reminder update data is required");

            var dto = JsonSerializer.Deserialize<MedicationReminderDto>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null)
                return ErrorResponse(400, "Invalid medication reminder update data");

            var id = ExtractId(path, 2);

            var reminder = await dbContext.MedicationReminders.FindAsync(id);
            if (reminder == null)
                return ErrorResponse(404, $"Medication Reminder with ID {id} not found.");

            // Validate Status if provided
            if (!string.IsNullOrEmpty(dto.Status))
            {
                var validStatuses = new[] { "pending", "taken" };
                if (!validStatuses.Contains(dto.Status.ToLower()))
                {
                    return ErrorResponse(400, "Invalid status. Valid values: 'pending', 'taken'.");
                }
                reminder.Status = dto.Status.ToLower();
            }

            // Update fields
            reminder.MedicationName = dto.MedicationName ?? reminder.MedicationName;
            reminder.Description = dto.Description ?? reminder.Description;
            reminder.Dosage = dto.Dosage ?? reminder.Dosage;
            reminder.ReminderDate = dto.ReminderDate != default ? dto.ReminderDate : reminder.ReminderDate;
            reminder.ReminderDue = dto.ReminderDue != default ? dto.ReminderDue : reminder.ReminderDue;
            reminder.Notes = dto.Notes ?? reminder.Notes;
            reminder.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            // Send SNS notification
            await SendMedicationReminderNotification(reminder, dbContext, snsClient, context, "updated");

            return Ok(new
            {
                message = "Medication Reminder updated successfully.",
                reminder
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"UpdateMedicationReminder Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while updating the reminder", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> DeleteMedicationReminder(string path, AppDbContext dbContext)
    {
        try
        {
            var id = ExtractId(path, 2);

            var reminder = await dbContext.MedicationReminders.FindAsync(id);
            if (reminder == null)
                return ErrorResponse(404, $"Medication Reminder with ID {id} not found.");

            dbContext.MedicationReminders.Remove(reminder);
            await dbContext.SaveChangesAsync();

            return Ok(new { message = "Medication Reminder deleted successfully." });
        }
        catch (Exception ex)
        {
            return ErrorResponse(500, "An error occurred while deleting the reminder", ex.Message);
        }
    }

    private async Task SendMedicationReminderNotification(MedicationReminder reminder,
        AppDbContext dbContext,
        IAmazonSimpleNotificationService snsClient,
        ILambdaContext context,
        string action)
    {
        try
        {
            // Get user information for better notification
            var user = await dbContext.Users.FindAsync(reminder.UserId);
            var userName = user != null ? $"{user.FirstName} {user.LastName}" : $"User {reminder.UserId}";

            var actionText = action == "created" ? "Created" : "Updated";

            var message = $@"
Medical Reminder {actionText}

User: {userName} (ID: {reminder.UserId})
Medication: {reminder.MedicationName}
Description: {reminder.Description ?? "Not specified"}
Dosage: {reminder.Dosage ?? "Not specified"}
Reminder Date: {reminder.ReminderDate:yyyy-MM-dd HH:mm}
Reminder Due: {(reminder.ReminderDue != default ? reminder.ReminderDue.ToString(@"hh\:mm") : "Not specified")}
Status: {reminder.Status}
Notes: {reminder.Notes ?? "No notes"}

This is an automated notification from the Diabetes Care System.
            ".Trim();


            

            var publishRequest = new PublishRequest
            {
                TopicArn = SNS_TOPIC_ARN,
                Message = message,
                Subject = $"Medication Reminder - {userName}"
            };

            await snsClient.PublishAsync(publishRequest);
            context.Logger.LogInformation($"SNS notification sent successfully for medication reminder {action}");
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the reminder operation
            context.Logger.LogError($"Error sending SNS notification: {ex.Message}");
        }
    }

    private int ExtractId(string path, int index)
    {
        var parts = path.Split('/');
        return int.Parse(parts[index]);
    }

    private int ExtractUserIdFromPath(string path)
    {
        // Expected path: /medicationreminders/user/{userId}/upcoming
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && int.TryParse(parts[2], out int userId))
            return userId;

        throw new ArgumentException($"Could not extract userId from path: {path}");
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