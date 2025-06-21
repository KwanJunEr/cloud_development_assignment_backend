var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add CORS to allow your Next.js frontend to connect
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJsFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // Change this to your Next.js app URL
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowNextJsFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
