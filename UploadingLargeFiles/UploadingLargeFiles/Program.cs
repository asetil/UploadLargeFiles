using UploadingLargeFiles.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    //serverOptions.Limits.MaxRequestBodySize = long.MaxValue;
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()  // Allow any origin
                  .AllowAnyHeader()  // Allow any headers
                  .AllowAnyMethod(); // Allow any HTTP method (GET, POST, etc.)
        });
});

builder.Services.AddScoped<IFileService, FileService>();

var app = builder.Build();

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }