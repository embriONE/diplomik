using AutoMapper;
using Board.Application.AppData.Contexts.Adverts.Repositories;
using Board.Application.AppData.Contexts.Adverts.Services;
using Board.Application.AppData.Contexts.Categories.Repositories;
using Board.Application.AppData.Contexts.Categories.Services;
using Board.Application.AppData.Contexts.Files.Repositories;
using Board.Application.AppData.Contexts.Files.Services;
using Board.Application.AppData.Services;
using Board.Contracts.Advert;
using Board.Contracts.Interfaces;
using Board.Infrastucture.DataAccess;
using Board.Infrastucture.DataAccess.Contexts.Account.Repository;
using Board.Infrastucture.DataAccess.Contexts.Advert.Repository;
using Board.Infrastucture.DataAccess.Contexts.Category.Repository;
using Board.Infrastucture.DataAccess.Contexts.Files.Repository;
using Board.Infrastucture.DataAccess.Interfaces;
using Board.Infrastucture.MapProfiles;
using Board.Infrastucture.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Добавляем DbContext
builder.Services.AddSingleton<IDbContextOptionsConfigurator<BoardDbContext>, BoardDbContextConfiguration>();
        
builder.Services.AddDbContext<BoardDbContext>((Action<IServiceProvider, DbContextOptionsBuilder>)
    ((sp, dbOptions) => sp.GetRequiredService<IDbContextOptionsConfigurator<BoardDbContext>>()
        .Configure((DbContextOptionsBuilder<BoardDbContext>)dbOptions)));

builder.Services.AddScoped((Func<IServiceProvider, DbContext>) (sp => sp.GetRequiredService<BoardDbContext>()));

// Add repositories to the container.
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IAdvertRepository, AdvertRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IAdvertRepository, AdvertRepository>();

// Add services to the container.
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAdvertService, AdvertService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAdvertService, AdvertService>();
builder.Services.AddScoped<IForbiddenWordsService, ForbiddenWordsService>();

builder.Services.AddSingleton<IMapper>(new Mapper(GetMapperConfiguration()));

builder.Services.AddControllers();

builder.Services.AddCors();

#region Authentication & Authorization

builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(
    options =>
    {
        var secretKey = builder.Configuration["Jwt:Key"];

        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateActor = false,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

#endregion

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Roadly", Version = "V1" });
    options.IncludeXmlComments(Path.Combine(Path.Combine(AppContext.BaseDirectory,
        $"{typeof(CreateAdvertDto).Assembly.GetName().Name}.xml")));
    options.IncludeXmlComments(Path.Combine(Path.Combine(AppContext.BaseDirectory, "Documentation.xml")));

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.  
                        Enter 'Bearer' [space] and then your token in the text input below.
                        Example: 'Bearer secretKey'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = JwtBearerDefaults.AuthenticationScheme
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            { 
                Reference = new OpenApiReference
                { 
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme="oauth2",
                Name= "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowed(origin => true) // allow any origin
    .AllowCredentials());

app.UseHsts();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static MapperConfiguration GetMapperConfiguration()
{
    var configuration = new MapperConfiguration(cfg => 
    {
        cfg.AddProfile<CategoryProfile>();
        cfg.AddProfile<AdvertProfile>();
        cfg.AddProfile<FileProfile>();
    });
    configuration.AssertConfigurationIsValid();
    return configuration;
}

public partial class Program {}