using System.ComponentModel.DataAnnotations;

namespace University_Agent_System.Models.ViewModel
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public string? UserTypeName { get; set; }
    }
}
