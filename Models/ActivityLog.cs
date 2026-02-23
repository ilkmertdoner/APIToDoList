using System;

namespace TaskManagerApi.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public string TokenId { get; set; }
        public int? TaskId { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}