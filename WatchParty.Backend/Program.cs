using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using StackExchange.Redis;
using WatchParty.Backend.Configuration;
using WatchParty.Backend.Hubs;
using WatchParty.Backend.Repositories;
using WatchParty.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

var configPath = Path.Combine(builder.Environment.ContentRootPath, "config.yaml");
if (File.Exists(configPath))
{
    var yaml = File.ReadAllText(configPath);
    var config = YamlDotNet.Serialization.Deserializer().Deserialize<Dictionary<string, object>>(yaml);
    
    if (config.TryGetValue("jwt", out var jwtSection) && jwtSection is Dictionary<object, object> jwt)
    {
        builder.Configuration["Jwt:Secret"] = jwt.GetValueOrDefault("secret", "")?.ToString();
        builder.Configuration["Jwt:Issuer"] = jwt.GetValueOrDefault("issuer", "WatchParty")?.ToString();
        builder.Configuration["Jwt:Audience"] = jwt.GetValueOrDefault("audience", "WatchParty")?.ToString();
        builder.Configuration["Jwt:ExpirationDays"] = jwt.GetValueOrDefault("expirationDays", "7")?.ToString();
    }
    
    if (config.TryGetValue("mongodb", out var mongoSection) && mongoSection is Dictionary<object, object> mongo)
    {
        builder.Configuration["MongoDB:ConnectionString"] = mongo.GetValueOrDefault("connectionString", "")?.ToString();
        builder.Configuration["MongoDB:DatabaseName"] = mongo.GetValueOrDefault("databaseName", "watchparty")?.ToString();
    }
    
    if (config.TryGetValue("redis", out var redisSection) && redisSection is Dictionary<object, object> redis)
    {
        builder.Configuration["Redis:ConnectionString"] = redis.GetValueOrDefault("connectionString", "")?.ToString();
    }
}

var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"] 
    ?? Environment.GetEnvironmentVariable("MONGO_URI") 
    ?? "mongodb://localhost:27017";
var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"] 
    ?? "watchparty";

var redisConnectionString = builder.Configuration["Redis:ConnectionString"] 
    ?? Environment.GetEnvironmentVariable("REDIS_URL") 
    ?? "localhost:6379";

var jwtSecret = builder.Configuration["Jwt:Secret"] 
    ?? Environment.GetEnvironmentVariable("JWT_SECRET") 
    ?? "YourSuperSecretKeyThatShouldBeAtLeast32Characters!";

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WatchParty";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WatchParty";
var jwtExpirationDays = int.Parse(builder.Configuration["Jwt:ExpirationDays"] ?? "7");

builder.Services.AddSingleton<MongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(sp => sp.GetRequiredService<MongoClient>().GetDatabase(mongoDatabaseName));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => 
{
    try
    {
        return ConnectionMultiplexer.Connect(redisConnectionString);
    }
    catch
    {
        Console.WriteLine("Warning: Redis connection failed. Using in-memory fallback.");
        return null!;
    }
});

var jwtSettings = new JwtSettings
{
    Secret = jwtSecret,
    Issuer = jwtIssuer,
    Audience = jwtAudience,
    ExpirationDays = jwtExpirationDays
};
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IVoteRepository, VoteRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IRedisService, RedisService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5500",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:5500"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WatchParty API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<WatchPartyHub>("/hubs/watchparty");

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
