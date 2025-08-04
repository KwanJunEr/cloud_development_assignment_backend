using Amazon.Lambda.Core;
using System.Text.Json;
using System;
using Amazon.Lambda.APIGatewayEvents;
using cloud_development_assignment_backend.Data; // Your shared library namespace
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Amazon.SimpleNotificationService;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FollowUpService;

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
            var result = await ProcessRequest(request, dbContext, context, snsClient);
            context.Logger.LogInformation($"Request completed successfully with status {result.StatusCode}");
            return result;
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing request: {ex.Message}");
            return CreateErrorResponse(500, "Internal server error", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> ProcessRequest(
        APIGatewayProxyRequest request,
        AppDbContext dbContext,
        ILambdaContext context,
        IAmazonSimpleNotificationService snsClient)
    {
        var path = request.Path.ToLower();
        var method = request.HttpMethod.ToUpper();

        // Routing
        return (path, method) switch
        {
            ("/followup", "GET") => await GetAllFollowUps(dbContext),
            _ when path.StartsWith("/followup/") && method == "GET" && IsGetByIdPattern(path) =>
                await GetFollowUpById(path, dbContext),
            _ when path.StartsWith("/followup/patient/") && method == "GET" =>
                await GetFollowUpsByPatientId(path, dbContext),
            _ when path.StartsWith("/followup/physician/") && path.EndsWith("/not-resolved") && method == "GET" =>
                await GetFollowUpsForPhysicianNotResolved(path, dbContext),
            _ when path.StartsWith("/followup/physician/") && path.Split('/').Length == 4 && method == "GET" =>
                await GetFollowUpsByPhysicianId(path, dbContext),
            _ when path.StartsWith("/followup/physician/") && path.Contains("/status/") && method == "GET" =>
                await GetFollowUpsByStatus(path, dbContext),
            _ when path.StartsWith("/followup/physician/") && path.Contains("/urgency/") && method == "GET" =>
                await GetFollowUpsByUrgency(path, dbContext),
            ("/followup", "POST") => await CreateFollowUp(request, dbContext, snsClient),
            _ when path.StartsWith("/followup/") && path.EndsWith("/status") && method == "PUT" =>
                await UpdateFollowUpStatus(path, request, dbContext),
            _ when path.StartsWith("/followup/") && path.EndsWith("/resolve") && method == "PUT" =>
                await MarkAsResolved(path, dbContext),
            (_, "OPTIONS") => CreateCorsResponse(),
            _ => CreateErrorResponse(404, "Endpoint not found")
        };
    }

    //Endpoint Implementations 

    private async Task<APIGatewayProxyResponse> GetAllFollowUps(AppDbContext dbContext)
    {
        var followUps = await dbContext.FollowUps
            .OrderByDescending(f => f.FlaggedDate)
            .ToListAsync();

        var result = followUps.Select(f =>
        {
            var user = dbContext.Users.FirstOrDefault(u => u.Id == f.PatientId);
            return new FollowUpOutputDto
            {
                Id = f.Id,
                PatientId = f.PatientId,
                PhysicianId = f.PhysicianId,
                PatientName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                FlaggedDate = f.FlaggedDate,
                FlagReason = f.FlagReason ?? "",
                FlaggedBy = f.FlaggedBy ?? "",
                UrgencyLevel = f.UrgencyLevel ?? "",
                Status = f.Status ?? "",
                FollowUpDate = f.FollowUpDate,
                FollowUpNotes = f.FollowUpNotes ?? ""
            };
        }).ToList();

        return CreateSuccessResponse(result);
    }

    private async Task<APIGatewayProxyResponse> GetFollowUpById(string path, AppDbContext dbContext)
    {
        var id = int.Parse(path.Split('/')[2]);
        var followUp = await dbContext.FollowUps.FirstOrDefaultAsync(f => f.Id == id);

        if (followUp == null)
        {
            return CreateErrorResponse(404, "Follow-up not found");
        }

        var user = dbContext.Users.FirstOrDefault(u => u.Id == followUp.PatientId);
        var dto = new FollowUpOutputDto
        {
            Id = followUp.Id,
            PatientId = followUp.PatientId,
            PhysicianId = followUp.PhysicianId,
            PatientName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
            FlaggedDate = followUp.FlaggedDate,
            FlagReason = followUp.FlagReason ?? "",
            FlaggedBy = followUp.FlaggedBy ?? "",
            UrgencyLevel = followUp.UrgencyLevel ?? "",
            Status = followUp.Status ?? "",
            FollowUpDate = followUp.FollowUpDate,
            FollowUpNotes = followUp.FollowUpNotes ?? ""
        };

        return CreateSuccessResponse(dto);
    }

    private async Task<APIGatewayProxyResponse> GetFollowUpsByPatientId(string path, AppDbContext dbContext)
    {
        var patientId = int.Parse(path.Split('/')[3]);
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == patientId);
        if (user == null)
        {
            return CreateErrorResponse(404, $"Patient with ID {patientId} not found");
        }

        var followUps = await dbContext.FollowUps
            .Where(f => f.PatientId == patientId)
            .OrderByDescending(f => f.FlaggedDate)
            .ToListAsync();

        var result = followUps.Select(f => new FollowUpOutputDto
        {
            Id = f.Id,
            PatientId = f.PatientId,
            PhysicianId = f.PhysicianId,
            PatientName = $"{user.FirstName} {user.LastName}",
            FlaggedDate = f.FlaggedDate,
            FlagReason = f.FlagReason ?? "",
            FlaggedBy = f.FlaggedBy ?? "",
            UrgencyLevel = f.UrgencyLevel ?? "",
            Status = f.Status ?? "",
            FollowUpDate = f.FollowUpDate,
            FollowUpNotes = f.FollowUpNotes ?? ""
        }).ToList();

        return CreateSuccessResponse(result);
    }

    private async Task<APIGatewayProxyResponse> GetFollowUpsForPhysicianNotResolved(string path, AppDbContext dbContext)
    {
        var physicianId = int.Parse(path.Split('/')[3]);
        var followUps = await dbContext.FollowUps
            .Where(f => f.PhysicianId == physicianId && f.Status.ToLower() != "resolved")
            .OrderByDescending(f => f.FlaggedDate)
            .ToListAsync();

        var result = followUps.Select(f =>
        {
            var user = dbContext.Users.FirstOrDefault(u => u.Id == f.PatientId);
            return new FollowUpOutputDto
            {
                Id = f.Id,
                PatientId = f.PatientId,
                PhysicianId = f.PhysicianId,
                PatientName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                FlaggedDate = f.FlaggedDate,
                FlagReason = f.FlagReason ?? "",
                FlaggedBy = f.FlaggedBy ?? "",
                UrgencyLevel = f.UrgencyLevel ?? "",
                Status = f.Status ?? "",
                FollowUpDate = f.FollowUpDate,
                FollowUpNotes = f.FollowUpNotes ?? ""
            };
        }).ToList();

        return CreateSuccessResponse(result);
    }

    private async Task<APIGatewayProxyResponse> GetFollowUpsByPhysicianId(string path, AppDbContext dbContext)
    {
        var physicianId = int.Parse(path.Split('/')[3]);
        var followUps = await dbContext.FollowUps
            .Where(f => f.PhysicianId == physicianId)
            .OrderByDescending(f => f.FlaggedDate)
            .ToListAsync();

        var result = followUps.Select(f =>
        {
            var user = dbContext.Users.FirstOrDefault(u => u.Id == f.PatientId);
            return new FollowUpOutputDto
            {
                Id = f.Id,
                PatientId = f.PatientId,
                PhysicianId = f.PhysicianId,
                PatientName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                FlaggedDate = f.FlaggedDate,
                FlagReason = f.FlagReason ?? "",
                FlaggedBy = f.FlaggedBy ?? "",
                UrgencyLevel = f.UrgencyLevel ?? "",
                Status = f.Status ?? "",
                FollowUpDate = f.FollowUpDate,
                FollowUpNotes = f.FollowUpNotes ?? ""
            };
        }).ToList();

        return CreateSuccessResponse(result);
    }

    private async Task<APIGatewayProxyResponse> GetFollowUpsByStatus(string path, AppDbContext dbContext)
    {
        var parts = path.Split('/');
        var physicianId = int.Parse(parts[3]);
        var status = parts[5];

        if (string.IsNullOrEmpty(status) || !(status == "pending" || status == "scheduled" || status == "resolved"))
        {
            return CreateErrorResponse(400, "Invalid status. Valid values are: pending, scheduled, resolved");
        }

        var followUps = await dbContext.FollowUps
            .Where(f => f.PhysicianId == physicianId && f.Status.ToLower() == status.ToLower())
            .OrderByDescending(f => f.FlaggedDate)
            .ToListAsync();

        var result = followUps.Select(f =>
        {
            var user = dbContext.Users.FirstOrDefault(u => u.Id == f.PatientId);
            return new FollowUpOutputDto
            {
                Id = f.Id,
                PatientId = f.PatientId,
                PhysicianId = f.PhysicianId,
                PatientName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                FlaggedDate = f.FlaggedDate,
                FlagReason = f.FlagReason ?? "",
                FlaggedBy = f.FlaggedBy ?? "",
                UrgencyLevel = f.UrgencyLevel ?? "",
                Status = f.Status ?? "",
                FollowUpDate = f.FollowUpDate,
                FollowUpNotes = f.FollowUpNotes ?? ""
            };
        }).ToList();

        return CreateSuccessResponse(result);
    }

    private async Task<APIGatewayProxyResponse> GetFollowUpsByUrgency(string path, AppDbContext dbContext)
    {
        var parts = path.Split('/');
        var physicianId = int.Parse(parts[3]);
        var urgencyLevel = parts[5];

        if (string.IsNullOrEmpty(urgencyLevel) || !(urgencyLevel == "low" || urgencyLevel == "medium" || urgencyLevel == "high"))
        {
            return CreateErrorResponse(400, "Invalid urgency level. Valid values are: low, medium, high");
        }

        var followUps = await dbContext.FollowUps
            .Where(f => f.PhysicianId == physicianId && f.UrgencyLevel.ToLower() == urgencyLevel.ToLower())
            .OrderByDescending(f => f.FlaggedDate)
            .ToListAsync();

        var result = followUps.Select(f =>
        {
            var user = dbContext.Users.FirstOrDefault(u => u.Id == f.PatientId);
            return new FollowUpOutputDto
            {
                Id = f.Id,
                PatientId = f.PatientId,
                PhysicianId = f.PhysicianId,
                PatientName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                FlaggedDate = f.FlaggedDate,
                FlagReason = f.FlagReason ?? "",
                FlaggedBy = f.FlaggedBy ?? "",
                UrgencyLevel = f.UrgencyLevel ?? "",
                Status = f.Status ?? "",
                FollowUpDate = f.FollowUpDate,
                FollowUpNotes = f.FollowUpNotes ?? ""
            };
        }).ToList();

        return CreateSuccessResponse(result);
    }

    private async Task<APIGatewayProxyResponse> CreateFollowUp(
        APIGatewayProxyRequest request,
        AppDbContext dbContext,
        IAmazonSimpleNotificationService snsClient)
    {
        if (string.IsNullOrEmpty(request.Body))
        {
            Console.WriteLine("Request body is empty.");
            return CreateErrorResponse(400, "Follow-up data is required");
        }
            

        var dto = JsonSerializer.Deserialize<FollowUpDto>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (dto == null)
        {
            Console.WriteLine("DTO is null after deserialization.");
            return CreateErrorResponse(400, "Invalid follow-up data.");
        }

        if (dto.PatientId == 0)
        {
            return CreateErrorResponse(400, "Patient ID is required");
        }
        if (dto.PhysicianId == 0)
        {
            return CreateErrorResponse(400, "Physician ID is required");
        }
        if (string.IsNullOrEmpty(dto.FlagReason))
        {
            return CreateErrorResponse(400, "Flag reason is required");
        }
        if (string.IsNullOrEmpty(dto.UrgencyLevel))
        {
            return CreateErrorResponse(400, "Urgency level is required");
        }

        try
        {
            var user = dbContext.Users.FirstOrDefault(u => u.Id == dto.PatientId);
            if (user == null)
            {
                Console.WriteLine("Patient not found.");
                return CreateErrorResponse(404, $"Patient with ID {dto.PatientId} not found");
            }

            var physician = dbContext.Users.FirstOrDefault(u => u.Id == dto.PhysicianId);
            if (physician == null)
            {
                Console.WriteLine("Physician not found.");
                return CreateErrorResponse(404, $"Physician with ID {dto.PhysicianId} not found");
            }

            string urgency = dto.UrgencyLevel.ToLower();
            if (urgency != "low" && urgency != "medium" && urgency != "high")
            {
                return CreateErrorResponse(400, "Invalid urgency level. Valid values are: low, medium, high");
            }

            var followUp = new FollowUp
            {
                PatientId = dto.PatientId,
                PhysicianId = dto.PhysicianId,
                FlaggedDate = dto.FlaggedDate != default ? dto.FlaggedDate : DateTime.Now,
                FlagReason = dto.FlagReason,
                FlaggedBy = dto.FlaggedBy,
                UrgencyLevel = dto.UrgencyLevel,
                Status = dto.Status ?? "pending",
                FollowUpDate = dto.FollowUpDate,
                FollowUpNotes = dto.FollowUpNotes
            };

            dbContext.FollowUps.Add(followUp);
            await dbContext.SaveChangesAsync();

            var topicArn = Environment.GetEnvironmentVariable("SNS_TOPIC_ARN");
            if (!string.IsNullOrEmpty(topicArn))
            {
                var message =
                    $"Event: Follow Up Created\n" +
                    $"FollowUpId: {followUp.Id}\n" +
                    $"PatientId: {followUp.PatientId}\n" +
                    $"PhysicianId: {followUp.PhysicianId}\n" +
                    $"FlaggedDate: {followUp.FlaggedDate:yyyy-MM-ddTHH:mm:ssZ}\n" +
                    $"UrgencyLevel: {followUp.UrgencyLevel}\n" +
                    $"Status: {followUp.Status}";

                await snsClient.PublishAsync(new Amazon.SimpleNotificationService.Model.PublishRequest
                {
                    TopicArn = topicArn,
                    Message = message,
                    Subject = "New Follow-Up Created"
                });
            }

            var outputDto = new FollowUpOutputDto
            {
                Id = followUp.Id,
                PatientId = followUp.PatientId,
                PhysicianId = followUp.PhysicianId,
                PatientName = $"{user.FirstName} {user.LastName}",
                FlaggedDate = followUp.FlaggedDate,
                FlagReason = followUp.FlagReason ?? "",
                FlaggedBy = followUp.FlaggedBy ?? "",
                UrgencyLevel = followUp.UrgencyLevel ?? "",
                Status = followUp.Status ?? "",
                FollowUpDate = followUp.FollowUpDate,
                FollowUpNotes = followUp.FollowUpNotes ?? ""
            };

            return CreateSuccessResponse(outputDto, 201);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(500, "Internal server error", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> UpdateFollowUpStatus(string path, APIGatewayProxyRequest request, AppDbContext dbContext)
    {
        var id = int.Parse(path.Split('/')[2]);
        var status = JsonSerializer.Deserialize<string>(request.Body);
        if (string.IsNullOrEmpty(status))
        {
            return CreateErrorResponse(400, "Status is required");
        }

        var followUp = await dbContext.FollowUps.FirstOrDefaultAsync(f => f.Id == id);
        if (followUp == null)
        {
            return CreateErrorResponse(404, "Follow-up not found");
        }

        followUp.Status = status;
        await dbContext.SaveChangesAsync();

        return CreateSuccessResponse(new { message = "Status updated" }, 204);
    }

    private async Task<APIGatewayProxyResponse> MarkAsResolved(string path, AppDbContext dbContext)
    {
        var id = int.Parse(path.Split('/')[2]);
        var followUp = await dbContext.FollowUps.FirstOrDefaultAsync(f => f.Id == id);
        if (followUp == null)
        {
            return CreateErrorResponse(404, "Follow-up not found");
        }

        followUp.Status = "resolved";
        await dbContext.SaveChangesAsync();

        return CreateSuccessResponse(new { message = "Marked as resolved" }, 204);
    }


    private bool IsGetByIdPattern(string path)
    {
        var parts = path.Split('/');
        return parts.Length == 3 && int.TryParse(parts[2], out _);
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
            timestamp = DateTime.UtcNow.ToString("o")
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
