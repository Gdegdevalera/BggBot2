using BggBot2.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BggBot2.Data
{
    [Index(nameof(ApplicationUserId), nameof(FeedUrl), IsUnique = true)]
    public class Subscription
    {
        public long Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }

        public virtual ApplicationUser ApplicationUser { get; set; }

        [Url]
        public string FeedUrl { get; set; }

        public bool IsEnabled { get; set; }

        [NotMapped]
        public int PendingCount { get; set; }

        [NotMapped]
        public bool HasError { get; set; }

        [NotMapped]
        public bool IsReadOnly { get; set; } = true;
    }
}
