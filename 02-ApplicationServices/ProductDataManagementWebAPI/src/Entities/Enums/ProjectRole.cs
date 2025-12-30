using System.ComponentModel.DataAnnotations;

namespace Entities.Enums
{
    public enum ProjectRole
    {
        [Display(Name = "Administrator")]
        Admin = 0,

        [Display(Name = "Edytor")]
        Editor = 1,

        [Display(Name = "Przeglądający")]
        Viewer = 2,

        [Display(Name = "Członek")]
        Member = 3
    }
}
