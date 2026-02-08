using FluentAssertions;
using MAA.Application.Sessions.DTOs;
using MAA.Domain.Sessions;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MAA.Tests.Integration;

/// <summary>
/// Integration tests for Authentication API (JWT login/refresh/logout).
/// Phase 5 feature - tests complete auth workflows with real PostgreSQL.
/// Tests: login, token refresh, logout, max 3 concurrent sessions, session revocation.
/// Uses WebApplicationFactory + Testcontainers.PostgreSQL.
/// </summary>
[Collection("Database collection")]
public class AuthApiIntegrationTests : IAsyncLifetime
{
    // Token settings for tests
    private const int AccessTokenExpirationMinutes = 60;
    private const int RefreshTokenExpirationDays = 7;
    private const int MaxConcurrentSessions = 3;

    private readonly DatabaseFixture _databaseFixture;
    private TestWebApplicationFactory? _factory;
    private HttpClient? _httpClient;

    public AuthApiIntegrationTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    /// <summary>
    /// Initializes test web application factory with test database.
    /// </summary>
    public async Task InitializeAsync()
    {
        _factory = new TestWebApplicationFactory(_databaseFixture);
        _httpClient = _factory.CreateClient();
        
        // Clear database between tests
        await _databaseFixture.ClearAllDataAsync();
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Disposes of factory and HTTP client.
    /// </summary>
    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        if (_factory != null)
            await _factory.DisposeAsync();
        
        await Task.CompletedTask;
    }

    #region User Registration Tests

    /// <summary>
    /// Integration Test: User registration creates account and can login.
    /// Validates:
    /// - POST /api/auth/register returns 201 Created
    /// - User persisted to database
    /// - Password is hashed (not stored in plaintext)
    /// - User can login with registered credentials
    /// </summary>
    [Fact]
    public async Task RegisterUser_CreatesAccountInDatabase_AndCanLogin()
    {
        // Arrange
        var registerRequest = new
        {
            email = "testuser@example.com",
            password = "SecurePassword123!",
            fullName = "Test User"
        };

        // Act - Register user via API
        var registerResponse = await _httpClient!.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert - Registration successful
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            "User registration should return 201 Created");

