using Postgrest.Attributes;
using Postgrest.Models;

namespace kamee.app.Models
{
    [Table("room_members")]
    public class RoomMember : BaseModel
    {
        [Column("room_id")]
        public string RoomId { get; set; } = string.Empty;

        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; }
    }
}
