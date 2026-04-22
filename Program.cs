using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Sedziowanie.Data;
using Sedziowanie.Middleware;
using Sedziowanie.Models;
using Sedziowanie.Services;
using Sedziowanie.Services.Interfaces;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddScoped<IMeczService, MeczService>();
builder.Services.AddScoped<INiedyspozycjaService, NiedyspozycjaService>();
builder.Services.AddScoped<IRozgrywkiService, RozgrywkiService>();
builder.Services.AddScoped<ISedziaService, SedziaService>();

var connString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<DBObsadyContext>(options =>
{
    options.UseSqlServer(connString, sql => sql.UseCompatibilityLevel(120));
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<DBObsadyContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 10;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DBObsadyContext>();
    await SeedData.InitializeAsync(scope.ServiceProvider); 
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseMiddleware<ForcePasswordChangeMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Start}/{action=Index}/{id?}");
app.MapRazorPages();
app.Run();

