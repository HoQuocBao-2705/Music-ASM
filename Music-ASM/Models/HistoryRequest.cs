namespace Music_ASM.Models
{
    public class HistoryRequest
    {
        public int SongId { get; set; }
        public int Duration { get; set; }
        public bool IsCompleted { get; set; }
    }
}
