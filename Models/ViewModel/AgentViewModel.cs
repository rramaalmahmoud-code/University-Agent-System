using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using University_Agent_System.Models.Oracle;

namespace University_Agent_System.Models.ViewModel
{
    public class AgentViewModel
    {
        [Required(ErrorMessage = "Agent Name is required")]
        public string? agentNameEnglish { get; set; }
        [Required(ErrorMessage = "اسم الوكيل مطلوب")]
        public string? agentNameArabic { get; set; }
        public int agentId { get; set; } //Primary Key
        public string? agentStatus { get; set; } //
        [Required(ErrorMessage = "Agent Code Number is required")]
        public int? agentCode { get; set; }
        [Required(ErrorMessage = "ID Number is required")]
        public string? nationalId { get; set; }
        [Required(ErrorMessage = "Nationality is required")]
        public int? nationalityId { get; set; }
        [Required(ErrorMessage = "Country is required")]
        public int? countryId { get; set; }
        public string? city { get; set; }
        [Required(ErrorMessage = "Agent Email is required")]

        public string? agentEmail { get; set; }
        public string? agentIban { get; set; }
        public string? passowrd { get; set; }
        public string? userPassword { get; set; }


        [Required(ErrorMessage = "Please choose country")]
        public string agentPhoneCountryIso2 { get; set; }

        [Required(ErrorMessage = "Please enter phone number")]
        public string agentPhoneNational { get; set; }

        public string agentPhone { get; set; }

        public List<SelectListItem> PhoneCountries { get; set; } = new();
        //[Required(ErrorMessage = "Student Phone is required")]
        //public string? agentPhone { get; set; }
        public string? Nationality { get; set; }
        public string? Country { get; set; }
        public string? notes { get; set; }
        public string? commission { get; set; }
        [Required(ErrorMessage = "contract StartDate  is required")]

        public DateTime? contractStartDate { get; set; }
        [Required(ErrorMessage = "contract EndDate  is required")]
        public DateTime?    contractEndDate { get; set; }
        public string? isActive { get; set; }
        public int? active { get; set; }
        [Required(ErrorMessage = "agent Contract is required")]
        public IFormFile? agentContract { get; set; }
        public string? agentContractPath { get; set; }
        public string? SearchTerm { get; set; }
        // For displaying dropdowns
        public List<AgentVM>? Agents { get; set; } = new();
        public List<nationality>? Nationalities { get; set; }
        public List<country>? Countries { get; set; }



        // Pagination
        public int AgentTotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }


    }
}
