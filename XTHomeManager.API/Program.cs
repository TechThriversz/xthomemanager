using Amazon.Runtime;
using Amazon.S3;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using XTHomeManager.API.Data;
using XTHomeManager.API.Services;
using XTHomeManager.API.Jobs;
using XTHomeManager.API.Filters; // ? ADD THIS

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<EmailService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactNativeAndWebOrigins", policy =>
    {
        policy.WithOrigins(
                "https://xthomemanagerfe.vercel.app",
                "https://xthomemanager.vercel.app",
                "http://localhost:5173",
                "http://localhost:8081",
                "https://hmapi.somee.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// AWS R2
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

// JWT
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            NameClaimType = "id"
        };
    });

// HANGFIRE — MUST BE BEFORE app.Build()
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

var app = builder.Build();

// HANGFIRE DASHBOARD
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// SCHEDULE JOB
RecurringJob.AddOrUpdate<DeletionJob>(
    "user-deletion-cleanup",
    job => job.Execute(),
    "*/5 * * * *"
);

// Middleware
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AllowReactNativeAndWebOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();