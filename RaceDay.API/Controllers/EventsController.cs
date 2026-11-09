using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EventsController : RaceDayControllerBase
    {
        public EventsController(RaceDayContext context) : base(context)
        {
        }

        private static IQueryable<EventResponseDTO> Project(IQueryable<Event> query) =>
            query.Select(e => new EventResponseDTO
            {
                EventID = e.EventID,
                OrganiserID = e.OrganiserID,
                Name = e.Name,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                Distance = e.Distance,
                EventType = e.EventType,
                EnrolmentCount = e.Enrolments.Count,
                ImageURL = e.EventImages.Where(i => i.IsMain).Select(i => i.ImageURL).FirstOrDefault()
            });

        /// <summary>
        /// Lists all events, soonest first. Open to everyone.
        /// </summary>
        /// <param name="search">Optional text to match against the event name or location.</param>
        /// <param name="eventType">Optional filter: Run, Walk or Cycle.</param>
        /// <response code="200">The list of events.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<EventResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvents([FromQuery] string? search, [FromQuery] string? eventType)
        {
            var query = Context.Events.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.Name.Contains(search) || e.Location.Contains(search));

            if (!string.IsNullOrWhiteSpace(eventType))
                query = query.Where(e => e.EventType == eventType);

            var events = await Project(query.OrderBy(e => e.EventDate)).ToListAsync();

            return Ok(events);
        }

        /// <summary>
        /// Gets one event by its ID. Open to everyone.
        /// </summary>
        /// <param name="id">The event ID.</param>
        /// <response code="200">The event.</response>
        /// <response code="404">No event with that ID.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(EventResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await Project(Context.Events.Where(e => e.EventID == id))
                .FirstOrDefaultAsync();

            if (ev == null)
                return NotFound();

            return Ok(ev);
        }

        /// <summary>
        /// Lists the events created by the logged-in Organiser (used for the dashboard).
        /// </summary>
        /// <response code="200">The Organiser's events.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant.</response>
        [Authorize(Roles = "Organiser")]
        [HttpGet("mine")]
        [ProducesResponseType(typeof(List<EventResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> MyEvents()
        {
            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null)
                return Forbid();

            var events = await Project(
                    Context.Events
                        .Where(e => e.OrganiserID == organiser.OrganiserID)
                        .OrderBy(e => e.EventDate))
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>
        /// Creates a new event owned by the logged-in Organiser.
        /// </summary>
        /// <response code="201">Event created.</response>
        /// <response code="400">Invalid details (for example EventType is not Run, Walk or Cycle).</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        [ProducesResponseType(typeof(EventResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent(EventDTO dto)
        {
            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null)
                return Forbid();

            var newEvent = new Event
            {
                OrganiserID = organiser.OrganiserID,
                Name = dto.Name.Trim(),
                Description = dto.Description.Trim(),
                EventDate = dto.EventDate!.Value.Date,
                Location = dto.Location.Trim(),
                Distance = dto.Distance,
                EventType = dto.EventType
            };

            Context.Events.Add(newEvent);
            await Context.SaveChangesAsync();

            var response = await Project(
                    Context.Events.Where(e => e.EventID == newEvent.EventID))
                .FirstAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = newEvent.EventID }, response);
        }

        /// <summary>
        /// Updates one of the logged-in Organiser's own events.
        /// </summary>
        /// <param name="id">The event ID.</param>
        /// <response code="200">Event updated.</response>
        /// <response code="400">Invalid details.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">No event with that ID.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(EventResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int id, EventDTO dto)
        {
            var existingEvent = await Context.Events.FindAsync(id);

            if (existingEvent == null)
                return NotFound();

            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null ||
                existingEvent.OrganiserID != organiser.OrganiserID)
            {
                return Forbid();
            }

            existingEvent.Name = dto.Name.Trim();
            existingEvent.Description = dto.Description.Trim();
            existingEvent.EventDate = dto.EventDate!.Value.Date;
            existingEvent.Location = dto.Location.Trim();
            existingEvent.Distance = dto.Distance;
            existingEvent.EventType = dto.EventType;
            existingEvent.UpdatedAt = DateTime.UtcNow;

            await Context.SaveChangesAsync();

            var response = await Project(Context.Events.Where(e => e.EventID == id))
                .FirstAsync();

            return Ok(response);
        }

        /// <summary>
        /// Deletes one of the logged-in Organiser's own events.
        /// </summary>
        /// <param name="id">The event ID.</param>
        /// <response code="200">Event deleted.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">No event with that ID.</response>
        /// <response code="409">Participants are already enrolled, so the event cannot be deleted.</response>
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var existingEvent = await Context.Events.FindAsync(id);

            if (existingEvent == null)
                return NotFound();

            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null ||
                existingEvent.OrganiserID != organiser.OrganiserID)
            {
                return Forbid();
            }

            if (await Context.Enrolments.AnyAsync(e => e.EventID == id))
            {
                return Conflict(new MessageDTO
                {
                    Message = "This event cannot be deleted because participants are already enrolled."
                });
            }

            Context.Events.Remove(existingEvent);
            await Context.SaveChangesAsync();

            return Ok(new MessageDTO { Message = "Event deleted successfully." });
        }
    }
}