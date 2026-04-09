using System.Collections.Generic;

namespace Music_ASM.Models
{
    public class HomeViewModel
    {
        public List<Playlist> Playlists { get; set; }  // ← Đổi từ PlaylistSongs thành Playlists
        public List<Song> TopSongs { get; set; }
        public List<dynamic> Genres { get; set; }
    }
}