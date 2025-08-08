using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.DTO;
using Amazon;
using Amazon.S3;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PrescriptionService;

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

        services.AddAWSService<IAmazonS3>();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var s3Client = scope.ServiceProvider.GetRequiredService<IAmazonS3>();
            return await ProcessRequest(request, dbContext, context, s3Client);
        }
        catch (Exception ex)
        {
            return ErrorResponse(500, "Internal server error", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> ProcessRequest(
        APIGatewayProxyRequest request,AppDbContext dbContext,
        ILambdaContext context,IAmazonS3 s3Client)
    {
        var path = request.Path.ToLower();
        var method = request.HttpMethod.ToUpper();

        return (path, method) switch
        {
            ("/prescription", "GET") => await GetAllPrescriptions(dbContext),
            _ when path.StartsWith("/prescription/") && method == "GET" && IsGetByIdPattern(path) =>
                await GetPrescriptionById(path, dbContext),
            _ when path.StartsWith("/prescription/patient/") && method == "GET" =>
                await GetPrescriptionsByPatientId(path, dbContext),
            ("/prescription", "POST") => await CreatePrescription(request, dbContext, s3Client),
            _ when path.StartsWith("/prescription/") && method == "PUT" && IsGetByIdPattern(path) =>
                await UpdatePrescription(path, request, dbContext),
            _ when path.StartsWith("/prescription/") && method == "DELETE" && IsGetByIdPattern(path) =>
                await DeletePrescription(path, dbContext),
            _ when path.Contains("/medications") && method == "POST" =>
                await AddMedicationToPrescription(path, request, dbContext),
            _ when path.Contains("/medications/") && method == "DELETE" =>
                await RemoveMedicationFromPrescription(path, dbContext),
            _ when path.Contains("/medications") && method == "GET" =>
                await GetMedicationsForPrescription(path, dbContext),
            (_, "OPTIONS") => CorsResponse(),

            _ => ErrorResponse(404, "Endpoint not found")
        };
    }

    // --- Endpoint Implementations ---

    private async Task<APIGatewayProxyResponse> GetAllPrescriptions(AppDbContext dbContext)
    {
        var prescriptions = await dbContext.Prescriptions
            .Include(p => p.Medications)
            .ToListAsync();

        var result = prescriptions.Select(p => ToOutputDto(p)).ToList();
        return Ok(result);
    }

    private async Task<APIGatewayProxyResponse> GetPrescriptionById(string path, AppDbContext dbContext)
    {
        var id = ExtractId(path, 2);
        var p = await dbContext.Prescriptions
            .Include(x => x.Medications)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (p == null)
            return ErrorResponse(404, "Prescription not found");

        return Ok(ToOutputDto(p));
    }

    private async Task<APIGatewayProxyResponse> GetPrescriptionsByPatientId(string path, AppDbContext dbContext)
    {
        var patientId = ExtractId(path, 3);
        var prescriptions = await dbContext.Prescriptions
            .Include(p => p.Medications)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.Date)
            .ToListAsync();

        var result = prescriptions.Select(p => ToOutputDto(p)).ToList();
        return Ok(result);
    }

    private async Task<APIGatewayProxyResponse> CreatePrescription(
        APIGatewayProxyRequest request, AppDbContext dbContext,IAmazonS3 s3Client)
    {
        if (string.IsNullOrEmpty(request.Body))
        {
            return ErrorResponse(400, "Prescription data is required");
        }

        var dto = JsonSerializer.Deserialize<PrescriptionDto>(
            request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (dto == null)
        {
            return ErrorResponse(400, "Invalid prescription data");
        }

        if (dto.PatientId == 0)
        {
            return ErrorResponse(400, "PatientId is a required field");
        }

        var prescription = new Prescription
        {
            PatientId = dto.PatientId, Date = dto.Date, Notes = dto.Notes, 
            PhysicianId = dto.PhysicianId, CreatedAt = DateTime.UtcNow, Medications = new List<Medication>()
        };

        if (dto.Medications != null)
        {
            foreach (var m in dto.Medications)
            {
                prescription.Medications.Add(new Medication
                {
                    Name = m.Name, Dosage = m.Dosage, Frequency = m.Frequency, Duration = m.Duration, 
                    Notes = m.Notes, StartDate = m.StartDate, EndDate = m.EndDate, 
                    CreatedAt = DateTime.UtcNow, IsActive = true
                });
            }
        }

        dbContext.Prescriptions.Add(prescription);
        await dbContext.SaveChangesAsync();

        string bucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME");
        if (!string.IsNullOrEmpty(bucketName))
        {
            string s3Key = $"prescriptions/{prescription.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            string s3Content = JsonSerializer.Serialize(new
            {
                prescription.Id,
                prescription.PatientId,
                prescription.PhysicianId,
                prescription.Date,
                prescription.Notes,
                prescription.CreatedAt,
                Medications = prescription.Medications.Select(m => new {
                    m.Id,
                    m.Name,
                    m.Dosage,
                    m.Frequency,
                    m.Duration,
                    m.Notes,
                    m.StartDate,
                    m.EndDate,
                    m.IsActive,
                    m.CreatedAt,
                    m.UpdatedAt
                }).ToList()
            });

            var putRequest = new Amazon.S3.Model.PutObjectRequest
            {
                BucketName = bucketName,
                Key = s3Key,
                ContentBody = s3Content
            };

            await s3Client.PutObjectAsync(putRequest);
        }

        var created = await dbContext.Prescriptions
            .Include(p => p.Medications)
            .FirstOrDefaultAsync(p => p.Id == prescription.Id);

        return Created(ToOutputDto(created));
    }

    private async Task<APIGatewayProxyResponse> UpdatePrescription(string path, APIGatewayProxyRequest request, AppDbContext dbContext)
    {
        var id = ExtractId(path, 2);
        var dto = JsonSerializer.Deserialize<PrescriptionDto>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (dto == null)
            return ErrorResponse(400, "Invalid prescription data");

        var existing = await dbContext.Prescriptions
            .Include(p => p.Medications)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (existing == null)
            return ErrorResponse(404, "Prescription not found");

        existing.PatientId = dto.PatientId;
        existing.Date = dto.Date;
        existing.Notes = dto.Notes;
        existing.PhysicianId = dto.PhysicianId;
        existing.UpdatedAt = DateTime.UtcNow;

        dbContext.Medications.RemoveRange(existing.Medications);

        existing.Medications = dto.Medications?.Select(m => new Medication
        {
            Name = m.Name,
            Dosage = m.Dosage,
            Frequency = m.Frequency,
            Duration = m.Duration,
            Notes = m.Notes,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        }).ToList() ?? new List<Medication>();

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    private async Task<APIGatewayProxyResponse> DeletePrescription(string path, AppDbContext dbContext)
    {
        var id = ExtractId(path, 2);
        var prescription = await dbContext.Prescriptions
            .Include(p => p.Medications)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prescription == null)
            return ErrorResponse(404, "Prescription not found");

        dbContext.Medications.RemoveRange(prescription.Medications);
        dbContext.Prescriptions.Remove(prescription);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    private async Task<APIGatewayProxyResponse> AddMedicationToPrescription(string path, APIGatewayProxyRequest request, AppDbContext dbContext)
    {
        var prescriptionId = ExtractId(path, 2);
        var medicationDto = JsonSerializer.Deserialize<MedicationDto>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (medicationDto == null)
            return ErrorResponse(400, "Medication data is required");

        var prescription = await dbContext.Prescriptions
            .Include(p => p.Medications)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId);

        if (prescription == null)
            return ErrorResponse(404, "Prescription not found");

        var medication = new Medication
        {
            Name = medicationDto.Name,
            Dosage = medicationDto.Dosage,
            Frequency = medicationDto.Frequency,
            Duration = medicationDto.Duration,
            Notes = medicationDto.Notes,
            StartDate = medicationDto.StartDate,
            EndDate = medicationDto.EndDate,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            PrescriptionId = prescriptionId
        };

        prescription.Medications.Add(medication);
        await dbContext.SaveChangesAsync();

        return Created(medication);
    }

    private async Task<APIGatewayProxyResponse> RemoveMedicationFromPrescription(string path, AppDbContext dbContext)
    {
        var parts = path.Split('/');
        var prescriptionId = int.Parse(parts[2]);
        var medicationId = int.Parse(parts[4]);

        var prescription = await dbContext.Prescriptions
            .Include(p => p.Medications)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId);

        if (prescription == null)
            return ErrorResponse(404, "Prescription not found");

        var medication = prescription.Medications.FirstOrDefault(m => m.Id == medicationId);

        if (medication == null)
            return ErrorResponse(404, "Medication not found");

        dbContext.Medications.Remove(medication);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    private async Task<APIGatewayProxyResponse> GetMedicationsForPrescription(string path, AppDbContext dbContext)
    {
        var prescriptionId = ExtractId(path, 2);
        var prescription = await dbContext.Prescriptions
            .Include(p => p.Medications)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId);

        if (prescription == null)
            return ErrorResponse(404, "Prescription not found");

        var result = prescription.Medications.Select(m => new MedicationOutputDto
        {
            Id = m.Id,
            PrescriptionId = m.PrescriptionId,
            Name = m.Name,
            Dosage = m.Dosage,
            Frequency = m.Frequency,
            Duration = m.Duration,
            Notes = m.Notes,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            IsActive = m.IsActive,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        }).ToList();

        return Ok(result);
    }

    private int ExtractId(string path, int index)
    {
        var parts = path.Split('/');
        return int.Parse(parts[index]);
    }

    private bool IsGetByIdPattern(string path)
    {
        var parts = path.Split('/');
        return parts.Length == 3 && int.TryParse(parts[2], out _);
    }

    private PrescriptionOutputDto ToOutputDto(Prescription p)
    {
        return new PrescriptionOutputDto
        {
            Id = p.Id,
            PatientId = p.PatientId,
            Date = p.Date,
            Notes = p.Notes,
            PhysicianId = p.PhysicianId,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Medications = p.Medications.Select(m => new MedicationOutputDto
            {
                Id = m.Id,
                PrescriptionId = m.PrescriptionId,
                Name = m.Name,
                Dosage = m.Dosage,
                Frequency = m.Frequency,
                Duration = m.Duration,
                Notes = m.Notes,
                StartDate = m.StartDate,
                EndDate = m.EndDate,
                IsActive = m.IsActive,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).ToList()
        };
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
