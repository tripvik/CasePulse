using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPendant.MAUIHybrid.Models
{
    public class ChatMessage
    {
        public string Message { get; set; } 
        public string User { get; set; }
        public DateTime Timestamp { get; set; } 
        public string Initials { get; set; } 
    }
}
