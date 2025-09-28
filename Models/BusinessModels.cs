using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExcelReplacement.Models
{
    // User Management
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Email { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }
        
        [Required]
        public string PasswordHash { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLoginAt { get; set; }
        
        // Foreign Keys
        public int OrganizationId { get; set; }
        public Organization Organization { get; set; }
        
        public int RoleId { get; set; }
        public Role Role { get; set; }
        
        // Navigation Properties
        public ICollection<DocumentTemplate> DocumentTemplates { get; set; } = new List<DocumentTemplate>();
        public ICollection<ProcessingJob> ProcessingJobs { get; set; } = new List<ProcessingJob>();
    }

    // Organization Management
    public class Organization
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
        
        [MaxLength(500)]
        public string Description { get; set; }
        
        [MaxLength(100)]
        public string Domain { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? SubscriptionExpiresAt { get; set; }
        
        public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
        
        // Navigation Properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<DataSource> DataSources { get; set; } = new List<DataSource>();
        public ICollection<DocumentTemplate> DocumentTemplates { get; set; } = new List<DocumentTemplate>();
    }

    // Role-Based Access Control
    public class Role
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }
        
        [MaxLength(200)]
        public string Description { get; set; }
        
        public bool CanManageUsers { get; set; }
        public bool CanManageTemplates { get; set; }
        public bool CanManageDataSources { get; set; }
        public bool CanProcessDocuments { get; set; }
        public bool CanViewAnalytics { get; set; }
        public bool CanManageSettings { get; set; }
    }

    // Data Source Management
    public class DataSource
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        [MaxLength(200)]
        public string Description { get; set; }
        
        public DataSourceType Type { get; set; }
        
        [MaxLength(500)]
        public string ConnectionString { get; set; }
        
        [MaxLength(1000)]
        public string Configuration { get; set; } // JSON configuration
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastTestedAt { get; set; }
        
        public bool IsConnectionValid { get; set; }
        
        // Foreign Keys
        public int OrganizationId { get; set; }
        public Organization Organization { get; set; }
        
        public int CreatedById { get; set; }
        public User CreatedBy { get; set; }
        
        // Navigation Properties
        public ICollection<ProcessingJob> ProcessingJobs { get; set; } = new List<ProcessingJob>();
    }

    // Enhanced Document Templates
    public class DocumentTemplate
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
        
        [MaxLength(500)]
        public string Description { get; set; }
        
        public TemplateType Type { get; set; }
        
        [MaxLength(1000)]
        public string FilePath { get; set; }
        
        [MaxLength(2000)]
        public string Placeholders { get; set; } // JSON array of placeholders
        
        public bool IsActive { get; set; } = true;
        
        public bool IsPublic { get; set; } = false; // For marketplace
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Foreign Keys
        public int OrganizationId { get; set; }
        public Organization Organization { get; set; }
        
        public int CreatedById { get; set; }
        public User CreatedBy { get; set; }
        
        // Navigation Properties
        public ICollection<ProcessingJob> ProcessingJobs { get; set; } = new List<ProcessingJob>();
    }

    // Processing Jobs with Enhanced Features
    public class ProcessingJob
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
        
        public JobStatus Status { get; set; } = JobStatus.Pending;
        
        public int DocumentsProcessed { get; set; } = 0;
        
        public int DocumentsTotal { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? StartedAt { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        [MaxLength(2000)]
        public string ErrorMessage { get; set; }
        
        [MaxLength(1000)]
        public string OutputPath { get; set; }
        
        // Foreign Keys
        public int OrganizationId { get; set; }
        public Organization Organization { get; set; }
        
        public int CreatedById { get; set; }
        public User CreatedBy { get; set; }
        
        public int? DataSourceId { get; set; }
        public DataSource DataSource { get; set; }
        
        public int DocumentTemplateId { get; set; }
        public DocumentTemplate DocumentTemplate { get; set; }
        
        // Navigation Properties
        public ICollection<ProcessingJobLog> Logs { get; set; } = new List<ProcessingJobLog>();
    }

    // Job Logging for Audit and Debugging
    public class ProcessingJobLog
    {
        [Key]
        public int Id { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public LogLevel Level { get; set; }
        
        [MaxLength(1000)]
        public string Message { get; set; }
        
        [MaxLength(2000)]
        public string Details { get; set; } // JSON details
        
        // Foreign Keys
        public int ProcessingJobId { get; set; }
        public ProcessingJob ProcessingJob { get; set; }
    }

    // AI Integration Models
    public class AISuggestion
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Type { get; set; } // mapping, content, error, etc.
        
        [Required]
        [MaxLength(1000)]
        public string Suggestion { get; set; }
        
        public float Confidence { get; set; }
        
        public bool IsAccepted { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Foreign Keys
        public int ProcessingJobId { get; set; }
        public ProcessingJob ProcessingJob { get; set; }
    }

    // Enums
    public enum SubscriptionTier
    {
        Free = 0,
        Professional = 1,
        Enterprise = 2,
        Custom = 3
    }

    public enum DataSourceType
    {
        CSV = 0,
        Excel = 1,
        SQLServer = 2,
        MySQL = 3,
        PostgreSQL = 4,
        SQLite = 5,
        RESTAPI = 6,
        GraphQL = 7,
        GoogleSheets = 8,
        Salesforce = 9
    }

    public enum TemplateType
    {
        Word = 0,
        Excel = 1,
        PowerPoint = 2,
        PDF = 3,
        Email = 4,
        Report = 5
    }

    public enum JobStatus
    {
        Pending = 0,
        Running = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4
    }

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    }
}
