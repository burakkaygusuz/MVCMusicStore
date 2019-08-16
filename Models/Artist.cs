using System.ComponentModel.DataAnnotations;

namespace MVCMusicStore.Models
{
    public class Artist
    {
        public int ArtistId { get; set; }

        [Required]
        public string Name { get; set; }
    }
}