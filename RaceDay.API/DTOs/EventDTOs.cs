using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    /// <summary>Fields an Organiser supplies to create or update an event.</summary>
    public class EventDTO
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime? EventDate { get; set; }

        [Required, MaxLength(300)]
        public string Location { get; set; } = string.Empty;

        /// <summary>Distance in kilometres.</summary>
        [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
        public decimal Distance { get; set; }

        /// <summary>Run, Walk or Cycle.</summary>
        [Required]
        [RegularExpression("^(Run|Walk|Cycle)$",
            ErrorMessage = "EventType must be Run, Walk or Cycle.")]
        public string EventType { get; set; } = "Run";
    }

    /// <summary>An event as returned by the API.</summary>
    public class EventResponseDTO
    {
        public int EventID { get; set; }
        public int OrganiserID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public string EventType { get; set; } = string.Empty;

        /// <summary>Number of enrolments for this event.</summary>
        public int EnrolmentCount { get; set; }
        public string? ImageURL { get; set; }
    }
}
