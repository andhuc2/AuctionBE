using System;
using System.Collections.Generic;

namespace API.Models
{
    public partial class Report
    {
        public int? Id { get; set; }
        public int? UserId { get; set; }
        public string? Content { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
    }
}