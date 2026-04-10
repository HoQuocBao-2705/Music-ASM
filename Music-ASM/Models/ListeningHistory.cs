using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Music_ASM.Models
{
    public class ListeningHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public int UserId { get; set; }
        public int SongId { get; set; }
        public DateTime ListenedAt { get; set; } = DateTime.Now;
        public int Duration { get; set; } // Số giây đã nghe

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("SongId")]
        public virtual Song Song { get; set; }
    }
}