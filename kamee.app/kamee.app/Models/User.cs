using Postgrest.Attributes;
using Postgrest.Models;

namespace kamee.app.Models
{
    [Table("profiles")]
    public class User : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        [Column("is_online")]
        public bool IsOnline { get; set; }

        [Column("current_room_id")]
        public string? CurrentRoomId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Computed — not stored in DB
        public string AvatarInitials => Username.Length >= 2 ? Username[..2].ToUpper() : "??";

        // Populated from auth session, not from profiles table
        public string Email { get; set; } = string.Empty;
    }
}
