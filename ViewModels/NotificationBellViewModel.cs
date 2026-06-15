using System.Collections.Generic;
using AutoRepairERD.Models;

namespace AutoRepairERD.ViewModels
{
    public class NotificationBellViewModel
    {
        public int UnreadCount { get; set; }
        public List<Notification> Latest { get; set; } = new List<Notification>();
    }
}
