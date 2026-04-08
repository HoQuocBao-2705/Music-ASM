using System;
using System.Collections.Generic;

namespace Music_ASM.Models;

public partial class Song
{
    public int SongId { get; set; }

    public string Title { get; set; } = null!;

    public int Duration { get; set; }

    public string FilePath { get; set; } = null!;

    public string? CoverImageUrl { get; set; }

    public int? ListenCount { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public int GenreId { get; set; }

    public int ArtistId { get; set; }

    public int? AlbumId { get; set; }

    public virtual Album? Album { get; set; }

    public virtual Artist Artist { get; set; } = null!;

    public virtual Genre Genre { get; set; } = null!;

    public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; } = new List<PlaylistSong>();
}
