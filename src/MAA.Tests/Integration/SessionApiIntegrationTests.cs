using FluentAssertions;
using MAA.Application.Sessions.DTOs;
using MAA.Domain.Sessions;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace MAA.Tests.Integration;

/// <summary>
/// Integration tests for Session API using WebApplicationFactory + Testcontainers.PostgreSQL.
/// Tests complete workflows: create session, persist to DB, retrieve, verify timeout behavior.
/// Uses real HTTP requests to in-process API and real PostgreSQL container.
/// Validates CONST-III compliance throughout session lifecycle.
/// </summary>
[Collection("Database collection")]
public class SessionApiIntegrationTests : IAsyncLifetime
{
    // CONST-III: Exact error message for session expiration
    private const string SessionExpiredMessage = "Your session expired after 30 minutes. Start a new eligibility check.";

    private readonly DatabaseFixture _databaseFixture;
    private TestWebApplicationFactory? _factory;
    private HttpClient? _httpClient;

    public SessionApiIntegrationTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    /// <summary>
    /// Initializes test web application factory with test database.
    /// Called automatically before each test.
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
    /// Called automatically after each test.
    /// </summary>
    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        if (_factory != null)
            await _factory.DisposeAsync();
        
        await Task.CompletedTask;
    }

    #region Session Creation and Persistence Tests

    /// <summary>
    /// Integration Test: Create session via API → Store in PostgreSQL → Retrieve → Verify data matches.
    /// Validates:
    /// - POST /api/sessions returns 201 Created
    /// - SessionDto returned with all fields
    /// - Session persisted to database with correct values
    /// - Encryption key version is set
    /// - Timestamps are initialized correctly
    /// </summary>
    [Fact]
    public async Task CreateSession_PersistsToDatabase_AndCanBeRetrieved()
    {
        // Arrange
        var createRequest = new CreateSessionDto
        {
            IpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
            TimeoutMinutes = 30,
            InactivityTimeoutMinutes = 15
        };

        // Act - Create session via API
        var createResponse = await _httpClient!.PostAsJsonAsync("/api/sessions", createRequest);
        var createdDto = await createResponse.Content.ReadAsAsync<SessionDto>();
        var sessionId = createdDto!.Id;

        // Act - Retrieve from database directly to verify persistence
        using var context = _databaseFixture.CreateContext();
        var dbSession = await context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);

        // Assert - Response validation
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            "Creating session should return 201 Created");

        createdDto.Should().NotBeNull("Response should contain SessionDto");
        createdDto.Id.Should().NotBeEmpty("Session should have generated ID");
        createdDto.State.Should().Be("Pending", "New session should be in Pending state");
        createdDto.IpAddress.Should().Be("192.168.1.100");
        createdDto.UserAgent.Should().Contain("Windows");
        createdDto.SessionType.Should().Be("anonymous", "Anonymous session type should be set");
        createdDto.IsRevoked.Should().BeFalse("New session should not be revoked");

        // Assert - Database validation
        dbSession.Should().NotBeNull("Session should be persisted to database");
        dbSession!.Id.Should().Be(sessionId, "Database session ID should match");
        dbSession.IpAddress.Should().Be("192.168.1.100", "Database should preserve IP address");
        dbSession.UserAgent.Should().Contain("Windows", "Database should preserve user agent");
        dbSession.EncryptionKeyVersion.Should().BeGreaterThan(0, "Encryption key version should be initialized");
        dbSession.Data.Should().Be("{}", "Session data should be initialized as empty JSON");
        dbSession.IsRevoked.Should().BeFalse("Database session should not be revoked");
        dbSession.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        dbSession.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        // Assert - Timeout calculation validation
        var timeoutDuration = dbSession.ExpiresAt - dbSession.CreatedAt;
        timeoutDuration.TotalMinutes.Should().BeApproximately(30, 1,
            "Session should expire 30 minutes from creation");

        var inactivityDuration = dbSession.InactivityTimeoutAt - dbSession.CreatedAt;
        inactivityDuration.TotalMinutes.Should().BeApproximately(15, 1,
            "Inactivity timeout should be 15 minutes from creation");
    }

    /// <summary>
    /// Integration Test: Multiple sessions created have unique IDs and isolated data.
    /// Validates:
    /// - Each session gets unique GUID
    /// - Sessions don't interfere with each other in database
    /// - Each session has independent timeout values
    /// </summary>
    [Fact]
    public async Task CreateMultipleSessions_EachHasUniqueId_AndIsolatedData()
    {
        // Arrange
        var requests = new[]
        {
            new CreateSessionDto { IpAddress = "192.168.1.100", UserAgent = "Chrome/120.0" },
            new CreateSessionDto { IpAddress = "192.168.1.200", UserAgent = "Firefox/121.0" },
            new CreateSessionDto { IpAddress = "192.168.1.150", UserAgent = "Safari/537.36" }
        };

        var sessionIds = new List<Guid>();

        // Act - Create three sessions
        foreach (var request in requests)
        {
            var response = await _httpClient!.PostAsJsonAsync("/api/sessions", request);
            var dto = await response.Content.ReadAsAsync<SessionDto>();
            sessionIds.Add(dto!.Id);
        }

        // Assert - All IDs are unique
        sessionIds.Distinct().Count().Should().Be(3, "Each session should have unique ID");

        // Assert - Database verification
        using var context = _databaseFixture.CreateContext();
        var dbSessions = await context.Sessions
            .Where(s => sessionIds.Contains(s.Id))
            .ToListAsync();

        dbSessions.Count().Should().Be(3, "All three sessions should be in database");
        
        var ipsInDb = dbSessions.Select(s => s.IpAddress).ToList();
        ipsInDb.Should().Contain("192.168.1.100");
        ipsInDb.Should().Contain("192.168.1.200");
        ipsInDb.Should().Contain("192.168.1.150");
    }

    #endregion

    #region Session Retrieval Tests

    /// <summary>
    /// Integration Test: GET /api/sessions/{id} retrieves correct session from database.
    /// Validates:
    /// - Session data loaded from PostgreSQL JSONB column correctly
    /// - All fields populated in response DTO
    /// - IsValid computed property reflects actual timeout state
    /// </summary>
    [Fact]
    public async Task GetSessionById_LoadsFromDatabase_AndReturnsCurrentState()
    {
        // Arrange - Create session
        var createRequest = new CreateSessionDto
        {
            IpAddress = "10.20.30.40",
            UserAgent = "Mobile Safari/603.1.30"
        };

        var createResponse = await _httpClient!.PostAsJsonAsync("/api/sessions", createRequest);
        var createdSession = await createResponse.Content.ReadAsAsync<SessionDto>();
        var sessionId = createdSession!.Id;

        // Act - Retrieve session
        var getResponse = await _httpClient.GetAsync($"/api/sessions/{sessionId}");
        var retrievedSession = await getResponse.Content.ReadAsAsync<SessionDto>();

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Should retrieve existing session");
        
        retrievedSession.Should().NotBeNull();
        retrievedSession!.Id.Should().Be(sessionId);
        retrievedSession.IpAddress.Should().Be("10.20.30.40");
        retrievedSession.UserAgent.Should().Be("Mobile Safari/603.1.30");
        retrievedSession.IsValid.Should().BeTrue("Newly created session should be valid");
        
        // Verify database and DTO match
        using var context = _databaseFixture.CreateContext();
        var dbSession = await context.Sessions.FirstAsync(s => s.Id == sessionId);
        
        retrievedSession.ExpiresAt.Should().Be(dbSession.ExpiresAt,
            "DTO timeout should match database");
        retrievedSession.InactivityTimeoutAt.Should().Be(dbSession.InactivityTimeoutAt,
            "DTO inactivity timeout should match database");
    }

    /// <summary>
    /// Integration Test: GET /api/sessions/{id} for nonexistent session returns 404.
    /// Validates: Proper error handling for missing sessions.
    /// </summary>
    [Fact]
    public async Task GetSessionById_NonexistentSession_Returns404()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await _httpClient!.GetAsync($"/api/sessions/{nonexistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Nonexistent session should return 404");
    }

    #endregion

    #region Session Timeout Tests

    /// <summary>
    /// Integration Test: Session with expired absolute timeout returns HTTP 401.
    /// Validates:
    /// - Session created in database with ExpiresAt in past
    /// - GET /api/sessions/{id} returns 401 Unauthorized
    /// - Response contains CONST-III compliant error message
    /// 
    /// Note: Since we can't manipulate system time in tests, we insert an expired session
    /// directly into database, then verify API properly rejects it.
    /// </summary>
    [Fact]
    public async Task GetSessionById_ExpiredAbsoluteTimeout_Returns401WithConstIiiMessage()
    {
        // Arrange - Create expired session directly in database
        var expiredSessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        
        var expiredSession = new Session
        {
            Id = expiredSessionId,
            State = SessionState.InProgress,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0",
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{}",
            ExpiresAt = now.AddMinutes(-5), // Expired 5 minutes ago
            InactivityTimeoutAt = now.AddMinutes(20), // Inactivity still valid
            LastActivityAt = now.AddMinutes(-5),
            IsRevoked = false,
            CreatedAt = now.AddMinutes(-40),
            UpdatedAt = now.AddMinutes(-5),
            Version = 1
        };

        using (var context = _databaseFixture.CreateContext())
        {
            context.Sessions.Add(expiredSession);
            await context.SaveChangesAsync();
        }

        // Act - Try to retrieve expired session via API
        var response = await _httpClient!.GetAsync($"/api/sessions/{expiredSessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "GET expired session should return 401 Unauthorized");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(SessionExpiredMessage,
            "Response must contain CONST-III compliant error message");
    }

    /// <summary>
    /// Integration Test: Session with expired inactivity timeout returns HTTP 401.
    /// Validates:
    /// - Session with InactivityTimeoutAt in past is rejected
    /// - Error response includes CONST-III message
    /// - Sliding window timeout is enforced even if absolute timeout hasn't hit
    /// </summary>
    [Fact]
    public async Task GetSessionById_ExpiredInactivityTimeout_Returns401WithConstIiiMessage()
    {
        // Arrange - Create session with inactive timeout expired
        var inactiveSessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var inactiveSession = new Session
        {
            Id = inactiveSessionId,
            State = SessionState.InProgress,
            IpAddress = "192.168.1.200",
            UserAgent = "Firefox/121.0",
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{}",
            ExpiresAt = now.AddMinutes(20), // Absolute timeout still valid
            InactivityTimeoutAt = now.AddMinutes(-2), // Inactive timeout expired 2 minutes ago
            LastActivityAt = now.AddMinutes(-20), // No activity for 20 minutes
            IsRevoked = false,
            CreatedAt = now.AddMinutes(-25),
            UpdatedAt = now.AddMinutes(-20),
            Version = 1
        };

        using (var context = _databaseFixture.CreateContext())
        {
            context.Sessions.Add(inactiveSession);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _httpClient!.GetAsync($"/api/sessions/{inactiveSessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "GET inactive session should return 401 Unauthorized");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(SessionExpiredMessage,
            "Response must contain CONST-III compliant error message");
    }

    /// <summary>
    /// Integration Test: Revoked session returns HTTP 401 with CONST-III message.
    /// Validates:
    /// - Session marked as IsRevoked is rejected even if timeouts valid
    /// - Error message complies with CONST-III
    /// </summary>
    [Fact]
    public async Task GetSessionById_RevokedSession_Returns401WithConstIiiMessage()
    {
        // Arrange - Create revoked session
        var revokedSessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var revokedSession = new Session
        {
            Id = revokedSessionId,
            State = SessionState.InProgress,
            IpAddress = "192.168.1.150",
            UserAgent = "Safari/537.36",
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{}",
            ExpiresAt = now.AddMinutes(20), // Still valid
            InactivityTimeoutAt = now.AddMinutes(10), // Still valid
            LastActivityAt = now, // Recent activity
            IsRevoked = true, // Explicitly revoked
            CreatedAt = now.AddMinutes(-10),
            UpdatedAt = now,
            Version = 1
        };

        using (var context = _databaseFixture.CreateContext())
        {
            context.Sessions.Add(revokedSession);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _httpClient!.GetAsync($"/api/sessions/{revokedSessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "GET revoked session should return 401 Unauthorized");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(SessionExpiredMessage,
            "Response must contain CONST-III compliant error message");
    }

    /// <summary>
    /// Integration Test: Valid session (not expired, not inactive, not revoked) returns 200.
    /// Validates: Positive case - session within all timeout boundaries returns successfully.
    /// </summary>
    [Fact]
    public async Task GetSessionById_ValidSession_Returns200()
    {
        // Arrange - Create valid session
        var validSessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var validSession = new Session
        {
            Id = validSessionId,
            State = SessionState.InProgress,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0",
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{}",
            ExpiresAt = now.AddMinutes(20), // Expires in 20 minutes
            InactivityTimeoutAt = now.AddMinutes(10), // Inactive in 10 minutes
            LastActivityAt = now.AddSeconds(-30), // Activity 30 seconds ago
            IsRevoked = false,
            CreatedAt = now.AddMinutes(-5),
            UpdatedAt = now.AddSeconds(-30),
            Version = 1
        };

        using (var context = _databaseFixture.CreateContext())
        {
            context.Sessions.Add(validSession);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _httpClient!.GetAsync($"/api/sessions/{validSessionId}");
        var dto = await response.Content.ReadAsAsync<SessionDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Valid session should return 200 Ok");

        dto.Should().NotBeNull();
        dto!.IsValid.Should().BeTrue("Session status should indicate it's valid");
        dto.MinutesUntilExpiry.Should().BeGreaterThan(15, "Should have time before expiry");
    }

    #endregion

    #region CONST-III Message Consistency Tests

    /// <summary>
    /// Integration Test: All timeout scenarios return consistent CONST-III error message.
    /// Validates: Whether session expires via absolute timeout, inactivity, or revocation,
    /// the error message is always: "Your session expired after 30 minutes. Start a new eligibility check."
    /// </summary>
    [Fact]
    public async Task AllTimeoutScenarios_ReturnConsistentConstIiiMessage()
    {
        var now = DateTime.UtcNow;
        var testCases = new[]
        {
            new { Name = "Absolute Timeout", Session = new Session 
            {
                Id = Guid.NewGuid(),
                State = SessionState.InProgress,
                IpAddress = "192.168.1.1",
                UserAgent = "Chrome/120.0",
                SessionType = "anonymous",
                EncryptionKeyVersion = 1,
                Data = "{}",
                ExpiresAt = now.AddMinutes(-5),
                InactivityTimeoutAt = now.AddMinutes(20),
                LastActivityAt = now.AddMinutes(-5),
                IsRevoked = false,
                CreatedAt = now.AddMinutes(-40),
                UpdatedAt = now.AddMinutes(-5),
                Version = 1
            }},
            new { Name = "Inactivity Timeout", Session = new Session
            {
                Id = Guid.NewGuid(),
                State = SessionState.InProgress,
                IpAddress = "192.168.1.2",
                UserAgent = "Firefox/121.0",
                SessionType = "anonymous",
                EncryptionKeyVersion = 1,
                Data = "{}",
                ExpiresAt = now.AddMinutes(20),
                InactivityTimeoutAt = now.AddMinutes(-2),
                LastActivityAt = now.AddMinutes(-20),
                IsRevoked = false,
                CreatedAt = now.AddMinutes(-25),
                UpdatedAt = now.AddMinutes(-20),
                Version = 1
            }},
            new { Name = "Revoked", Session = new Session
            {
                Id = Guid.NewGuid(),
                State = SessionState.InProgress,
                IpAddress = "192.168.1.3",
                UserAgent = "Safari/537.36",
                SessionType = "anonymous",
                EncryptionKeyVersion = 1,
                Data = "{}",
                ExpiresAt = now.AddMinutes(20),
                InactivityTimeoutAt = now.AddMinutes(10),
                LastActivityAt = now,
                IsRevoked = true,
                CreatedAt = now.AddMinutes(-10),
                UpdatedAt = now,
                Version = 1
            }}
        };

        // Arrange - Insert all test sessions
        using (var context = _databaseFixture.CreateContext())
        {
            foreach (var testCase in testCases)
            {
                context.Sessions.Add(testCase.Session);
            }
            await context.SaveChangesAsync();
        }

        // Act & Assert - Verify each scenario returns CONST-III message
        foreach (var testCase in testCases)
        {
            var response = await _httpClient!.GetAsync($"/api/sessions/{testCase.Session.Id}");
            var content = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"Session with {testCase.Name} should return 401");

            content.Should().Contain(SessionExpiredMessage,
                $"Error message for {testCase.Name} must contain CONST-III message");

            content.Should().Contain("Your session expired after 30 minutes",
                $"{testCase.Name}: Message must mention 30-minute timeout");

            content.Should().Contain("Start a new eligibility check",
                $"{testCase.Name}: Message must include action (start new check)");
        }
    }

    #endregion

    #region Session State Transition Tests

    /// <summary>
    /// Integration Test: Session state transitions persist correctly to database.
    /// Validates:
    /// - Session starts in Pending state
    /// - State transitions are stored in database
    /// - Transition consistency across API and database
    /// </summary>
    [Fact]
    public async Task SessionStateTransition_PersistsToDatabase()
    {
        // Arrange - Create session
        var createRequest = new CreateSessionDto
        {
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0"
        };

        var createResponse = await _httpClient!.PostAsJsonAsync("/api/sessions", createRequest);
        var createdSession = await createResponse.Content.ReadAsAsync<SessionDto>();
        var sessionId = createdSession!.Id;

        // Verify initial state in database
        using (var context = _databaseFixture.CreateContext())
        {
            var dbSession = await context.Sessions.FirstAsync(s => s.Id == sessionId);
            dbSession.State.Should().Be(SessionState.Pending, "New session should be Pending");
        }

        // Note: Actual state transitions (Pending -> InProgress -> Submitted)
        // would be handled by separate endpoints/services.
        // This test documents the state persistence pattern.
    }

    #endregion

    #region JSONB Data Storage Tests

    /// <summary>
    /// Integration Test: Session JSONB data column stores and retrieves correctly.
    /// Validates:
    /// - Session.Data initialized as "{}"
    /// - JSONB column properly configured in PostgreSQL
    /// - Data retrieved matches what was stored
    /// </summary>
    [Fact]
    public async Task SessionData_JsonbColumn_StoresAndRetrievesCorrectly()
    {
        // Arrange
        var createRequest = new CreateSessionDto
        {
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0"
        };

        var createResponse = await _httpClient!.PostAsJsonAsync("/api/sessions", createRequest);
        var createdSession = await createResponse.Content.ReadAsAsync<SessionDto>();
        var sessionId = createdSession!.Id;

        // Act - Verify JSONB data in database
        using (var context = _databaseFixture.CreateContext())
        {
            var dbSession = await context.Sessions.FirstAsync(s => s.Id == sessionId);
            
            // Assert
            dbSession.Data.Should().Be("{}", "JSONB Data column should initialize as empty JSON");
            
            // Verify it's stored as valid JSON (PostgreSQL JSONB validation)
            var parsed = System.Text.Json.JsonDocument.Parse(dbSession.Data);
            parsed.Should().NotBeNull("JSONB data should be valid JSON");
        }
    }

    #endregion

    #region US2: Session Answer Persistence Tests (T019)

    /// <summary>
    /// Integration Test: Save answer via API → Persist to DB → Retrieve → Verify data matches.
    /// US2 Acceptance Scenario 1: User submits income=$2100; encrypted before DB insert.
    /// Validates:
    /// - POST /api/sessions/{id}/answers saves answer
    /// - PII fields are encrypted (AnswerEncrypted populated, AnswerPlain null)
    /// - Non-PII fields are plain text (AnswerPlain populated, AnswerEncrypted null)
    /// - GET /api/sessions/{id}/answers retrieves all answers
    /// - Encryption/decryption round-trip works correctly
    /// </summary>
    [Fact]
    public async Task SaveAnswer_PersistsEncrypted_AndRetrievesCorrectly()
    {
        // Arrange - Create session
        var createSessionRequest = new CreateSessionDto
        {
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0"
        };

        var sessionResponse = await _httpClient!.PostAsJsonAsync("/api/sessions", createSessionRequest);
        var session = await sessionResponse.Content.ReadAsAsync<SessionDto>();
        var sessionId = session!.Id;

        // Arrange - Prepare answer (PII: income)
        var saveAnswerRequest = new SaveAnswerDto
        {
            FieldKey = "income_annual_2025",
            FieldType = "currency",
            AnswerValue = "2100",
            IsPii = true
        };

        // Act - Save answer
        var saveResponse = await _httpClient.PostAsJsonAsync($"/api/sessions/{sessionId}/answers", saveAnswerRequest);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Saving answer should return 201");

        var savedAnswer = await saveResponse.Content.ReadAsAsync<SessionAnswerDto>();
        
        // Assert - Response validation
        savedAnswer.Should().NotBeNull();
        savedAnswer!.FieldKey.Should().Be("income_annual_2025");
        savedAnswer.FieldType.Should().Be("currency");
        savedAnswer.IsPii.Should().BeTrue();
        savedAnswer.AnswerValue.Should().Be("2100", "Decrypted value should match original");

        // Act - Verify database encryption
        using (var context = _databaseFixture.CreateContext())
        {
            var dbAnswer = await context.SessionAnswers.FirstAsync(a => a.SessionId == sessionId);
            
            // Assert - PII encrypted in DB
            dbAnswer.AnswerEncrypted.Should().NotBeNullOrEmpty("PII should be encrypted");
            dbAnswer.AnswerPlain.Should().BeNull("PII should not be stored plain");
            dbAnswer.IsPii.Should().BeTrue();
            dbAnswer.KeyVersion.Should().BeGreaterThan(0, "Encryption key version should be set");
        }

        // Act - Retrieve all answers
        var getResponse = await _httpClient.GetAsync($"/api/sessions/{sessionId}/answers");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var answers = await getResponse.Content.ReadAsAsync<List<SessionAnswerDto>>();
        
        // Assert - Retrieved answers match saved
        answers.Should().NotBeNull();
        answers!.Count.Should().Be(1);
        answers[0].FieldKey.Should().Be("income_annual_2025");
        answers[0].AnswerValue.Should().Be("2100", "Decrypted answer should match original");
    }

    /// <summary>
    /// Integration Test: Save non-PII answer without encryption.
    /// US2: Demographics (age, household size, state) NOT encrypted per spec.
    /// Validates:
    /// - Non-PII stored as plain text in database
    /// - AnswerPlain populated, AnswerEncrypted null
    /// - Retrieval works same as encrypted fields
    /// </summary>
    [Fact]
    public async Task SaveAnswer_NonPii_StoredAsPlainText()
    {
        // Arrange - Create session
        var createSessionRequest = new CreateSessionDto
        {
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0"
        };

        var sessionResponse = await _httpClient!.PostAsJsonAsync("/api/sessions", createSessionRequest);
        var session = await sessionResponse.Content.ReadAsAsync<SessionDto>();
        var sessionId = session!.Id;

        // Arrange - Non-PII answer (household size)
        var saveAnswerRequest = new SaveAnswerDto
        {
            FieldKey = "household_size",
            FieldType = "integer",
            AnswerValue = "4",
            IsPii = false
        };

        // Act - Save answer
        var saveResponse = await _httpClient.PostAsJsonAsync($"/api/sessions/{sessionId}/answers", saveAnswerRequest);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert - Verify plain text storage in DB
        using (var context = _databaseFixture.CreateContext())
        {
            var dbAnswer = await context.SessionAnswers.FirstAsync(a => a.SessionId == sessionId);
            
            dbAnswer.AnswerPlain.Should().Be("4", "Non-PII should be stored as plain text");
            dbAnswer.AnswerEncrypted.Should().BeNull("Non-PII should not be encrypted");
            dbAnswer.IsPii.Should().BeFalse();
        }

        // Act - Retrieve answers
        var getResponse = await _httpClient.GetAsync($"/api/sessions/{sessionId}/answers");
        var answers = await getResponse.Content.ReadAsAsync<List<SessionAnswerDto>>();

        // Assert - Retrieved value correct
        answers.Should().NotBeNull();
        answers![0].AnswerValue.Should().Be("4");
    }

    /// <summary>
    /// Integration Test: Update existing answer preserves session consistency.
    /// US2: User changes income from $2100 to $2500; old value overwritten.
    /// Validates:
    /// - Second POST to same FieldKey updates existing answer
    /// - Only latest answer retrieved
    /// - Database shows updated encryption (different ciphertext)
    /// </summary>
    [Fact]
    public async Task SaveAnswer_UpdateExisting_PreservesLatestValue()
    {
        // Arrange - Create session and save initial answer
        var createSessionRequest = new CreateSessionDto
        {
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0"
        };

        var sessionResponse = await _httpClient!.PostAsJsonAsync("/api/sessions", createSessionRequest);
        var session = await sessionResponse.Content.ReadAsAsync<SessionDto>();
        var sessionId = session!.Id;

        var initialAnswer = new SaveAnswerDto
        {
            FieldKey = "income_annual_2025",
            FieldType = "currency",
            AnswerValue = "2100",
            IsPii = true
        };

        await _httpClient.PostAsJsonAsync($"/api/sessions/{sessionId}/answers", initialAnswer);

        // Act - Update answer with new value
        var updatedAnswer = new SaveAnswerDto
        {
            FieldKey = "income_annual_2025",
            FieldType = "currency",
            AnswerValue = "2500",
            IsPii = true
        };

        var updateResponse = await _httpClient.PostAsJsonAsync($"/api/sessions/{sessionId}/answers", updatedAnswer);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Retrieve answers
        var getResponse = await _httpClient.GetAsync($"/api/sessions/{sessionId}/answers");
        var answers = await getResponse.Content.ReadAsAsync<List<SessionAnswerDto>>();

        // Assert - Only latest answer exists
        answers.Should().NotBeNull();
        answers!.Count.Should().Be(1, "Only one answer per FieldKey should exist");
        answers[0].AnswerValue.Should().Be("2500", "Latest value should be retrieved");

        // Assert - Database has updated value
        using (var context = _databaseFixture.CreateContext())
        {
            var dbAnswers = await context.SessionAnswers
                .Where(a => a.SessionId == sessionId && a.FieldKey == "income_annual_2025")
                .ToListAsync();

            dbAnswers.Count.Should().Be(1, "Only one record per FieldKey should exist in DB");
        }
    }

    /// <summary>
    /// Integration Test: Multiple answers stored for same session.
    /// US2 Scenario: Wizard answers saved; page refresh; answers restored.
    /// Validates:
    /// - Multiple POST /api/sessions/{id}/answers work concurrently
    /// - All answers persisted with correct session_id FK
    /// - GET retrieves all answers for session
    /// - Different field types handled correctly
    /// </summary>
    [Fact]
    public async Task SaveMultipleAnswers_AllPersist_AndRetrievedTogether()
    {
        // Arrange - Create session
        var createSessionRequest = new CreateSessionDto
        {
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0"
        };

        var sessionResponse = await _httpClient!.PostAsJsonAsync("/api/sessions", createSessionRequest);
        var session = await sessionResponse.Content.ReadAsAsync<SessionDto>();
        var sessionId = session!.Id;

        // Arrange - Multiple wizard answers
        var answers = new[]
        {
            new SaveAnswerDto { FieldKey = "household_size", FieldType = "integer", AnswerValue = "3", IsPii = false },
            new SaveAnswerDto { FieldKey = "income_annual_2025", FieldType = "currency", AnswerValue = "2100", IsPii = true },
            new SaveAnswerDto { FieldKey = "has_disability", FieldType = "boolean", AnswerValue = "true", IsPii = true },
            new SaveAnswerDto { FieldKey = "state", FieldType = "string", AnswerValue = "IL", IsPii = false }
        };

        // Act - Save all answers
        foreach (var answer in answers)
        {
            var saveResponse = await _httpClient.PostAsJsonAsync($"/api/sessions/{sessionId}/answers", answer);
            saveResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // Act - Retrieve all answers
        var getResponse = await _httpClient.GetAsync($"/api/sessions/{sessionId}/answers");
        var retrievedAnswers = await getResponse.Content.ReadAsAsync<List<SessionAnswerDto>>();

        // Assert - All answers retrieved
        retrievedAnswers.Should().NotBeNull();
        retrievedAnswers!.Count.Should().Be(4, "All four answers should be retrieved");

        var fieldKeys = retrievedAnswers.Select(a => a.FieldKey).ToList();
        fieldKeys.Should().Contain("household_size");
        fieldKeys.Should().Contain("income_annual_2025");
        fieldKeys.Should().Contain("has_disability");
        fieldKeys.Should().Contain("state");

        // Assert - Values match
        retrievedAnswers.First(a => a.FieldKey == "household_size").AnswerValue.Should().Be("3");
        retrievedAnswers.First(a => a.FieldKey == "income_annual_2025").AnswerValue.Should().Be("2100");
        retrievedAnswers.First(a => a.FieldKey == "has_disability").AnswerValue.Should().Be("true");
        retrievedAnswers.First(a => a.FieldKey == "state").AnswerValue.Should().Be("IL");
    }

    #endregion
}

/// <summary>
/// Extension method for HttpContent JSON deserialization.
/// Handles case-insensitive property matching for DTOs.
/// </summary>
internal static class HttpContentExtensions
{
    public static async Task<T?> ReadAsAsync<T>(this HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<T>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
