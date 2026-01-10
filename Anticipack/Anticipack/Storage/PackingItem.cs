using SQLite;

namespace Anticipack.Storage
{
    public class PackingItem
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ActivityId { get; set; } = string.Empty; // Foreign key to PackingActivity

        public string Name { get; set; } = string.Empty;
        public bool IsPacked { get; set; }
        public string Category { get; set; }
        public string Notes { get; set; }
        public int SortOrder { get; set; } // Order of the item within its category
    }
}
