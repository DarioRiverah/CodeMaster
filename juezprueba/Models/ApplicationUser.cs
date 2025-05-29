using Microsoft.AspNetCore.Identity;

namespace juezprueba.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}
