using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using University_Agent_System.Data;
using University_Agent_System.Services;

var builder = WebApplication.CreateBuilder(args);
// Increase form options


// Configure localization services
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources"); // Ensure Resources path is correct
builder.Services.AddMvc()
    .AddMvcLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)  // Ensures that views with a culture suffix are picked up.
    .AddDataAnnotationsLocalization();  // Enables data annotations localization (if used)

// DbContext setup (database connection setup as usual)
builder.Services.AddDbContext<UASDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<AcademicService>();
builder.Services.AddScoped<StudentsBySemester>();
builder.Services.AddScoped<AgentHelper>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>();
builder.Services.AddTransient<IDbConnection>(sp =>
    new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwt = builder.Configuration.GetSection("Jwt");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]))
    };


    // 👇 Read token from cookie instead of Authorization header
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        
        
        
        
        
        {
            var token = context.Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddScoped
    <IOracleMajorSyncService, OracleMajorSyncService>();
builder.Services.AddScoped
    <IAdmissionMajorService, AdmissionMajorService>();
builder.Services.AddScoped
    <IAdmissionMajorDiscountService,
     AdmissionMajorDiscountService>();

var app = builder.Build();

// Define supported cultures
var supportedCultures = new[] { "en", "ar" };

// Configure localization options
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

// Apply localization middleware before routing
app.UseRequestLocalization(localizationOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
// Ensure culture is always part of the URL
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{culture=en}/{controller=Home}/{action=Home}/{id?}");
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{culture=en}/{controller=En}/{action=Login}/{id?}");


app.MapControllerRoute(
    name: "default",
    pattern: "{culture=en}/{controller=En}/{action=Login}/{id?}");

app.MapRazorPages();
app.Run();
