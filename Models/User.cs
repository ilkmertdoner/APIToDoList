using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManagerApi.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        public string? Email { get; set; }

        [Required]
        public string Username { get; set; }
        
        [Required]
        public string Password { get; set; } 

        public DateTime CreationTime { get; set; } = DateTime.Now;

        [JsonIgnore]
        public ICollection<TaskAssign>? AssignedTasks { get; set; }
    }
}
