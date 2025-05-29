using System.ComponentModel.DataAnnotations;

namespace juezprueba.ViewModels
{
    public class EditarPerfilViewModel
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }

        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [Display(Name = "Biografía")]
        public string? Biografia { get; set; }

        [DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; }

        [Display(Name = "Imagen de perfil")]
        public IFormFile? ImagenPerfil { get; set; }

        public string? ImagenActualUrl { get; set; }
    }
}
