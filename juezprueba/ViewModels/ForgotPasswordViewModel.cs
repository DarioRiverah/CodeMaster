using System.ComponentModel.DataAnnotations;

namespace juezprueba.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

}
