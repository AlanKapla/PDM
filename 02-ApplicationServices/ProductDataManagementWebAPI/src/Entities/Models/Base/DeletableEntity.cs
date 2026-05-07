namespace Entities.Models.Base
{
    public abstract class DeletableEntity : BaseEntity
    {
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
