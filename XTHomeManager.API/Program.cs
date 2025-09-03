using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using XTHomeManager.API.Data;
using XTHomeManager.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<EmailService>();

// Configure CORS for React Native and Web
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactNativeAndWebOrigins", builder =>
    {
        builder.WithOrigins(
                "https://xthomemanagerfe.vercel.app", // Existing web frontend
                "http://localhost:5173",             // Existing web dev server
                "http://localhost:8081",            // React Native Metro bundler
                "http://192.168.1.0/24"             // Allow local network range (adjust as needed)
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowedToAllowWildcardSubdomains(); // Allows subdomains if needed
    });
});

// Configure AWS services for Cloudflare R2
builder.Services.AddSingleton<AmazonS3Client>(sp =>
{
    var config = new AmazonS3Config
    {
        ServiceURL = "https://1264ab1158e680e14e1634cfd0f3d033.r2.cloudflarestorage.com",
        ForcePathStyle = true
    };

    var credentials = new BasicAWSCredentials(
        builder.Configuration["AWS:AccessKey"],
        builder.Configuration["AWS:SecretKey"]
    );

    return new AmazonS3Client(credentials, config);
});

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactNativeAndWebOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();