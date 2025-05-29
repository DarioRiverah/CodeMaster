using System.ComponentModel.DataAnnotations;

namespace juezprueba.ViewModels
{
    public class ResendEmailConfirmationViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

}
