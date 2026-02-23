namespace TaskManagerApi.Models
{
    public class TaskAssign
    {
        public int TaskId { get; set; }
        public TaskItem Task { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
