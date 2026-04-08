using System;
using System.Collections.Generic;

namespace Music_ASM.Models;

public partial class Playlist
{
    public int PlaylistId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? CoverImageUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int UserId { get; set; }

    public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; } = new List<PlaylistSong>();

    public virtual User User { get; set; } = null!;
}
