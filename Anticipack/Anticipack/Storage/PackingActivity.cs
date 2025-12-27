using SQLite;

namespace Anticipack.Storage
{
    public class PackingActivity
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public DateTime LastPacked { get; set; }
        public int RunCount { get; set; }
        public bool IsShared { get; set; } = false; // New property for sharing capability

        [Ignore] // Not mapped, for convenience
        public IReadOnlyCollection<PackingItem>? Items { get; set; }
    }
}