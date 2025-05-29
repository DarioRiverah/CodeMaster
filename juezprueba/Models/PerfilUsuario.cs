using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace juezprueba.Models
{
    public class PerfilUsuario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        public ApplicationUser Usuario { get; set; }

        public string? ImagenPerfilUrl { get; set; }
        public string? Biografia { get; set; }
        public string? Direccion { get; set; }

        [DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; }
    }
}
