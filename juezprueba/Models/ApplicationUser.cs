using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace juezprueba.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Relación 1 a 1
        public PerfilUsuario Perfil { get; set; }
    }
}
