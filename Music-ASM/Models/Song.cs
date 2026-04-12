using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Music_ASM.Models;

public partial class Song
{
    public int SongId { get; set; }

    public string Title { get; set; } = null!;

    public int Duration { get; set; }

    public string? FilePath { get; set; }

    public string? CoverImageUrl { get; set; }

    public int ListenCount { get; set; } = 0;

    public DateOnly? ReleaseDate { get; set; }

    public int GenreId { get; set; }

    public int ArtistId { get; set; }

    public int? AlbumId { get; set; }

    // ❗ QUAN TRỌNG: bỏ validation navigation
    [ValidateNever]
    public virtual Album? Album { get; set; }

    [ValidateNever]
    public virtual Artist? Artist { get; set; }

    [ValidateNever]
    public virtual Genre? Genre { get; set; }

    [ValidateNever]
    public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; } = new List<PlaylistSong>();
}