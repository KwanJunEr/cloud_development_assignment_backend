using Amazon.Lambda.Core;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.DTO;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace NotificationService;

public class Function
{

    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
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

        services.AddAWSService<IAmazonSimpleNotificationService>();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processing {request.HttpMethod} {request.Path}");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var snsClient = scope.ServiceProvider.GetRequiredService<IAmazonSimpleNotificationService>();

            var result = await ProcessRequest(request, dbContext, snsClient, context);
            context.Logger.LogInformation($"Request completed successfully with status {result.StatusCode}");
            return result;
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing request: {ex.Message}");
            context.Logger.LogError($"Stack trace: {ex.StackTrace}");
            return CreateErrorResponse(500, "Internal server error", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> ProcessRequest(
        APIGatewayProxyRequest request,
        AppDbContext dbContext,
        IAmazonSimpleNotificationService snsClient,
        ILambdaContext context)
    {
        var path = request.Path.ToLower();
        var method = request.HttpMethod.ToUpper();

        return (path, method) switch
        {
            // POST /notifications
            ("/notifications", "POST") =>
                await CreateNotification(request, dbContext, snsClient, context),

            // GET /notifications/{physicianId}
            _ when path.StartsWith("/notifications/") && method == "GET" && IsGetNotificationsPattern(path) =>
                await GetNotifications(request, dbContext, context),

            // GET /notifications/{physicianId}/unread
            _ when path.Contains("/notifications/") && path.EndsWith("/unread") && method == "GET" =>
                await GetUnreadNotifications(request, dbContext, context),

            // GET /notifications/{physicianId}/type/{type}
            _ when path.Contains("/notifications/") && path.Contains("/type/") && method == "GET" =>
                await GetNotificationsByType(request, dbContext, context),

            // PUT /notification/{notificationId}/read
            _ when path.StartsWith("/notification/") && path.EndsWith("/read") && method == "PUT" =>
                await MarkAsRead(request, dbContext, context),

            // DELETE /notification/{notificationId}
            _ when path.StartsWith("/notification/") && method == "DELETE" && IsDeleteNotificationPattern(path) =>
                await DeleteNotification(request, dbContext, context),

            // OPTIONS requests for CORS
            (_, "OPTIONS") => CreateCorsResponse(),

            _ => CreateErrorResponse(404, "Endpoint not found")
        };
    }

    private bool IsGetNotificationsPattern(string path)
    {
        var parts = path.Split('/');
        return parts.Length == 3 && parts[1] == "notifications" && int.TryParse(parts[2], out _);
    }

    private bool IsDeleteNotificationPattern(string path)
    {
        var parts = path.Split('/');
        return parts.Length == 3 && parts[1] == "notification" && int.TryParse(parts[2], out _);
    }

    private async Task<APIGatewayProxyResponse> GetNotifications(APIGatewayProxyRequest request, AppDbContext dbContext, ILambdaContext context)
    {
        try
        {
            var physicianId = ExtractPhysicianId(request.Path);
            if (physicianId == null)
            {
                context.Logger.LogWarning("Invalid physician ID in path");
                return CreateErrorResponse(400, "Invalid physician ID");
            }

            var typeFilter = request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey("type")
                ? request.QueryStringParameters["type"] : null;
            var unreadFilter = request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey("unread")
                ? request.QueryStringParameters["unread"] : null;

            var query = dbContext.PhysicianNotifications.Where(n => n.PhysicianId == physicianId.Value);

            if (!string.IsNullOrEmpty(typeFilter))
            {
                query = query.Where(n => n.Type == typeFilter);
                context.Logger.LogInformation($"Applied type filter: {typeFilter}");
            }

            if (bool.TryParse(unreadFilter, out var isUnread))
            {
                query = query.Where(n => n.IsRead == !isUnread);
                context.Logger.LogInformation($"Applied unread filter: {isUnread}");
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new PhysicianNotificationOutputDto
                {
                    Id = n.Id,
                    Message = n.Message,
                    CreatedAt = n.CreatedAt.ToString("o"),
                    IsRead = n.IsRead,
                    PhysicianId = n.PhysicianId,
                    Type = n.Type,
                    Sender = n.Sender,
                    Subject = n.Subject
                })
                .ToListAsync();

            context.Logger.LogInformation($"Retrieved {notifications.Count} notifications for physician {physicianId}");
            return CreateSuccessResponse(notifications);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error getting notifications: {ex.Message}");
            return CreateErrorResponse(500, "Error retrieving notifications", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> GetUnreadNotifications(APIGatewayProxyRequest request, AppDbContext dbContext, ILambdaContext context)
    {
        try
        {
            var physicianId = ExtractPhysicianId(request.Path.Replace("/unread", ""));
            if (physicianId == null)
            {
                context.Logger.LogWarning("Invalid physician ID in unread notifications path");
                return CreateErrorResponse(400, "Invalid physician ID");
            }

            var notifications = await dbContext.PhysicianNotifications
                .Where(n => n.PhysicianId == physicianId.Value && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new PhysicianNotificationOutputDto
                {
                    Id = n.Id,
                    Message = n.Message,
                    CreatedAt = n.CreatedAt.ToString("o"),
                    IsRead = n.IsRead,
                    PhysicianId = n.PhysicianId,
                    Type = n.Type,
                    Sender = n.Sender,
                    Subject = n.Subject
                })
                .ToListAsync();

            context.Logger.LogInformation($"Retrieved {notifications.Count} unread notifications for physician {physicianId}");
            return CreateSuccessResponse(notifications);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error getting unread notifications: {ex.Message}");
            return CreateErrorResponse(500, "Error retrieving unread notifications", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> GetNotificationsByType(APIGatewayProxyRequest request, AppDbContext dbContext, ILambdaContext context)
    {
        try
        {
            var pathParts = request.Path.Split('/');
            if (pathParts.Length < 5)
            {
                context.Logger.LogWarning($"Invalid path format for notifications by type: {request.Path}");
                return CreateErrorResponse(400, "Invalid path format. Expected: /notifications/{physicianId}/type/{type}");
            }


            if (!int.TryParse(pathParts[2], out var physicianId))
            {
                context.Logger.LogWarning($"Invalid physician ID in type filter path: {pathParts[2]}");
                return CreateErrorResponse(400, "Invalid physician ID");
            }

            var type = pathParts[4];

            var notifications = await dbContext.PhysicianNotifications
                .Where(n => n.PhysicianId == physicianId && n.Type == type)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new PhysicianNotificationOutputDto
                {
                    Id = n.Id,
                    Message = n.Message,
                    CreatedAt = n.CreatedAt.ToString("o"),
                    IsRead = n.IsRead,
                    PhysicianId = n.PhysicianId,
                    Type = n.Type,
                    Sender = n.Sender,
                    Subject = n.Subject
                })
                .ToListAsync();

            context.Logger.LogInformation($"Retrieved {notifications.Count} notifications of type '{type}' for physician {physicianId}");
            return CreateSuccessResponse(notifications);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error getting notifications by type: {ex.Message}");
            return CreateErrorResponse(500, "Error retrieving notifications by type", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> CreateNotification(
        APIGatewayProxyRequest request,
        AppDbContext dbContext,
        IAmazonSimpleNotificationService snsClient,
        ILambdaContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Body))
            {
                context.Logger.LogWarning("Create notification request received with empty body");
                return CreateErrorResponse(400, "Request body is required");
            }

            var createDto = JsonSerializer.Deserialize<CreateNotificationDto>(request.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (createDto == null)
            {
                context.Logger.LogWarning("Failed to deserialize notification request body");
                return CreateErrorResponse(400, "Invalid request body");
            }

            if (createDto.PhysicianId == 0 ||
                string.IsNullOrEmpty(createDto.Message) ||
                string.IsNullOrEmpty(createDto.Type))
            {
                context.Logger.LogWarning($"Invalid notification data - PhysicianId: {createDto.PhysicianId}, Message: {!string.IsNullOrEmpty(createDto.Message)}, Type: {!string.IsNullOrEmpty(createDto.Type)}");
                return CreateErrorResponse(400, "PhysicianId, Message, and Type are required fields");
            }

            var physicianExists = await dbContext.Users.AnyAsync(u => u.Id == createDto.PhysicianId);
            if (!physicianExists)
            {
                context.Logger.LogWarning($"Physician with ID {createDto.PhysicianId} not found");
                return CreateErrorResponse(404, "Physician not found");
            }

            var notification = new PhysicianNotification
            {
                PhysicianId = createDto.PhysicianId,
                Message = createDto.Message,
                Type = createDto.Type,
                Sender = createDto.Sender,
                Subject = createDto.Subject,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            dbContext.PhysicianNotifications.Add(notification);
            await dbContext.SaveChangesAsync();

            context.Logger.LogInformation($"Created notification {notification.Id} for physician {notification.PhysicianId}");

            try
            {
                await PublishToSNS(snsClient, notification, context);
                Console.WriteLine("SNS Done");
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Failed to publish to SNS: {ex.Message}");
                Console.WriteLine("SNS Fail");
            }

            var responseDto = new PhysicianNotificationOutputDto
            {
                Id = notification.Id,
                Message = notification.Message,
                CreatedAt = notification.CreatedAt.ToString("o"),
                IsRead = notification.IsRead,
                PhysicianId = notification.PhysicianId,
                Type = notification.Type,
                Sender = notification.Sender,
                Subject = notification.Subject
            };

            return CreateSuccessResponse(responseDto, 201);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error creating notification: {ex.Message}");
            return CreateErrorResponse(500, "Error creating notification", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> MarkAsRead(APIGatewayProxyRequest request, AppDbContext dbContext, ILambdaContext context)
    {
        try
        {
            var notificationId = ExtractNotificationId(request.Path.Replace("/read", ""));
            if (notificationId == null)
            {
                context.Logger.LogWarning($"Invalid notification ID in mark as read path: {request.Path}");
                return CreateErrorResponse(400, "Invalid notification ID");
            }

            var notification = await dbContext.PhysicianNotifications.FindAsync(notificationId.Value);
            if (notification == null)
            {
                context.Logger.LogWarning($"Notification {notificationId} not found for mark as read");
                return CreateErrorResponse(404, "Notification not found");
            }

            notification.IsRead = true;
            await dbContext.SaveChangesAsync();

            context.Logger.LogInformation($"Marked notification {notificationId} as read");

            return CreateSuccessResponse(new { message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error marking notification as read: {ex.Message}");
            return CreateErrorResponse(500, "Error updating notification", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> DeleteNotification(
        APIGatewayProxyRequest request,
        AppDbContext dbContext,
        ILambdaContext context)
    {
        try
        {
            var notificationId = ExtractNotificationId(request.Path);
            if (notificationId == null)
            {
                context.Logger.LogWarning($"Invalid notification ID in delete path: {request.Path}");
                return CreateErrorResponse(400, "Invalid notification ID");
            }

            var notification = await dbContext.PhysicianNotifications.FindAsync(notificationId.Value);
            if (notification == null)
            {
                context.Logger.LogWarning($"Notification {notificationId} not found for deletion");
                return CreateErrorResponse(404, "Notification not found");
            }

            dbContext.PhysicianNotifications.Remove(notification);
            await dbContext.SaveChangesAsync();

            context.Logger.LogInformation($"Deleted notification {notificationId}");

            return CreateSuccessResponse(new { message = "Notification deleted successfully" });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error deleting notification: {ex.Message}");
            return CreateErrorResponse(500, "Error deleting notification", ex.Message);
        }
    }

    private async Task PublishToSNS(
        IAmazonSimpleNotificationService snsClient,
        PhysicianNotification notification,
        ILambdaContext context)
    {
        try
        {
            var topicArn = Environment.GetEnvironmentVariable("SNS_TOPIC_ARN");
            if (string.IsNullOrEmpty(topicArn))
            {
                context.Logger.LogInformation("SNS_TOPIC_ARN not configured, skipping SNS publish");
                return;
            }

            var snsMessage = new SNSNotificationDto
            {
                NotificationId = notification.Id,
                PhysicianId = notification.PhysicianId,
                Message = notification.Message,
                Type = notification.Type,
                Sender = notification.Sender,
                Subject = notification.Subject,
                CreatedAt = notification.CreatedAt
            };

            await snsClient.PublishAsync(new PublishRequest
            {
                TopicArn = topicArn,
                Message = JsonSerializer.Serialize(snsMessage),
                Subject = $"New {notification.Type} Notification",
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["NotificationType"] = new MessageAttributeValue { StringValue = notification.Type, DataType = "String" },
                    ["PhysicianId"] = new MessageAttributeValue { StringValue = notification.PhysicianId.ToString(), DataType = "Number" }
                }
            });

            context.Logger.LogInformation($"Published notification {notification.Id} to SNS topic {topicArn}");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Failed to publish notification {notification.Id} to SNS: {ex.Message}");
        }
    }

    private int? ExtractPhysicianId(string path)
    {
        var parts = path.Split('/');
        if (parts.Length >= 3 && int.TryParse(parts[2], out var id))
            return id;
        return null;
    }

    private int? ExtractNotificationId(string path)
    {
        var parts = path.Split('/');
        if (parts.Length >= 3 && int.TryParse(parts[2], out var id))
            return id;
        return null;
    }

    private APIGatewayProxyResponse CreateCorsResponse()
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string>
            {
                { "Access-Control-Allow-Origin", "*" },
                { "Access-Control-Allow-Headers", "*" },
                { "Access-Control-Allow-Methods", "*" }
            },
            Body = ""
        };
    }

    private APIGatewayProxyResponse CreateSuccessResponse(object data, int statusCode = 200)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Access-Control-Allow-Origin", "*" },
                { "Access-Control-Allow-Headers", "*" },
                { "Access-Control-Allow-Methods", "*" }
            },
            Body = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };
    }

    private APIGatewayProxyResponse CreateErrorResponse(int statusCode, string message, string? details = null)
    {
        var errorResponse = new
        {
            error = message,
            details = details,
            timestamp = DateTime.UtcNow.ToString("o"),
            requestId = Environment.GetEnvironmentVariable("AWS_REQUEST_ID")
        };

        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Access-Control-Allow-Origin", "*" },
                { "Access-Control-Allow-Headers", "*" },
                { "Access-Control-Allow-Methods", "*" }
            },
            Body = JsonSerializer.Serialize(errorResponse)
        };
    }
}
