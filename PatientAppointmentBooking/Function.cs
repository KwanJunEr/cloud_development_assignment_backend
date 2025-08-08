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

namespace PatientAppointmentBooking;

public class Function
{
    private readonly IServiceProvider _serviceProvider;
    private const string SNS_TOPIC_ARN = "arn:aws:sns:us-east-1:306656959951:PatientAppointmentBooking";

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

        // Get Dieticians
        if (path == "/dietician" && method == "GET")
            return await GetDieticians(dbContext, context);

        // Get Doctors
        if (path == "/doctors" && method == "GET")
            return await GetDoctors(dbContext, context);

        // Get Doctor Availability
        if (path.StartsWith("/doctor-availability/") && method == "GET")
            return await GetProviderAvailability(path, dbContext, context);

        // Get Dietician Availability
        if (path.StartsWith("/dietician-availability/") && method == "GET")
            return await GetDieticianAvailability(path, dbContext, context);

        // Create Appointment
        if (path == "/create" && method == "POST")
            return await CreateAppointment(request, dbContext, snsClient, context);

        // Get Appointments by Patient
        if (path.StartsWith("/getappointment/") && method == "GET")
            return await GetAppointments(path, dbContext, context);

        // Get All Appointments by Patient
        if (path.StartsWith("/getallappointment/") && method == "GET")
            return await GetAllAppointments(path, dbContext, context);

        // Complete Appointment Status
        if (path.StartsWith("/completestatus/") && method == "PUT")
            return await CompleteAppointmentStatus(path, dbContext, snsClient, context);

        // Cancel Appointment Status
        if (path.StartsWith("/cancelstatus/") && method == "PUT")
            return await CancelAppointmentStatus(path, dbContext, snsClient, context);

        // Get All Dietician Appointments
        if (path.StartsWith("/getalldieticianappointment/") && method == "GET")
            return await GetAllDieticianAppointments(path, dbContext, context);

        // Get All Appointments for Family
        if (path.StartsWith("/getallappointmentforfamily/") && method == "GET")
            return await GetAllAppointmentForFamily(path, dbContext, context);

        if (method == "OPTIONS")
            return CorsResponse();

