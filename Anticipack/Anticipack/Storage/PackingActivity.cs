using Anticipack.Packing;
using SQLite;

namespace Anticipack.Storage
{
    public class PackingActivity
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public DateTime LastPacked { get; set; }
        public PackingCategory Category { get; set; }
        public int RunCount { get; set; }

        [Ignore] // Not mapped, for convenience
        public IReadOnlyCollection<PackingItem>? Items { get; set; }
    }
}