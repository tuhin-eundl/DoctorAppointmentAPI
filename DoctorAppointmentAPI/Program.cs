using DoctorAppointment.Application;
using DoctorAppointment.Infrastructures;
using DoctorAppointment.Service.MCPTools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.SemanticKernel;
using ModelContextProtocol.AspNetCore;
using OpenAI;
using System.ClientModel;
using System.Text;
using System.Text.Json.Nodes;
using DoctorAppointment.Service.Middlewares;
using DoctorAppointment.Service.Hubs;
using DoctorAppointment.Application.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Register MCP tools so they are available for DI when building the Kernel
builder.Services.AddTransient<UserTools>();
builder.Services.AddTransient<AppointmentTools>();
builder.Services.AddTransient<DoctorScheduleTools>();
builder.Services.AddTransient<NotificationTools>();
builder.Services.AddTransient<HospitalTools>();

builder.Services.AddHttpClient("DoctorAppointmentApi", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException(
            "Api:BaseUrl is not configured"));
});

var keyBytes = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty);
if (keyBytes.Length == 0)
{
    throw new InvalidOperationException("Jwt:Key is not configured");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };
    // Browsers can't set headers on a WebSocket, so the SignalR client sends the token in the query string.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments(NotificationHub.Path))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

// Read AI Settings
var aiApiKey = builder.Configuration["AiSettings:ApiKey"] ?? throw new ArgumentNullException("ApiKey missing");
var aiEndpoint = builder.Configuration["AiSettings:Endpoint"] ?? "https://openrouter.ai/api/v1";
var aiModelId = builder.Configuration["AiSettings:ModelId"]
    ?? throw new InvalidOperationException("AiSettings:ModelId is not configured");

// Configure Semantic Kernel with Custom OpenAI Endpoint
builder.Services.AddTransient<Kernel>(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();

    // 1. Configure custom OpenAI endpoint options
    // The OpenAI SDK uses the endpoint as the full base URL and only appends
    // "/chat/completions", so it must include the version path
    // (e.g. https://openrouter.ai/api/v1).
    var openAiClientOptions = new OpenAIClientOptions
    {
        Endpoint = new Uri(aiEndpoint)
    };

    // 2. Create the OpenAI client pointing to the custom endpoint
    var openAiClient = new OpenAIClient(new ApiKeyCredential(aiApiKey), openAiClientOptions);

    // 3. Register Chat Completion in Semantic Kernel using the configured client
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: aiModelId,
        openAIClient: openAiClient
    );

    // Register your tool plugin
    var userTools = sp.GetRequiredService<UserTools>();
    kernelBuilder.Plugins.AddFromObject(userTools, pluginName: "UserManagement");

    var appointmentTools = sp.GetRequiredService<AppointmentTools>();
    kernelBuilder.Plugins.AddFromObject(appointmentTools, pluginName: "Appointments");

    var doctorScheduleTools = sp.GetRequiredService<DoctorScheduleTools>();
    kernelBuilder.Plugins.AddFromObject(doctorScheduleTools, pluginName: "DoctorSchedules");
    kernelBuilder.Plugins.AddFromObject(sp.GetRequiredService<NotificationTools>(), pluginName: "Notifications");
    kernelBuilder.Plugins.AddFromObject(sp.GetRequiredService<HospitalTools>(), pluginName: "Hospitals");

    return kernelBuilder.Build();
});

// Configure CORS for Expo / Android Emulator development
builder.Services.AddCors(options =>
{
    options.AddPolicy("ExpoClientPolicy", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<INotificationPublisher, SignalRNotificationPublisher>();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DoctorAppointment API", Version = "v1" });
    c.MapType<TimeOnly>(() => new OpenApiSchema
    {
        Type = JsonSchemaType.String,
        Format = "time",
        Description = "Local time in HH:mm:ss format, without a timezone suffix.",
        Examples = [JsonValue.Create("09:00:00")]
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n" +
                      "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                      "Example: 'Bearer eyJhbGciOiJIUzI1Ni...'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document), [] }
    });
    c.EnableAnnotations();
});

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.SessionMode = HttpServerSessionMode.Stateless;
    })
    .WithTools<UserTools>()
    .WithTools<DoctorScheduleTools>()
    .WithTools<AppointmentTools>()
    .WithTools<NotificationTools>()
    .WithTools<HospitalTools>();

var app = builder.Build();

app.UseCors("ExpoClientPolicy");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DoctorAppointment API v1");
    c.RoutePrefix = "swagger";
});

// In Development the clients talk to the API over plain HTTP on localhost. Redirecting to HTTPS sends
// the browser to a different origin (port 7178), which drops the Authorization header and returns 401.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();
app.MapHub<NotificationHub>(NotificationHub.Path);
app.MapMcp("/mcp")
    .RequireAuthorization();

app.Run();