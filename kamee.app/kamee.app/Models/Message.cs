using Postgrest.Attributes;
using Postgrest.Models;

namespace kamee.app.Models
{
    [Table("messages")]
    public class Message : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        [Column("room_id")]
        public string RoomId { get; set; } = string.Empty;

        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("content")]
        public string Content { get; set; } = string.Empty;

        [Column("sent_at")]
        public DateTime SentAt { get; set; }

        // Populated after query — not DB columns
        public string Username { get; set; } = string.Empty;
        public string AvatarInitials { get; set; } = string.Empty;
        public bool IsFromCurrentUser { get; set; }
    }
}
