using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.DTO;
using Amazon.SimpleNotificationService;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace DietTipService;

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
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var snsClient = scope.ServiceProvider.GetRequiredService<IAmazonSimpleNotificationService>();
            return await ProcessRequest(request, dbContext, snsClient);
        }
        catch (Exception ex)
        {
            return ErrorResponse(500, "Internal server error", ex.Message);
        }
    }


    private async Task<APIGatewayProxyResponse> ProcessRequest(
        APIGatewayProxyRequest request,
        AppDbContext dbContext,
        IAmazonSimpleNotificationService snsClient)
    {
        var path = request.Path.ToLower();
        var method = request.HttpMethod.ToUpper();

        return (path, method) switch
        {
            ("/diettip", "GET") => await GetAllDietTips(dbContext),
            ("/diettip", "POST") => await CreateDietTip(request, dbContext, snsClient),
            _ when path.StartsWith("/diettip/dietician/") && method == "GET" =>
                await GetByDieticianId(path, dbContext),
            _ when path.StartsWith("/diettip/") && method == "PUT" =>
                await UpdateDietTip(path, request, dbContext),
            _ when path.StartsWith("/diettip/") && method == "DELETE" =>
                await DeleteDietTip(path, dbContext),
            ("/diettip/getalldiettip", "GET") => await GetAllDietTipsForPatient(dbContext),
            (_, "OPTIONS") => CorsResponse(),
            _ => ErrorResponse(404, "Endpoint not found")
        };
    }

    private async Task<APIGatewayProxyResponse> CreateDietTip(
        APIGatewayProxyRequest request,
        AppDbContext dbContext,
        IAmazonSimpleNotificationService snsClient)
    {
        var dto = JsonSerializer.Deserialize<CreateDietTipDto>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (dto == null)
        {
            return ErrorResponse(400, "Invalid DietTip data");
        }

        var dietTip = new DietTip
        {
            DieticianId = dto.DieticianId,
            Title = dto.Title,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.DietTip.Add(dietTip);
        await dbContext.SaveChangesAsync();

        var topicArn = Environment.GetEnvironmentVariable("SNS_TOPIC_ARN");
        if (!string.IsNullOrEmpty(topicArn))
        {
            var message =
                $"Event: DietTipCreated\n" +
                $"DietTipId: {dietTip.Id}\n" +
                $"DieticianId: {dietTip.DieticianId}\n" +
                $"Title: {dietTip.Title}\n" +
                $"CreatedAt: {dietTip.CreatedAt:yyyy-MM-ddTHH:mm:ssZ}";

            await snsClient.PublishAsync(new Amazon.SimpleNotificationService.Model.PublishRequest
            {
                TopicArn = topicArn,
                Message = message,
                Subject = "New DietTip Created"
            });
        }

        return Ok(new { message = "Created DietTip Successfully", dietTip }, 201);
    }

    private async Task<APIGatewayProxyResponse> GetAllDietTips(AppDbContext dbContext)
    {
        var tips = await dbContext.DietTip.ToListAsync();
        return Ok(new { message = "Return all diettips", tips });
    }

    private async Task<APIGatewayProxyResponse> GetByDieticianId(string path, AppDbContext dbContext)
    {
        var dieticianId = int.Parse(path.Split('/')[3]);
        var tips = await dbContext.DietTip
            .Where(t => t.DieticianId == dieticianId)
            .ToListAsync();

        return Ok(new { message = "Retrieve all records for the individual dietician", tips });
    }

    private async Task<APIGatewayProxyResponse> UpdateDietTip(string path, APIGatewayProxyRequest request, AppDbContext dbContext)
    {
        var id = int.Parse(path.Split('/')[2]);
        var dto = JsonSerializer.Deserialize<CreateDietTipDto>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (dto == null)
        {
            return ErrorResponse(400, "Invalid DietTip data");
        }

        var dietTip = await dbContext.DietTip.FindAsync(id);
        if (dietTip == null)
        {
            return ErrorResponse(404, "DietTip not found");
        }

        dietTip.Title = dto.Title;
        dietTip.Content = dto.Content;
        dietTip.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return Ok(new { message = "DietTip updated successfully", dietTip });
    }

    private async Task<APIGatewayProxyResponse> DeleteDietTip(string path, AppDbContext dbContext)
    {
        var id = int.Parse(path.Split('/')[2]);
        var dietTip = await dbContext.DietTip.FindAsync(id);
        if (dietTip == null)
        {
            return ErrorResponse(404, "DietTip not found");
        }

        dbContext.DietTip.Remove(dietTip);
        await dbContext.SaveChangesAsync();

        return Ok(new { message = "DietTip deleted successfully" });
    }

    private async Task<APIGatewayProxyResponse> GetAllDietTipsForPatient(AppDbContext dbContext)
    {
        var tips = await (
            from dt in dbContext.DietTip
            join u in dbContext.Users on dt.DieticianId equals u.Id
            select new DietTipResponseDto
            {
                Id = dt.Id,
                Title = dt.Title,
                Content = dt.Content,
                CreatedAt = dt.CreatedAt,
                UpdatedAt = dt.UpdatedAt,
                DieticianFullName = u.FirstName + " " + u.LastName,
                DieticianSpecialization = u.Specialization,
                DieticianHospital = u.Hospital
            }
        ).ToListAsync();

        return Ok(new { message = "Return all diet tips with dietician details", tips });
    }

    private APIGatewayProxyResponse Ok(object data, int statusCode = 200) => new()
    {
        StatusCode = statusCode,
        Body = JsonSerializer.Serialize(data),
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