        return ErrorResponse(404, "Endpoint not found");
    }

    private async Task<APIGatewayProxyResponse> GetDieticians(AppDbContext dbContext, ILambdaContext context)
    {
        try
        {
            var dieticians = await dbContext.Users
                .Where(u => u.Role.ToLower() == "dietician")
                .ToListAsync();

            return Ok(new
            {
                message = "Successfully retrieved all dieticians",
                dieticians
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"GetDieticians Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while retrieving dieticians", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> GetDoctors(AppDbContext dbContext, ILambdaContext context)
    {
        try
        {
            var doctors = await dbContext.Users
                .Where(u => u.Role.ToLower() == "doctor")
                .ToListAsync();

            return Ok(new
            {
                message = "Successfully retrieved all doctors",
                doctors
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"GetDoctors Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while retrieving doctors", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> GetProviderAvailability(string path, AppDbContext dbContext, ILambdaContext context)
    {
        try
        {
            var providerId = ExtractId(path, 2);

            var availability = await dbContext.ProviderAvailabilities
                .Where(a =>
                    a.ProviderId == providerId &&
                    a.AvailabilityDate >= DateTime.Today &&
                    a.Status == "Available")
                .OrderBy(a => a.AvailabilityDate)
                .Select(a => new
                {
                    date = a.AvailabilityDate.ToString("yyyy-MM-dd"),
                    timeRange = $"{a.StartTime:hh\\:mm} - {a.EndTime:hh\\:mm}",
                    notes = a.Notes
                })
                .ToListAsync();

            return Ok(new
            {
                message = "Successfully retrieved available provider slots",
                availability
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"GetProviderAvailability Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while retrieving provider availability", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> GetDieticianAvailability(string path, AppDbContext dbContext, ILambdaContext context)
    {
        try
        {
            var providerId = ExtractId(path, 2);

            var availability = await dbContext.ProviderAvailabilities
                .Where(a =>
                    a.ProviderId == providerId &&
                    a.AvailabilityDate >= DateTime.Today &&
                    a.Status == "Available")
                .OrderBy(a => a.AvailabilityDate)
                .Select(a => new
                {
                    date = a.AvailabilityDate.ToString("yyyy-MM-dd"),
                    timeRange = $"{a.StartTime:hh\\:mm} - {a.EndTime:hh\\:mm}",
                    notes = a.Notes
                })
                .ToListAsync();

            return Ok(new
            {
                message = "Successfully retrieved available provider slots",
                availability
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"GetDieticianAvailability Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while retrieving provider availability", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> CreateAppointment(APIGatewayProxyRequest request,
        AppDbContext dbContext,
        IAmazonSimpleNotificationService snsClient,
        ILambdaContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Body))
                return ErrorResponse(400, "Appointment data is required");

            var dto = JsonSerializer.Deserialize<PatientAppointmentBookingDto>(request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null)
                return ErrorResponse(400, "Invalid appointment data");

            // Split and parse the time range 
        
            // Check provider availability for this date and time slot
            var providerAvailability = await dbContext.ProviderAvailabilities
                .FirstOrDefaultAsync(p =>
                    p.ProviderId == dto.ProviderID &&
                    p.AvailabilityDate == dto.ProviderAvailableDate &&
                    p.StartTime == startTime &&
                    p.EndTime == endTime);

            if (providerAvailability == null)
            {
                return ErrorResponse(400, "Provider availability not found for the selected date and time slot.");
            }

            if (providerAvailability.Status.Equals("taken", StringComparison.OrdinalIgnoreCase))
            {
                return ErrorResponse(400, "The selected time slot is already taken.");
            }

            // Create appointment - CHANGED: Using Appointment instead of PatientAppointmentBooking
            var appointment = new Appointment
            {
                PatientID = dto.PatientID,
                ProviderID = dto.ProviderID,
                Role = dto.Role,
                ProviderName = dto.ProviderName,
                ProviderSpecialization = dto.ProviderSpecialization,
                ProviderVenue = dto.ProviderVenue,
                ProviderAvailableDate = dto.ProviderAvailableDate,
                ProviderAvailableTimeSlot = dto.ProviderAvailableTimeSlot,
                BookingMode = dto.BookingMode,
                ServiceBooked = dto.ServiceBooked,
                ReasonsForVisit = dto.ReasonsForVisit,
                Status = dto.Status
            };

            // CHANGED: Using Appointments instead of PatientAppointmentBooking
            dbContext.Appointments.Add(appointment);

            // Update availability status to "taken"
            providerAvailability.Status = "taken";

            await dbContext.SaveChangesAsync();

            // Create notification using PhysicianNotificationService
            var notificationService = new PhysicianNotificationService(dbContext);
            notificationService.CreateNotification(
                dto.ProviderID,
                $"A new appointment has been booked by patient (ID: {dto.PatientID}) for {dto.ProviderAvailableDate:yyyy-MM-dd} at {dto.ProviderAvailableTimeSlot}.",
                "appointment",
                sender: dto.PatientID.ToString(),
                subject: "New Appointment Booking"
            );

            // Send SNS notification
            await SendAppointmentNotification(appointment, dbContext, snsClient, context, "created");

            return Ok(appointment);
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"CreateAppointment Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while creating the appointment", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> GetAppointments(string path, AppDbContext dbContext, ILambdaContext context)
    {
        try
        {
            var patientId = ExtractId(path, 2);

            // CHANGED: Using Appointments instead of PatientAppointmentBooking
            var appointments = await dbContext.Appointments
                .Where(a =>
                    a.Status.ToLower() == "confirmed" &&
                    a.PatientID == patientId)
                .OrderByDescending(a => a.ProviderAvailableDate)
                .ToListAsync();

            return Ok(new
            {
                message = "Successfully retrieved confirmed appointments.",
                appointments
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"GetAppointments Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while retrieving appointments", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> GetAllAppointments(string path, AppDbContext dbContext, ILambdaContext context)
    {
        try
        {
            var patientId = ExtractId(path, 2);

            // CHANGED: Using Appointments instead of PatientAppointmentBooking
            var appointments = await dbContext.Appointments
                .Where(a => a.PatientID == patientId)
                .OrderByDescending(a => a.ProviderAvailableDate)
                .ToListAsync();

            return Ok(new
            {
                message = "Successfully retrieved all appointments.",
                appointments
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"GetAllAppointments Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while retrieving appointments", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> CompleteAppointmentStatus(string path,
        AppDbContext dbContext,
        IAmazonSimpleNotificationService snsClient,
        ILambdaContext context)
    {
        try
        {
            var appointmentId = ExtractId(path, 2);

            // CHANGED: Using Appointments instead of PatientAppointmentBooking
            var appointment = await dbContext.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return ErrorResponse(404, "Appointment not found.");
            }

            // Update appointment status to "completed"
            appointment.Status = "completed";

            // Parse the time slot to get start and end time
            var timeParts = appointment.ProviderAvailableTimeSlot.Split('-');
            if (timeParts.Length != 2)
            {
                return ErrorResponse(400, "Invalid time slot format in appointment.");
            }

            if (!TimeSpan.TryParse(timeParts[0].Trim(), out var startTime))
            {
                return ErrorResponse(400, "Invalid start time format.");
            }

            if (!TimeSpan.TryParse(timeParts[1].Trim(), out var endTime))
            {
                return ErrorResponse(400, "Invalid end time format.");
            }

            // Find the related provider availability record
            var providerAvailability = await dbContext.ProviderAvailabilities
                .FirstOrDefaultAsync(p =>
                    p.ProviderId == appointment.ProviderID &&
                    p.AvailabilityDate == appointment.ProviderAvailableDate &&
                    p.StartTime == startTime &&
                    p.EndTime == endTime);

            if (providerAvailability != null)
            {
                providerAvailability.Status = "completed";
            }

            await dbContext.SaveChangesAsync();

            // Send SNS notification
            await SendAppointmentNotification(appointment, dbContext, snsClient, context, "completed");

            return Ok(new
            {
                message = "Appointment and provider availability status updated to completed.",
                appointment
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"CompleteAppointmentStatus Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while completing the appointment status", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> CancelAppointmentStatus(string path,
        AppDbContext dbContext,
        IAmazonSimpleNotificationService snsClient,
        ILambdaContext context)
    {
        try
        {
            var appointmentId = ExtractId(path, 2);

            // CHANGED: Using Appointments instead of PatientAppointmentBooking
            var appointment = await dbContext.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return ErrorResponse(404, "Appointment not found.");
            }

            // Update appointment status to "cancelled"
            appointment.Status = "cancelled";

            // Parse the time slot to get start and end time
            var timeParts = appointment.ProviderAvailableTimeSlot.Split('-');
            if (timeParts.Length != 2)
            {
                return ErrorResponse(400, "Invalid time slot format in appointment.");
            }

            if (!TimeSpan.TryParse(timeParts[0].Trim(), out var startTime))
            {
                return ErrorResponse(400, "Invalid start time format.");
            }

            if (!TimeSpan.TryParse(timeParts[1].Trim(), out var endTime))
            {
                return ErrorResponse(400, "Invalid end time format.");
            }

            // Find the related provider availability record
            var providerAvailability = await dbContext.ProviderAvailabilities
                .FirstOrDefaultAsync(p =>
                    p.ProviderId == appointment.ProviderID &&
                    p.AvailabilityDate == appointment.ProviderAvailableDate &&
                    p.StartTime == startTime &&
                    p.EndTime == endTime);

            if (providerAvailability != null)
            {
                providerAvailability.Status = "available";
            }

            await dbContext.SaveChangesAsync();

            // Send SNS notification
            await SendAppointmentNotification(appointment, dbContext, snsClient, context, "cancelled");

            return Ok(new
            {
                message = "Appointment status updated to cancelled and provider availability set to available.",
                appointment
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"CancelAppointmentStatus Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while cancelling the appointment", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> GetAllDieticianAppointments(string path, AppDbContext dbContext, ILambdaContext context)
    {
        try
        {
            var dieticianId = ExtractId(path, 2);

            // CHANGED: Using Appointments instead of PatientAppointmentBooking
            var appointments = await dbContext.Appointments
                .Where(a => a.ProviderID == dieticianId)
                .OrderByDescending(a => a.ProviderAvailableDate)
                .ToListAsync();

            return Ok(new
            {
                message = "Successfully retrieved dietician appointments.",
                appointments
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"GetAllDieticianAppointments Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while retrieving appointments", ex.Message);
        }
    }

    private async Task<APIGatewayProxyResponse> GetAllAppointmentForFamily(string path, AppDbContext dbContext, ILambdaContext context)
    {
        try
        {
            var familyId = ExtractId(path, 2);

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == familyId);
            if (user == null)
            {
                return ErrorResponse(404, "No user found for the specified FamilyId.");
            }

            if (user.PatientId == null)
            {
                return ErrorResponse(400, "This user does not have an associated PatientId.");
            }

            // CHANGED: Using Appointments instead of PatientAppointmentBooking
            var appointments = await dbContext.Appointments
                .Where(a => a.PatientID == user.PatientId && a.Status == "confirmed")
                .OrderByDescending(a => a.ProviderAvailableDate)
                .ToListAsync();

            return Ok(new
            {
                message = "Successfully retrieved appointments for the family.",
                appointments
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"GetAllAppointmentForFamily Error: {ex.Message}");
            return ErrorResponse(500, "An error occurred while retrieving appointments", ex.Message);
        }
    }

    // CHANGED: Parameter type from PatientAppointmentBooking to Appointment
    private async Task SendAppointmentNotification(Appointment appointment,
        AppDbContext dbContext,
        IAmazonSimpleNotificationService snsClient,
        ILambdaContext context,
        string action)
    {
        try
        {
            // Get patient information
            var patient = await dbContext.Users.FindAsync(appointment.PatientID);
            var patientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : $"Patient {appointment.PatientID}";

            // Get provider information
            var provider = await dbContext.Users.FindAsync(appointment.ProviderID);
            var providerName = provider != null ? $"{provider.FirstName} {provider.LastName}" : appointment.ProviderName;

            var actionText = action switch
            {
                "created" => "Booked",
                "completed" => "Completed",
                "cancelled" => "Cancelled",
                _ => "Updated"
            };

            var message = $@"
Appointment {actionText}

Patient: {patientName} (ID: {appointment.PatientID})
Provider: {providerName} ({appointment.Role})
Specialization: {appointment.ProviderSpecialization ?? "Not specified"}
Venue: {appointment.ProviderVenue ?? "Not specified"}
Date: {appointment.ProviderAvailableDate:yyyy-MM-dd}
Time Slot: {appointment.ProviderAvailableTimeSlot}
Service: {appointment.ServiceBooked ?? "Not specified"}
Booking Mode: {appointment.BookingMode ?? "Not specified"}
Reason for Visit: {appointment.ReasonsForVisit ?? "Not specified"}
Status: {appointment.Status}

This is an automated notification from the Diabetes Care System.
            ".Trim();

            var publishRequest = new PublishRequest
            {
                TopicArn = SNS_TOPIC_ARN,
                Message = message,
                Subject = $"Appointment {actionText} - {patientName}"
            };

            await snsClient.PublishAsync(publishRequest);
            context.Logger.LogInformation($"SNS notification sent successfully for appointment {action}");
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the appointment operation
            context.Logger.LogError($"Error sending SNS notification: {ex.Message}");
        }
    }

    private int ExtractId(string path, int index)
    {
        var parts = path.Split('/');
        return int.Parse(parts[index]);
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