namespace TaskManagerApi.Models
{
    public class FriendSystem
    {
        public int Id { get; set; }
        public int RequesterId { get; set; }
        public User Requester { get; set; }
        public int ReceiverId { get; set; }
        public User Receiver { get; set; }
        public bool IsAccepted { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
