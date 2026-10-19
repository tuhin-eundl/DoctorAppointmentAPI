using DoctorAppointment.Application;
using DoctorAppointment.Infrastructures;
using DoctorAppointment.Service.MCPTools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.SemanticKernel;
using ModelContextProtocol.AspNetCore;
using OpenAI;
using System.Net.Http.Headers;
using System.ClientModel;
using System.Text;
using System.Text.Json.Nodes;
using DoctorAppointment.Service.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Register MCP tools so they are available for DI when building the Kernel
builder.Services.AddTransient<UserTools>();

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
});

// Read AI Settings
var aiApiKey = builder.Configuration["AiSettings:ApiKey"] ?? throw new ArgumentNullException("ApiKey missing");
var aiModelId = builder.Configuration["AiSettings:ModelId"] ?? "meta-llama/llama-3.3-70b-instruct:free";
var aiEndpoint = builder.Configuration["AiSettings:Endpoint"] ?? "https://openrouter.ai/api/v1";

// Configure Semantic Kernel with Custom OpenAI Endpoint
builder.Services.AddTransient<Kernel>(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();

    // 1. Configure custom OpenAI endpoint options
    // Determine the correct base to give the OpenAI client. Some providers
    // (OpenRouter) serve under /api/v1 while the SDK appends /v1 itself. If the
    // configured aiEndpoint contains an /api path, use origin + "/api" so the
    // SDK ends up calling /api/v1. Otherwise use the origin and let the SDK
    // append /v1.
    Uri endpointUri = new Uri(aiEndpoint);
    var origin = new Uri(endpointUri.GetLeftPart(UriPartial.Authority));

    string baseForClient;
    if (endpointUri.AbsolutePath.Contains("/api", StringComparison.OrdinalIgnoreCase))
    {
        baseForClient = new Uri(origin, "api/").ToString().TrimEnd('/'); // https://openrouter.ai/api
    }
    else
    {
        baseForClient = origin.ToString().TrimEnd('/'); // https://openrouter.ai
    }

    var openAiClientOptions = new OpenAIClientOptions
    {
        Endpoint = new Uri(baseForClient)
    };

    // Quick connectivity check: try a few common model endpoints on the chosen
    // base so we can surface a clear error if the remote path differs from the
    // SDK expectations (this often causes 404s with proxy providers).
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("OpenAIEndpointTest");
    using (var http = new HttpClient { BaseAddress = new Uri(baseForClient) })
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aiApiKey);

        var endpointsToTry = new[] { "v1/models", "models" };
        var lastStatus = new List<string>();
        bool ok = false;
        foreach (var path in endpointsToTry)
        {
            try
            {
                var resp = http.GetAsync(path).GetAwaiter().GetResult();
                lastStatus.Add($"{new Uri(http.BaseAddress, path)} -> {(int)resp.StatusCode} {resp.ReasonPhrase}");
                if (resp.IsSuccessStatusCode)
                {
                    logger.LogInformation("OpenAI endpoint check succeeded for {Path}", path);
                    ok = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                lastStatus.Add($"{new Uri(http.BaseAddress, path)} -> EX: {ex.Message}");
            }
        }

        if (!ok)
        {
            logger.LogError("OpenAI endpoint connectivity check failed. Tried: {Tried}.", string.Join(';', lastStatus));
            throw new InvalidOperationException($"OpenAI endpoint did not respond successfully. Tried: {string.Join(';', lastStatus)}");
        }
    }

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
    .WithTools<DoctorScheduleTools>();

var app = builder.Build();

app.UseCors("ExpoClientPolicy");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DoctorAppointment API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();
app.MapMcp("/mcp")
    .RequireAuthorization();

app.Run();