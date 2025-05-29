using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using juezprueba.Models;
using juezprueba.Context;
using Microsoft.EntityFrameworkCore; // Asegúrate de tener este using para acceder al DbContext

public class ApplicationClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context; // Agregamos el DbContext

    public ApplicationClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> optionsAccessor,
        AppDbContext context) // Inyectamos el DbContext
        : base(userManager, optionsAccessor)
    {
        _userManager = userManager;
        _context = context;
    }

protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
{
    var identity = await base.GenerateClaimsAsync(user);
    
    // Obtener perfil del usuario
    var perfil = await _context.PerfilesUsuario
        .FirstOrDefaultAsync(p => p.ApplicationUserId == user.Id);

    // Agregar claims personalizados
    identity.AddClaim(new Claim("Nombre", user.Nombre ?? ""));
    identity.AddClaim(new Claim("Apellido", user.Apellido ?? ""));
    identity.AddClaim(new Claim("ImagenPerfilUrl", 
        perfil?.ImagenPerfilUrl ?? "https://cdn.pixabay.com/photo/2015/10/05/22/37/blank-profile-picture-973460_960_720.png"));

    // Agregar roles
    var roles = await _userManager.GetRolesAsync(user);
    foreach (var role in roles)
    {
        identity.AddClaim(new Claim(ClaimTypes.Role, role));
    }

    return identity;
}
}