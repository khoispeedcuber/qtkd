using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.Middleware;
using QTKD.Api.Repositories;
using QTKD.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 1) Cấu hình: chuỗi kết nối lấy từ User Secrets / biến môi trường (không hard-code mật khẩu).
var connectionString = builder.Configuration.GetConnectionString("QtkdDb")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:QtkdDb. Đặt bằng User Secrets hoặc biến môi trường (xem README.md).");

// 2) Dependency Injection: Controller -> Service -> Repository -> DbContext
builder.Services.AddDbContext<QtkdDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IContractService, ContractService>();

// 3) JSON: giữ nguyên tiếng Việt (không escape thành \uXXXX) cho cả Controller lẫn middleware.
var jsonEncoder = JavaScriptEncoder.Create(UnicodeRanges.All);
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Encoder = jsonEncoder);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Encoder = jsonEncoder);

// 4) Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware xử lý lỗi đặt ĐẦU TIÊN để bao bọc toàn bộ pipeline phía sau.
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Dữ liệu mẫu (chỉ Development, chỉ khi database đã migrate).
    await DbSeeder.SeedAsync(app.Services);
}

app.MapControllers();
app.Run();
