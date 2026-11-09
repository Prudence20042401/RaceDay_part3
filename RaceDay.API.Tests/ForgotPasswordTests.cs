using System.Net;
using System.Net.Http.Json;
using System.Web;
using Xunit;

namespace RaceDay.API.Tests
{
    /// <summary>Forgot-password and reset-password behaviour.</summary>
    public class ForgotPasswordTests : ApiTestBase
    {
        public ForgotPasswordTests(CustomWebApplicationFactory factory) : base(factory) { }

        public class ForgotResponse
        {
            public string Message { get; set; } = string.Empty;
            public string? ResetLink { get; set; }
        }

        private const string NewPassword = "BrandNewPassword456!";

        private async Task<string> RequestToken(string email)
        {
            var response = await Client.PostAsJsonAsync("/api/Auth/forgot-password", new { email });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<ForgotResponse>();
            Assert.False(string.IsNullOrEmpty(body?.ResetLink));

            var query = HttpUtility.ParseQueryString(new Uri(body!.ResetLink!).Query);
            return query["token"]!;
        }

        private Task<HttpResponseMessage> Reset(string token, string password) =>
            Client.PostAsJsonAsync("/api/Auth/reset-password", new { token, newPassword = password });

        private Task<HttpResponseMessage> LoginWith(string email, string password) =>
            Client.PostAsJsonAsync("/api/Auth/login", new { email, password });

        [Fact]
        public async Task ForgotPassword_ExistingEmail_ReturnsOkWithGenericMessage()
        {
            var email = UniqueEmail("forgot");
            await RegisterParticipant(email);

            var response = await Client.PostAsJsonAsync("/api/Auth/forgot-password", new { email });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ForgotResponse>();
            Assert.False(string.IsNullOrWhiteSpace(body?.Message));
        }

        [Fact]
        public async Task ForgotPassword_UnknownEmail_ReturnsSameOkMessageWithoutLink()
        {
            var known = UniqueEmail("known");
            await RegisterParticipant(known);

            var knownBody = await (await Client.PostAsJsonAsync("/api/Auth/forgot-password", new { email = known }))
                .Content.ReadFromJsonAsync<ForgotResponse>();
            var unknownResponse = await Client.PostAsJsonAsync("/api/Auth/forgot-password",
                new { email = UniqueEmail("nobody") });
            var unknownBody = await unknownResponse.Content.ReadFromJsonAsync<ForgotResponse>();

            Assert.Equal(HttpStatusCode.OK, unknownResponse.StatusCode);
            Assert.Equal(knownBody!.Message, unknownBody!.Message);
            Assert.Null(unknownBody.ResetLink);
        }

        [Fact]
        public async Task ForgotPassword_InvalidEmailFormat_ReturnsBadRequest()
        {
            var response = await Client.PostAsJsonAsync("/api/Auth/forgot-password", new { email = "not-an-email" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_WithValidToken_ChangesPassword()
        {
            var email = UniqueEmail("reset");
            await RegisterParticipant(email);
            var token = await RequestToken(email);

            var reset = await Reset(token, NewPassword);

            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LoginWith(email, NewPassword)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginWith(email, Password)).StatusCode);
        }

        [Fact]
        public async Task ResetPassword_TokenCannotBeUsedTwice()
        {
            var email = UniqueEmail("twice");
            await RegisterParticipant(email);
            var token = await RequestToken(email);

            Assert.Equal(HttpStatusCode.OK, (await Reset(token, NewPassword)).StatusCode);
            var second = await Reset(token, "AnotherPassword789!");

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LoginWith(email, NewPassword)).StatusCode);
        }

        [Fact]
        public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
        {
            var response = await Reset("this-is-not-a-real-token", NewPassword);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_WithTooShortPassword_ReturnsBadRequestAndKeepsOldPassword()
        {
            var email = UniqueEmail("short");
            await RegisterParticipant(email);
            var token = await RequestToken(email);

            var response = await Reset(token, "short");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LoginWith(email, Password)).StatusCode);
        }
    }
}