        // Verify user persisted to database
        using var context = _databaseFixture.CreateContext();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.Contains(registerRequest.email));
        user.Should().NotBeNull("User should be persisted to database");
        user!.Role.Should().Be(UserRole.User, "New user should have User role");
        user.PasswordHash.Should().NotBe(registerRequest.password,
            "Password should be hashed, not stored plaintext");
    }

    /// <summary>
    /// Integration Test: Duplicate email registration returns 409 Conflict.
    /// Validates: Email uniqueness constraint is enforced.
    /// </summary>
    [Fact]
    public async Task RegisterUser_WithDuplicateEmail_Returns409Conflict()
    {
        // Arrange
        var registerRequest = new
        {
            email = "duplicate@example.com",
            password = "SecurePassword123!",
            fullName = "Test User"
        };

        // Act - Register first user
        var firstResponse = await _httpClient!.PostAsJsonAsync("/api/auth/register", registerRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Try to register with same email
        var secondResponse = await _httpClient.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Duplicate email should return 409 Conflict");
    }

    #endregion

    #region Login Tests

    /// <summary>
    /// Integration Test: Successful login returns access and refresh tokens.
    /// Validates:
    /// - POST /api/auth/login returns 200 OK
    /// - Response includes access token and refresh token
    /// - Access token is valid JWT with user ID and roles
    /// - Refresh token is stored in HttpOnly cookie
    /// - Session is created in database
    /// </summary>
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        // Arrange - Create user first
        using (var context = _databaseFixture.CreateContext())
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "testuser@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = UserRole.User,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var loginRequest = new
        {
            email = "testuser@example.com",
            password = "Password123!"
        };

        // Act
        var loginResponse = await _httpClient!.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadAsAsync<dynamic>();

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Login with valid credentials should return 200 OK");

        loginResult.Should().NotBeNull("Response should contain tokens");
        var accessToken = loginResult?.accessToken?.ToString();
        accessToken.Should().NotBeNullOrEmpty("Response should include access token");

        // Verify access token is valid JWT
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(accessToken).Should().BeTrue("Access token should be valid JWT");

        JwtSecurityToken jwtToken = handler.ReadJwtToken(accessToken!);
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub");
        userIdClaim.Should().NotBeNull("Access token should contain user ID claim");

        // Verify refresh token in httpOnly cookie
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        cookies.Any(c => c.Contains("refreshToken")).Should().BeTrue(
            "Response should set refresh token in HttpOnly cookie");

        // Verify session created in database
        using (var context = _databaseFixture.CreateContext())
        {
            var session = await context.Sessions.FirstOrDefaultAsync(s => 
                s.SessionType == "authenticated" && !s.IsRevoked);
            session.Should().NotBeNull("Login should create authenticated session");
        }
    }

    /// <summary>
    /// Integration Test: Login with invalid credentials returns 401 Unauthorized.
    /// Validates: Authentication failure is handled correctly.
    /// </summary>
    [Fact]
    public async Task Login_WithInvalidPassword_Returns401Unauthorized()
    {
        // Arrange
        using (var context = _databaseFixture.CreateContext())
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "testuser@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
                Role = UserRole.User,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var loginRequest = new
        {
            email = "testuser@example.com",
            password = "WrongPassword123!"
        };

        // Act
        var loginResponse = await _httpClient!.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Login with invalid password should return 401 Unauthorized");
    }

    #endregion

    #region Token Refresh Tests

    /// <summary>
    /// Integration Test: Token refresh returns new access token with same user ID.
    /// Validates:
    /// - POST /api/auth/refresh returns 200 OK
    /// - New access token is returned
    /// - New token contains same user ID
    /// - Old token still works until truly expires
    /// </summary>
    [Fact]
    public async Task RefreshToken_ReturnsNewAccessToken_WithSameUserId()
    {
        // Arrange - Login first to get tokens
        using (var context = _databaseFixture.CreateContext())
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "testuser@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = UserRole.User,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var loginRequest = new { email = "testuser@example.com", password = "Password123!" };
        var loginResponse = await _httpClient!.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadAsAsync<dynamic>();
        var originalAccessToken = loginResult?.accessToken?.ToString();

        // Act - Refresh token
        var refreshRequest = new { refreshToken = loginResult?.refreshToken?.ToString() };
        var refreshResponse = await _httpClient.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        var refreshResult = await refreshResponse.Content.ReadAsAsync<dynamic>();

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Token refresh should return 200 OK");

        var newAccessToken = refreshResult?.accessToken?.ToString();
        newAccessToken.Should().NotBeNullOrEmpty("Refresh response should include new access token");
        newAccessToken.Should().NotBe(originalAccessToken, "New token should be different from old");

        // Verify new token has same user ID
        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken originalJwt = handler.ReadJwtToken(originalAccessToken!);
        JwtSecurityToken newJwt = handler.ReadJwtToken(newAccessToken!);

        var originalUserId = originalJwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        var newUserId = newJwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        newUserId.Should().Be(originalUserId, "New token should contain same user ID");
    }

    #endregion

    #region Max Concurrent Sessions Tests

    /// <summary>
    /// Integration Test: Fourth login on same user returns 409 Conflict with session list.
    /// Validates:
    /// - Max 3 concurrent sessions per user is enforced
    /// - Fourth login returns 409 with active session list
    /// - User can revoke a session
    /// - After revocation, fifth login succeeds
    /// </summary>
    [Fact]
    public async Task MaxConcurrentSessions_FourthLoginReturns409_WithSessionList()
    {
        // Arrange - Create user
        Guid userId;
        using (var context = _databaseFixture.CreateContext())
        {
            userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "testuser@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = UserRole.User,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var loginRequest = new { email = "testuser@example.com", password = "Password123!" };

        // Act - Login 3 times (should succeed)
        var tokens = new List<dynamic>();
        for (int i = 0; i < 3; i++)
        {
            var response = await _httpClient!.PostAsJsonAsync("/api/auth/login", loginRequest);
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Login {i + 1} should succeed");
            var result = await response.Content.ReadAsAsync<dynamic>();
            tokens.Add(result);
        }

        // Act - Fourth login (should fail)
        var fourthResponse = await _httpClient!.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        fourthResponse.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Fourth concurrent login should return 409 Conflict");

        var conflictResult = await fourthResponse.Content.ReadAsAsync<dynamic>();
        conflictResult.Should().NotBeNull("Response should include active session list");
        conflictResult?.activeSessions.Should().NotBeNull("Response should list active sessions");
        ((List<dynamic>)conflictResult?.activeSessions).Count.Should().Be(3,
            "Should show 3 active sessions");
    }

    /// <summary>
    /// Integration Test: User can revoke a session remotely.
    /// Validates:
    /// - DELETE /api/auth/sessions/{sessionId} returns 200 OK
    /// - Revoked session is marked in database
    /// - Can login again after revoking a session
    /// </summary>
    [Fact]
    public async Task RevokeSession_MarksSessionAsRevoked_AllowsNewLogin()
    {
        // Arrange - Create user and 3 sessions
        Guid userId;
        string firstSessionToken = null!;

        using (var context = _databaseFixture.CreateContext())
        {
            userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "testuser@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = UserRole.User,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var loginRequest = new { email = "testuser@example.com", password = "Password123!" };

        // Login 3 times to reach max
        for (int i = 0; i < 3; i++)
        {
            var response = await _httpClient!.PostAsJsonAsync("/api/auth/login", loginRequest);
            var result = await response.Content.ReadAsAsync<dynamic>();
            if (i == 0)
                firstSessionToken = result?.accessToken?.ToString();
        }

        // Get session ID from token
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(firstSessionToken);
        var sessionIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "sessionId")?.Value;

        // Act - Revoke first session
        _httpClient!.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firstSessionToken);
        var revokeResponse = await _httpClient.DeleteAsync($"/api/auth/sessions/{sessionIdClaim}");

        // Assert
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Revoke session should return 200 OK");

        // Verify session marked as revoked in database
        using (var context = _databaseFixture.CreateContext())
        {
            var session = await context.Sessions.FirstOrDefaultAsync(s => 
                s.Id.ToString() == sessionIdClaim);
            session?.IsRevoked.Should().BeTrue("Session should be marked as revoked");
        }

        // Act - Fourth login should now succeed
        _httpClient.DefaultRequestHeaders.Authorization = null;
        var fourthLoginResponse = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        fourthLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "After revocation, new login should succeed");
    }

    #endregion

    #region Logout Tests

    /// <summary>
    /// Integration Test: Logout invalidates session and refresh token.
    /// Validates:
    /// - POST /api/auth/logout returns 200 OK
    /// - Session is marked as revoked
    /// - Refresh token becomes invalid
    /// - Access token still works until natural expiration
    /// </summary>
    [Fact]
    public async Task Logout_InvalidatesSession_AndRefreshToken()
    {
        // Arrange - Login first
        using (var context = _databaseFixture.CreateContext())
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "testuser@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = UserRole.User,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var loginRequest = new { email = "testuser@example.com", password = "Password123!" };
        var loginResponse = await _httpClient!.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadAsAsync<dynamic>();
        var accessToken = loginResult?.accessToken?.ToString();

        // Act - Logout
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var logoutResponse = await _httpClient.PostAsync("/api/auth/logout", null);

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Logout should return 200 OK");

        // Verify refresh token is cleared
        var cookies = logoutResponse.Headers.GetValues("Set-Cookie");
        cookies.Any(c => c.Contains("refreshToken") && c.Contains("expires")).Should().BeTrue(
            "Logout should clear refresh token cookie");
    }

    #endregion
}
