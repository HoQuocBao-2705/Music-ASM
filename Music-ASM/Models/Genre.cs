using System;
using System.Collections.Generic;

namespace Music_ASM.Models;

public partial class Genre
{
    public int GenreId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<Song> Songs { get; set; } = new List<Song>();
}
