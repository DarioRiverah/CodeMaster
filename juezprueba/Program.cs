using juezprueba.Context;
using juezprueba.Models;
using juezprueba.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

    options.Events.OnRemoteFailure = context =>
    {
        context.Response.Redirect("/Auth/ExternalLoginCallback?remoteError=" + Uri.EscapeDataString(context.Failure.Message));
        context.HandleResponse(); // Evita que se lance una excepción
        return Task.CompletedTask;
    };
});

// Enviar emails:
builder.Services.AddTransient<IEmailSender, EmailSender>();

// 1. Configurar Identity con soporte de roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// 2. Configurar la conexión a la base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Connection")));

// Configuración de servicios
builder.Services.AddHttpClient("ImgBB", client => {
    client.Timeout = TimeSpan.FromSeconds(15);
    client.BaseAddress = new Uri("https://api.imgbb.com/");
});

builder.Services.AddMemoryCache();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationClaimsPrincipalFactory>();

// 4. Agregar servicios MVC y HttpClient
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<Judge0Service>();

var app = builder.Build();

// 5. Crear roles y asignar usuario admin
CreateRolesAndAdminUser(app.Services).GetAwaiter().GetResult();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

async Task CreateRolesAndAdminUser(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "Usuario" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var adminEmail = "admin1@gmail.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            Nombre = "Admin",
            Apellido = "User",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123*");
        if (!result.Succeeded)
        {
            throw new Exception("Error creando usuario Admin: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
