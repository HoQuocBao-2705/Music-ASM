using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Music_ASM.Models
{
    public class FavoriteSong
    {
        [Key]
        public int FavoriteId { get; set; }

        public int UserId { get; set; }
        public int SongId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("SongId")]
        public virtual Song Song { get; set; }
    }
}