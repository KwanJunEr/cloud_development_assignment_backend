using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.S3;
using Amazon.S3.Model;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ImageUploadService;

public class Function
{

    private readonly IAmazonS3 _s3Client;
    private const string BUCKET_NAME = "diabetecarepublicbucket";

    public Function()
    {
        _s3Client = new AmazonS3Client();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Body))
                return ErrorResponse(400, "Image upload request data is required");

            var uploadRequest = JsonSerializer.Deserialize<ImageUploadRequest>(request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (uploadRequest == null || string.IsNullOrEmpty(uploadRequest.FileName))
                return ErrorResponse(400, "FileName is required");

            var fileExtension = Path.GetExtension(uploadRequest.FileName);
            var uniqueFileName = $"uploads/{Guid.NewGuid()}{fileExtension}";

            var presignedUrlRequest = new GetPreSignedUrlRequest
            {
                BucketName = BUCKET_NAME,
                Key = uniqueFileName,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(15),
                ContentType = uploadRequest.ContentType ?? "image/jpeg"
            };

            var presignedUrl = _s3Client.GetPreSignedURL(presignedUrlRequest);
            var imageUrl = $"https://{BUCKET_NAME}.s3.amazonaws.com/{uniqueFileName}";

            return Ok(new
            {
                presignedUrl,
                imageUrl,
                fileName = uniqueFileName,
                message = "Presigned URL generated successfully"
            });
        }
        catch (Exception ex)
        {
            return ErrorResponse(500, "Error generating presigned URL", ex.Message);
        }
    }

    // --- DTO ---
    public class ImageUploadRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string? ContentType { get; set; }
    }

    // --- Response Helpers ---
    private APIGatewayProxyResponse Ok(object data) => new()
    {
        StatusCode = 200,
        Body = JsonSerializer.Serialize(data),
        Headers = CorsHeaders()
    };

    private APIGatewayProxyResponse ErrorResponse(int statusCode, string message, string? details = null) => new()
    {
        StatusCode = statusCode,
        Body = JsonSerializer.Serialize(new { error = message, details }),
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
