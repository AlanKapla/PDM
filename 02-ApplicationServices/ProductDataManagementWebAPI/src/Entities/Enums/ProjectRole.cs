using System.ComponentModel.DataAnnotations;

namespace Entities.Enums
{
    public enum ProjectRole
    {
        [Display(Name = "Administrator")]
        Admin = 0,
        
        [Display(Name = "Członek")]
        Member = 1
    }
}
