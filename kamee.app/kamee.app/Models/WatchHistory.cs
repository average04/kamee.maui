using Postgrest.Attributes;
using Postgrest.Models;

namespace kamee.app.Models
{
    [Table("watch_history")]
    public class WatchHistory : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("room_id")]
        public string RoomId { get; set; } = string.Empty;

        [Column("title")]
        public string? Title { get; set; }

        [Column("thumbnail_url")]
        public string? ThumbnailUrl { get; set; }

        [Column("watched_at")]
        public DateTime WatchedAt { get; set; }
    }
}
