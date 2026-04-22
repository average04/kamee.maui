using Postgrest.Attributes;
using Postgrest.Models;

namespace kamee.app.Models
{
    [Table("rooms")]
    public class Room : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("host_id")]
        public string HostId { get; set; } = string.Empty;

        [Column("streaming_platform")]
        public string StreamingPlatform { get; set; } = string.Empty;

        [Column("video_url")]
        public string? VideoUrl { get; set; }

        [Column("is_private")]
        public bool IsPrivate { get; set; }

        [Column("is_live")]
        public bool IsLive { get; set; } = true;

        [Column("viewer_count")]
        public int ViewerCount { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Populated after query — not DB columns
        public List<User> Viewers { get; set; } = new();
        public string HostUsername { get; set; } = string.Empty;
    }
}
