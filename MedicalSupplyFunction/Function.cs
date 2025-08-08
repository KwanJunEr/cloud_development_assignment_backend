using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.DTO;
using cloud_development_assignment_backend.Services;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.Extensions.NETCore.Setup;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MedicalSupplyFunction;

public class Function
{
    private readonly IServiceProvider _serviceProvider;
    private const string SNS_TOPIC_ARN = "arn:aws:sns:us-east-1:306656959951:MedicalSupplyNotify";

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

        // Register the MedicalSupplyStatusService
        services.AddScoped<MedicalSupplyStatusService>();

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
            var statusService = scope.ServiceProvider.GetRequiredService<MedicalSupplyStatusService>();
            var snsClient = scope.ServiceProvider.GetRequiredService<IAmazonSimpleNotificationService>();

            return await ProcessRequest(request, dbContext, statusService, snsClient, context);
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
        MedicalSupplyStatusService statusService,
        IAmazonSimpleNotificationService snsClient,
        ILambdaContext context)
    {
        var path = request.Path.ToLower();
        var method = request.HttpMethod.ToUpper();

        // Create Medical Supply
        if (path == "/medicalsupply" && method == "POST")
            return await CreateMedicalSupply(request, dbContext, statusService, snsClient, context);

        // Get Medical Supplies by Family ID
        if (path.StartsWith("/medicalsupply/family/") && method == "GET")
            return await GetMedicalSuppliesByFamily(path, dbContext, context);

        // Update Medical Supply
        if (path.StartsWith("/medicalsupply/") && method == "PUT" && IsUpdateByIdPattern(path))
            return await UpdateMedicalSupply(request, path, dbContext, statusService, snsClient, context);

        // Delete Medical Supply
        if (path.StartsWith("/medicalsupply/") && method == "DELETE" && IsDeleteByIdPattern(path))
            return await DeleteMedicalSupply(path, dbContext, context);

        if (method == "OPTIONS")
            return CorsResponse();

        return ErrorResponse(404, "Endpoint not found");
    }

    private async Task<APIGatewayProxyResponse> CreateMedicalSupply(APIGatewayProxyRequest request,
        AppDbContext dbContext,
        MedicalSupplyStatusService statusService,
        IAmazonSimpleNotificationService snsClient,
        ILambdaContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Body))
                return ErrorResponse(400, "Medical supply data is required");

            var dto = JsonSerializer.Deserialize<MedicalSupplyDto>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null)
                return ErrorResponse(400, "Invalid medical supply data");

            // Validate required fields
            if (dto.FamilyId == 0)
                return ErrorResponse(400, "FamilyId is required");

            if (string.IsNullOrWhiteSpace(dto.MedicineName))
                return ErrorResponse(400, "MedicineName is required");

            if (dto.Quantity <= 0)
                return ErrorResponse(400, "Quantity must be greater than 0");

            // Check if the family user exists
            var familyUser = await dbContext.Users.FindAsync(dto.FamilyId);
            if (familyUser == null)
                return ErrorResponse(400, $"Family user with ID {dto.FamilyId} not found");

            // If PatientId is provided, validate it exists, otherwise use FamilyId as PatientId
            int patientId = dto.PatientId > 0 ? dto.PatientId : dto.FamilyId;

            if (dto.PatientId > 0)
            {
                var patientUser = await dbContext.Users.FindAsync(dto.PatientId);
                if (patientUser == null)
                    return ErrorResponse(400, $"Patient user with ID {dto.PatientId} not found");
            }

            var supply = new MedicalSupply
            {
                FamilyId = dto.FamilyId,
                PatientId = patientId,
                MedicineName = dto.MedicineName.Trim(),
                MedicineDescription = string.IsNullOrWhiteSpace(dto.MedicineDescription) ? null : dto.MedicineDescription.Trim(),
                Quantity = dto.Quantity,
                Unit = string.IsNullOrWhiteSpace(dto.Unit) ? null : dto.Unit.Trim(),
                PlaceToPurchase = string.IsNullOrWhiteSpace(dto.PlaceToPurchase) ? null : dto.PlaceToPurchase.Trim(),
                ExpirationDate = dto.ExpirationDate,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                Status = statusService.GetStatus(dto.Quantity),
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            context.Logger.LogInformation($"Creating medical supply: FamilyId={supply.FamilyId}, PatientId={supply.PatientId}, Medicine={supply.MedicineName}");

            dbContext.MedicalSupply.Add(supply);
            await dbContext.SaveChangesAsync();

            context.Logger.LogInformation($"Medical supply created successfully with ID: {supply.Id}");

            // Send SNS notification for creation
            await SendMedicalSupplyNotification(supply, dbContext, snsClient, context, "created");

            return Created(new
            {
                message = "Medical Supply created successfully",
                supply = new
                {
                    supply.Id,
                    supply.FamilyId,
                    supply.PatientId,
                    supply.MedicineName,
                    supply.MedicineDescription,
                    supply.Quantity,
                    supply.Unit,
                    supply.PlaceToPurchase,
                    supply.ExpirationDate,
                    supply.Status,
                    supply.Notes,
                    supply.CreatedDate,
                    supply.UpdatedDate
                }
            });
        }
        catch (DbUpdateException dbEx)
        {
            context.Logger.LogError($"Database Error in CreateMedicalSupply: {dbEx.Message}");
            context.Logger.LogError($"Inner Exception: {dbEx.InnerException?.Message}");

            if (dbEx.InnerException?.Message?.Contains("FK_MedicalSupply_FamilyId") == true)
                return ErrorResponse(400, "Invalid FamilyId - user does not exist");

            if (dbEx.InnerException?.Message?.Contains("FK_MedicalSupply_PatientId") == true)
                return ErrorResponse(400, "Invalid PatientId - user does not exist");

            return ErrorResponse(500, "Database error occurred while creating medical supply", dbEx.Message);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"CreateMedicalSupply Error: {ex.Message}");
            context.Logger.LogError($"Stack Trace: {ex.StackTrace}");
            return ErrorResponse(500, "An error occurred while creating the medical supply", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> GetMedicalSuppliesByFamily(string path,
        AppDbContext dbContext,
        ILambdaContext context)
    {
        try
        {
            var familyId = ExtractFamilyIdFromPath(path);
            context.Logger.LogInformation($"Extracted Family ID: {familyId}");

            var supplies = await dbContext.MedicalSupply
                .Where(s => s.FamilyId == familyId)
                .ToListAsync();

            context.Logger.LogInformation($"Found {supplies.Count} medical supplies for family {familyId}");
            return Ok(supplies);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"GetMedicalSuppliesByFamily Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while retrieving medical supplies", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> UpdateMedicalSupply(APIGatewayProxyRequest request,
        string path,
        AppDbContext dbContext,
        MedicalSupplyStatusService statusService,
        IAmazonSimpleNotificationService snsClient,
        ILambdaContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Body))
                return ErrorResponse(400, "Medical supply update data is required");

            var dto = JsonSerializer.Deserialize<MedicalSupplyDto>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null)
                return ErrorResponse(400, "Invalid medical supply update data");

            var id = ExtractId(path, 2);

            var supply = await dbContext.MedicalSupply.FindAsync(id);
            if (supply == null)
                return ErrorResponse(404, $"Medical Supply with ID {id} not found.");

            // Update fields only if provided
            if (!string.IsNullOrWhiteSpace(dto.MedicineName))
                supply.MedicineName = dto.MedicineName.Trim();

            if (dto.MedicineDescription != null)
                supply.MedicineDescription = string.IsNullOrWhiteSpace(dto.MedicineDescription) ? null : dto.MedicineDescription.Trim();

            if (dto.Quantity > 0)
            {
                supply.Quantity = dto.Quantity;
                supply.Status = statusService.GetStatus(supply.Quantity);
            }

            if (dto.Unit != null)
                supply.Unit = string.IsNullOrWhiteSpace(dto.Unit) ? null : dto.Unit.Trim();

            if (dto.PlaceToPurchase != null)
                supply.PlaceToPurchase = string.IsNullOrWhiteSpace(dto.PlaceToPurchase) ? null : dto.PlaceToPurchase.Trim();

            if (dto.ExpirationDate.HasValue)
                supply.ExpirationDate = dto.ExpirationDate.Value;

            if (dto.Notes != null)
                supply.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

            supply.UpdatedDate = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            // Send SNS notification for update
            await SendMedicalSupplyNotification(supply, dbContext, snsClient, context, "updated");

            return Ok(new
            {
                message = "Medical Supply updated successfully.",
                supply = new
                {
                    supply.Id,
                    supply.FamilyId,
                    supply.PatientId,
                    supply.MedicineName,
                    supply.MedicineDescription,
                    supply.Quantity,
                    supply.Unit,
                    supply.PlaceToPurchase,
                    supply.ExpirationDate,
                    supply.Status,
                    supply.Notes,
                    supply.CreatedDate,
                    supply.UpdatedDate
                }
            });
        }
        catch (DbUpdateException dbEx)
        {
            context.Logger.LogError($"Database Error in UpdateMedicalSupply: {dbEx.Message}");
            context.Logger.LogError($"Inner Exception: {dbEx.InnerException?.Message}");
            return ErrorResponse(500, "Database error occurred while updating medical supply", dbEx.Message);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"UpdateMedicalSupply Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while updating the medical supply", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> DeleteMedicalSupply(string path,
        AppDbContext dbContext,
        ILambdaContext context)
    {
        try
        {
            var id = ExtractId(path, 2);

            var supply = await dbContext.MedicalSupply.FindAsync(id);
            if (supply == null)
                return ErrorResponse(404, $"Medical Supply with ID {id} not found.");

            dbContext.MedicalSupply.Remove(supply);
            await dbContext.SaveChangesAsync();

            return Ok(new { message = "Medical Supply deleted successfully." });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"DeleteMedicalSupply Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while deleting the medical supply", ex.Message);
        }
    }

    private async Task SendMedicalSupplyNotification(MedicalSupply supply,
        AppDbContext dbContext,
        IAmazonSimpleNotificationService snsClient,
        ILambdaContext context,
        string action)
    {
        try
        {
            // Get user information for better notification
            var user = await dbContext.Users.FindAsync(supply.FamilyId);
            var userName = user != null ? $"{user.FirstName} {user.LastName}" : $"User {supply.FamilyId}";

            var actionText = action == "created" ? "Created" : "Updated";

            var message = $@"
Medical Supply {actionText}

Family User: {userName} (ID: {supply.FamilyId})
Patient ID: {supply.PatientId}
Medicine Name: {supply.MedicineName}
Description: {supply.MedicineDescription ?? "Not specified"}
Quantity: {supply.Quantity} {supply.Unit ?? "units"}
Place to Purchase: {supply.PlaceToPurchase ?? "Not specified"}
Expiration Date: {(supply.ExpirationDate?.ToString("yyyy-MM-dd") ?? "Not specified")}
Status: {supply.Status}
Notes: {supply.Notes ?? "No notes"}

This is an automated notification from the Diabetes Care System.
            ".Trim();

            var publishRequest = new PublishRequest
            {
                TopicArn = SNS_TOPIC_ARN,
                Message = message,
                Subject = $"Medical Supply {actionText} - {userName}"
            };

            await snsClient.PublishAsync(publishRequest);
            context.Logger.LogInformation($"SNS notification sent successfully for medical supply {action}");
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the supply operation
            context.Logger.LogError($"Error sending SNS notification: {ex.Message}");
        }
    }

    private int ExtractId(string path, int index)
    {
        var parts = path.Split('/');
        if (parts.Length <= index || !int.TryParse(parts[index], out int id))
            throw new ArgumentException($"Could not extract ID from path: {path}");
        return id;
    }

    private int ExtractFamilyIdFromPath(string path)
    {
        // Expected path: /medicalsupply/family/{familyId}
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && int.TryParse(parts[2], out int familyId))
            return familyId;

        throw new ArgumentException($"Could not extract familyId from path: {path}");
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
        Body = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
        Headers = CorsHeaders()
    };

    private APIGatewayProxyResponse Created(object data) => new()
    {
        StatusCode = 201,
        Body = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
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
        Body = JsonSerializer.Serialize(new { error = message, details }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
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