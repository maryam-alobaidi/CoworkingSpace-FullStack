using CoworkingSpace.BLL.Interfaces;
using CoworkingSpace.BLL.Services;
using CoworkingSpace.DAL;
using CoworkingSpace.Models;
using Scalar.AspNetCore;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// --- 1. تسجيل الخدمات الأساسية (Core Services) ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// --- 2. إعداد قاعدة البيانات (Database Connection) ---
string? connString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connString))
{
    throw new Exception("Connection string 'DefaultConnection' is not found in the configuration.");
}
clsPrimaryFunctions.Initialize(connString);

// --- 3. سياسة الوصول (CORS Policy) ---
// تسمح هذه السياسة لتطبيق Angular بالوصول إلى الـ API من نطاقات مختلفة
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// --- 4. إعدادات الإيميل (Email Infrastructure) ---
// ربط قسم EmailSettings من appsettings.json بموديل الإعدادات
builder.Services.Configure<EmailSettingsModel>(builder.Configuration.GetSection("EmailSettings"));
// تسجيل خدمة الإيميل لتكون متاحة للحقن (Dependency Injection) في الـ Controllers
builder.Services.AddScoped<IEmailService, EmailService>();

var stripeSecretKey=builder.Configuration.GetSection("Stripe:SecretKey").Value;

StripeConfiguration.ApiKey = stripeSecretKey;

var app = builder.Build();

// --- 5. خط أنابيب الطلبات (HTTP Request Pipeline) ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();


app.UseCors("AllowAngular");


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();