using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

/// <summary>Uploads event banners and participant profile pictures to Azure Blob Storage.</summary>
[ApiController]
[Route("api/storage")]
[Authorize]
public class StorageController : RaceDayControllerBase
{
    private readonly IConfiguration _configuration;
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxFileSize = 5 * 1024 * 1024;
    private const long MaxRequestSize = 6 * 1024 * 1024;

    public StorageController(RaceDayContext context, IConfiguration configuration) : base(context) => _configuration = configuration;

    [HttpPost("events/{eventId:int}/banner")]
    [Authorize(Roles = "Organiser")]
    [RequestSizeLimit(MaxRequestSize)]
    public async Task<IActionResult> UploadEventBanner(int eventId, IFormFile file)
    {
        var organiser = await GetCurrentOrganiserAsync();
        var raceEvent = await Context.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (raceEvent == null) return NotFound(new { message = "Event not found." });
        if (organiser == null || raceEvent.OrganiserID != organiser.OrganiserID) return Forbid();
        var validation = Validate(file);
        if (validation != null) return BadRequest(new { message = validation });
        if (string.IsNullOrWhiteSpace(_configuration["AzureBlobStorage:ConnectionString"])) return StatusCode(503, new { message = "Azure Blob Storage is not configured." });
        string url;
        try { url = await UploadAsync(file, "event-banners"); } catch { return StatusCode(503, new { message = "Image storage is temporarily unavailable." }); }
        var previousImages = await Context.EventImages.Where(i => i.EventID == eventId && i.IsMain).ToListAsync();
        foreach (var previous in previousImages) previous.IsMain = false;
        Context.EventImages.Add(new EventImage { EventID = eventId, ImageURL = url, IsMain = true });
        await Context.SaveChangesAsync();
        return Ok(new { imageUrl = url, message = "Event banner uploaded." });
    }

    [HttpPost("profile-picture")]
    [Authorize(Roles = "Participant")]
    [RequestSizeLimit(MaxRequestSize)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var participant = await GetCurrentParticipantAsync();
        if (participant == null) return NotFound(new { message = "Participant profile not found." });
        var validation = Validate(file);
        if (validation != null) return BadRequest(new { message = validation });
        if (string.IsNullOrWhiteSpace(_configuration["AzureBlobStorage:ConnectionString"])) return StatusCode(503, new { message = "Azure Blob Storage is not configured." });
        string url;
        try { url = await UploadAsync(file, "profile-pictures"); } catch { return StatusCode(503, new { message = "Image storage is temporarily unavailable." }); }
        participant.ProfilePictureURL = url;
        participant.UpdatedAt = DateTime.UtcNow;
        await Context.SaveChangesAsync();
        return Ok(new { imageUrl = url, message = "Profile picture uploaded." });
    }

    private static string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0) return "Please select an image file.";
        if (file.Length > MaxFileSize) return "Images must be 5 MB or smaller.";
        if (!AllowedTypes.Contains(file.ContentType.ToLowerInvariant())) return "Only JPG, PNG and WEBP images are allowed.";
        return null;
    }

    private async Task<string> UploadAsync(IFormFile file, string folder)
    {
        var connection = _configuration["AzureBlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Azure Blob Storage is not configured. Set AzureBlobStorage__ConnectionString.");
        var containerName = _configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";
        var service = new BlobServiceClient(connection);
        var container = service.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blob = container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{extension}");
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
        return blob.Uri.ToString();
    }
}
