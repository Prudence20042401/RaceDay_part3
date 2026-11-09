using System.Net;
using System.Net.Http.Json;
using Xunit;


namespace RaceDay.API.Tests
{
    public class EventTests : ApiTestBase
    {
        public EventTests(CustomWebApplicationFactory factory) : base(factory) { }

        [Fact]
        public async Task UnauthenticatedUser_CannotCreateEvent()
        {
            var response = await Client.PostAsJsonAsync("/api/Events", NewEvent());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Participant_CannotCreateEvent()
        {
            await LoginAsNewParticipant();

            var response = await Client.PostAsJsonAsync("/api/Events", NewEvent());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CanCreateEvent()
        {
            await LoginAsNewOrganiser();

            var response = await Client.PostAsJsonAsync("/api/Events", NewEvent());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithInvalidEventType_ReturnsBadRequest()
        {
            await LoginAsNewOrganiser();

            var response = await Client.PostAsJsonAsync("/api/Events", new
            {
                name = "Bad Type",
                description = "Invalid type",
                eventDate = "2030-06-01",
                location = "Pretoria",
                distance = 5.0,
                eventType = "Swim"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithNegativeDistance_ReturnsBadRequest()
        {
            await LoginAsNewOrganiser();

            var response = await Client.PostAsJsonAsync("/api/Events", new
            {
                name = "Bad Distance",
                description = "Invalid distance",
                eventDate = "2030-06-01",
                location = "Pretoria",
                distance = -5.0,
                eventType = "Run"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Anyone_CanListEvents_AndGetOneById()
        {
            await LoginAsNewOrganiser();
            var eventId = await CreateEvent("Listed Event");
            await Logout();

            var listResponse = await Client.GetAsync("/api/Events");
            var events = await listResponse.Content.ReadFromJsonAsync<List<EventInfo>>();
            var oneResponse = await Client.GetAsync("/api/Events/" + eventId);

            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            Assert.Contains(events!, e => e.EventID == eventId);
            Assert.Equal(HttpStatusCode.OK, oneResponse.StatusCode);
        }

        [Fact]
        public async Task GetEvent_WithUnknownId_ReturnsNotFound()
        {
            var response = await Client.GetAsync("/api/Events/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CanSeeOnlyTheirOwnEventsInMine()
        {
            await LoginAsNewOrganiser();
            var firstEventId = await CreateEvent("Organiser A Event");
            await Logout();

            await LoginAsNewOrganiser();
            var secondEventId = await CreateEvent("Organiser B Event");

            var response = await Client.GetAsync("/api/Events/mine");
            var events = await response.Content.ReadFromJsonAsync<List<EventInfo>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(events!, e => e.EventID == secondEventId);
            Assert.DoesNotContain(events!, e => e.EventID == firstEventId);
        }

        [Fact]
        public async Task Participant_CannotViewOrganiserMineEndpoint()
        {
            await LoginAsNewParticipant();

            var response = await Client.GetAsync("/api/Events/mine");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        [Fact]
        public async Task Anyone_CanListEvents()
        {
            var response = await Client.GetAsync("/api/Events");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CanUpdateOwnEvent()
        {
            await LoginAsNewOrganiser();
            var eventId = await CreateEvent();

            var response = await Client.PutAsJsonAsync("/api/Events/" + eventId, new
            {
                name = "Renamed Event",
                description = "Updated description",
                eventDate = "2030-07-01",
                location = "Cape Town",
                distance = 21.1,
                eventType = "Run"
            });
            var updated = await response.Content.ReadFromJsonAsync<EventInfo>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Renamed Event", updated!.Name);
        }

        [Fact]
        public async Task Organiser_CannotUpdateAnotherOrganisersEvent()
        {
            await LoginAsNewOrganiser();
            var eventId = await CreateEvent();
            await Logout();

            await LoginAsNewOrganiser();
            var response = await Client.PutAsJsonAsync("/api/Events/" + eventId, NewEvent("Hijacked"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Participant_CannotUpdateEvent()
        {
            await LoginAsNewOrganiser();
            var eventId = await CreateEvent();
            await Logout();

            await LoginAsNewParticipant();
            var response = await Client.PutAsJsonAsync("/api/Events/" + eventId, NewEvent("Hijacked"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CanDeleteOwnEvent()
        {
            await LoginAsNewOrganiser();
            var eventId = await CreateEvent();

            var response = await Client.DeleteAsync("/api/Events/" + eventId);
            var afterDelete = await Client.GetAsync("/api/Events/" + eventId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
        }

        [Fact]
        public async Task Participant_CannotDeleteEvent()
        {
            await LoginAsNewOrganiser();
            var eventId = await CreateEvent();
            await Logout();

            await LoginAsNewParticipant();
            var response = await Client.DeleteAsync("/api/Events/" + eventId);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CannotDeleteEventThatHasEnrolments()
        {
            var (organiserEmail, eventId, categoryId) = await OrganiserWithEventAndCategory();
            await Logout();

            await LoginAsNewParticipant();
            Assert.Equal(HttpStatusCode.OK, (await Enrol(eventId, categoryId)).StatusCode);
            await Logout();

            await Login(organiserEmail);
            var response = await Client.DeleteAsync("/api/Events/" + eventId);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

    }
}
