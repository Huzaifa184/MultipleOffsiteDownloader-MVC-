
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace MultipleOffsiteDownloader.Models
{
    public class Login
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Yturl { get; set; } = string.Empty;

        public bool IsInProgress { get; set; }

        public bool IsComplete { get; set; }

        public bool IsFailed { get; set; }

        public DateTime Timestamp { get; set; }
        public string Title { get; set; } = string.Empty;

        public string VideoId { get; set; } = string.Empty;
        public int EmailId { get; set; }
        public string Description { get; set; } = string.Empty;

        public DateTime Created { get; set; }

        public DateTime? Completed { get; set; }
    }

}

