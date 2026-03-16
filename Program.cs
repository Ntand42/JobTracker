using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;

using JobTracker.Models;

using Microsoft.AspNetCore.Identity.UI.Services;
using JobTracker.Services;

var builder = WebApplication.CreateBuilder(args);


// Connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ✅ Identity (REGISTERED ONCE)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    const string superUserRoleName = "SuperUser";
    if (!await roleManager.RoleExistsAsync(superUserRoleName))
    {
        await roleManager.CreateAsync(new IdentityRole(superUserRoleName));
    }

    var superUserEmail = builder.Configuration["SuperUser:Email"];
    var superUserPassword = builder.Configuration["SuperUser:Password"];

    if (!string.IsNullOrWhiteSpace(superUserEmail))
    {
        var user = await userManager.FindByEmailAsync(superUserEmail);

        if (user == null && !string.IsNullOrWhiteSpace(superUserPassword))
        {
            user = new ApplicationUser
            {
                UserName = superUserEmail,
                Email = superUserEmail,
                FirstName = "Super",
                LastName = "User",
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, superUserPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));
                Console.WriteLine($"[SuperUser Seed] Failed to create superuser: {errors}");
                user = null;
            }
        }

        if (user != null && !await userManager.IsInRoleAsync(user, superUserRoleName))
        {
            await userManager.AddToRoleAsync(user, superUserRoleName);
        }
    }
}

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ REQUIRED for Identity
app.UseAuthentication();
app.UseAuthorization();

// Default route → Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapRazorPages();

app.Run();
