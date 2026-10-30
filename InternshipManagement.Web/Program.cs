using InternshipManagement.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// HTTP Client for API communication
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ClientIpForwardingHandler>();
builder.Services.AddSingleton<TokenRefreshCoordinator>();

builder.Services.AddHttpClient("InternshipApi", client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5003/";
    client.BaseAddress = new Uri(baseUrl);
})
.AddHttpMessageHandler<ClientIpForwardingHandler>();

// Register ALL API Clients
builder.Services.AddScoped<IAuthApiClient, AuthApiClient>();
builder.Services.AddScoped<ITwoFactorApiClient, TwoFactorApiClient>();
builder.Services.AddScoped<IInternshipApiClient, InternshipApiClient>();
builder.Services.AddScoped<IApplicationApiClient, ApplicationApiClient>();
builder.Services.AddScoped<IStudentApiClient, StudentApiClient>();
builder.Services.AddScoped<IDocumentApiClient, DocumentApiClient>();
builder.Services.AddScoped<ICompanyApiClient, CompanyApiClient>();
builder.Services.AddScoped<IAdminApiClient, AdminApiClient>();
builder.Services.AddScoped<IReportApiClient, ReportApiClient>();
builder.Services.AddScoped<IAiApiClient, AiApiClient>();
builder.Services.AddScoped<ILifecycleApiClient, LifecycleApiClient>();
builder.Services.AddSingleton<UploadedFileStorage>();

// Session storage for JWT token
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads")
        && !context.Request.Path.StartsWithSegments("/uploads/profile-pictures")
        && !context.Request.Path.StartsWithSegments("/uploads/company-logos"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseMiddleware<InternshipManagement.Web.Middleware.SessionTokenMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
