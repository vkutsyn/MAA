using FluentAssertions;
using MAA.Application.Services;
using MAA.Domain.Repositories;
using MAA.Domain.Sessions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MAA.Tests.Unit.Sessions;

/// <summary>
/// Unit tests for SessionService business logic.
/// Tests session creation, validation, timeout handling, and state transitions.
/// Uses mocked dependencies (ISessionRepository, IEncryptionService, ILogger).
/// Does NOT access database - all data is mocked.
/// </summary>
public class SessionServiceTests
{
    // CONST-III: Exact error message for session expiration
    private const string SessionExpiredMessage = "Your session expired after 30 minutes. Start a new eligibility check.";
    
    // Session timeout settings
    private const int AnonymousSessionTimeoutMinutes = 30;
    private const int InactivityTimeoutMinutes = 15;

    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<ILogger<SessionService>> _mockLogger;
    private readonly ISessionService _sessionService;

    public SessionServiceTests()
    {
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockLogger = new Mock<ILogger<SessionService>>();
        
        // Initialize SessionService with mocked dependencies
        _sessionService = new SessionService(
            _mockSessionRepository.Object,
            _mockEncryptionService.Object,
            _mockLogger.Object
        );
    }

    #region CreateSessionAsync Tests

    /// <summary>
    /// Test: CreateSessionAsync creates new anonymous session with correct timeout values.
    /// Verifies: Session state is Pending, timeouts are calculated correctly, encryption key is set.
    /// </summary>
    [Fact]
    public async Task CreateSessionAsync_Creates_AnonymousSession_WithCorrectTimeouts()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        var beforeCall = DateTime.UtcNow;

        var createdSession = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.Pending,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{}",
            ExpiresAt = DateTime.UtcNow.AddMinutes(AnonymousSessionTimeoutMinutes),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(InactivityTimeoutMinutes),
            LastActivityAt = DateTime.UtcNow,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        var afterCall = DateTime.UtcNow.AddMinutes(AnonymousSessionTimeoutMinutes);

        _mockSessionRepository
            .Setup(x => x.CreateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdSession);

        // Act
        var result = await _sessionService.CreateSessionAsync(ipAddress, userAgent);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty("Session should have a generated ID");
        result.State.Should().Be(SessionState.Pending, "New session should be in Pending state");
        result.IpAddress.Should().Be(ipAddress, "Session should store client IP address");
        result.UserAgent.Should().Be(userAgent, "Session should store user agent");
        result.SessionType.Should().Be("anonymous", "Session should be marked as anonymous");
        result.EncryptionKeyVersion.Should().Be(1, "Session should have encryption key version");
        result.IsRevoked.Should().BeFalse("New session should not be revoked");
        
        // Verify timeout is approximately 30 minutes from now (allow 1 second variance)
        result.ExpiresAt.Should()
            .BeCloseTo(createdSession.ExpiresAt, TimeSpan.FromSeconds(1),
                "ExpiresAt should be 30 minutes from creation");

        // Verify repository was called exactly once
        _mockSessionRepository.Verify(
            x => x.CreateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Repository CreateAsync should be called exactly once");
    }

    /// <summary>
    /// Test: CreateSessionAsync initializes encrypted session data as empty JSONB.
    /// Verifies: Data property is set to empty JSON object "{}".
    /// </summary>
    [Fact]
    public async Task CreateSessionAsync_Initializes_SessionData_AsEmptyJson()
    {
        // Arrange
        var ipAddress = "10.0.0.50";
        var userAgent = "Chrome/120.0";

        var createdSession = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.Pending,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{}",
            ExpiresAt = DateTime.UtcNow.AddMinutes(AnonymousSessionTimeoutMinutes),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(InactivityTimeoutMinutes),
            LastActivityAt = DateTime.UtcNow,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        _mockSessionRepository
            .Setup(x => x.CreateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdSession);

        // Act
        var result = await _sessionService.CreateSessionAsync(ipAddress, userAgent);

        // Assert
        result.Data.Should().Be("{}", "Session data should be initialized as empty JSON");
    }

    #endregion

    #region ValidateSessionAsync Tests

    /// <summary>
    /// Test: ValidateSessionAsync returns true for valid (non-expired, non-revoked) session.
    /// Verifies: Session within both absolute expiry and inactivity timeout returns true.
    /// </summary>
    [Fact]
    public async Task ValidateSessionAsync_ValidSession_ReturnsTrue()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var validSession = new Session
        {
            Id = sessionId,
            State = SessionState.InProgress,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0",
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{}",
            ExpiresAt = DateTime.UtcNow.AddMinutes(20), // Expires in 20 minutes
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(10), // Inactive timeout in 10 minutes
            LastActivityAt = DateTime.UtcNow.AddMinutes(-5), // Last activity 5 minutes ago
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        _mockSessionRepository
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validSession);

        // Act
        var result = await _sessionService.ValidateSessionAsync(sessionId);

        // Assert
        result.Should().BeTrue("Valid, non-expired, non-revoked session should pass validation");

        _mockSessionRepository.Verify(
            x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()),
            Times.Once,
            "Repository GetByIdAsync should be called exactly once");
    }

    /// <summary>
    /// Test: ValidateSessionAsync returns false when session is expired (absolute timeout).
    /// Verifies: Session past ExpiresAt returns false.
    /// </summary>
    [Fact]
    public async Task ValidateSessionAsync_ExpiredSession_ReturnsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expiredSession = new Session
        {
            Id = sessionId,
            State = SessionState.InProgress,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0",
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{}",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5), // Expired 5 minutes ago
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(10), // Inactivity timeout still valid
            LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-40),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5),
            Version = 1
        };

        _mockSessionRepository
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredSession);

        // Act
        var result = await _sessionService.ValidateSessionAsync(sessionId);

        // Assert
        result.Should().BeFalse("Session past ExpiresAt should fail validation (30-minute absolute timeout expired)");
    }

    /// <summary>
    /// Test: ValidateSessionAsync returns false when session has inactive timeout.
    /// Verifies: Session past InactivityTimeoutAt returns false (sliding window).
    /// </summary>
    [Fact]
    public async Task ValidateSessionAsync_InactiveSession_ReturnsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var inactiveSession = new Session
        {
            Id = sessionId,
            State = SessionState.InProgress,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0",
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{}",
            ExpiresAt = DateTime.UtcNow.AddMinutes(20), // Absolute expiry still valid
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(-2), // Inactivity timeout expired 2 minutes ago
            LastActivityAt = DateTime.UtcNow.AddMinutes(-20), // No activity for 20 minutes
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-25),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-20),
            Version = 1
        };

        _mockSessionRepository
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveSession);

        // Act
        var result = await _sessionService.ValidateSessionAsync(sessionId);

        // Assert
        result.Should().BeFalse("Session past InactivityTimeoutAt should fail validation (sliding window timeout expired)");
    }

    /// <summary>
    /// Test: ValidateSessionAsync returns false when session is revoked.
    /// Verifies: Revoked session returns false regardless of timeout state.
    /// </summary>
    [Fact]
    public async Task ValidateSessionAsync_RevokedSession_ReturnsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var revokedSession = new Session
        {
            Id = sessionId,
            State = SessionState.InProgress,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0",
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{}",
            ExpiresAt = DateTime.UtcNow.AddMinutes(20), // Still valid
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(10), // Still valid
            LastActivityAt = DateTime.UtcNow,
            IsRevoked = true, // Explicitly revoked
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        _mockSessionRepository
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedSession);

        // Act
        var result = await _sessionService.ValidateSessionAsync(sessionId);

        // Assert
        result.Should().BeFalse("Revoked session should fail validation");
    }

    /// <summary>
    /// Test: ValidateSessionAsync returns false when session not found.
    /// Verifies: Non-existent session ID returns false.
    /// </summary>
    [Fact]
    public async Task ValidateSessionAsync_NonexistentSession_ReturnsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mockSessionRepository
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session?)null);

        // Act
        var result = await _sessionService.ValidateSessionAsync(sessionId);

        // Assert
        result.Should().BeFalse("Non-existent session should fail validation");
    }

    #endregion

    #region GetSessionTimeoutMessageAsync Tests

    /// <summary>
    /// Test: GetSessionTimeoutMessageAsync returns CONST-III compliant error message.
    /// Verifies: Message is exactly "Your session expired after 30 minutes. Start a new eligibility check."
    /// This test ensures the UX consistency constraint CONST-III is met throughout the system.
    /// </summary>
    [Fact]
    public async Task GetSessionTimeoutMessageAsync_ReturnsConstIiiMessage()
    {
        // Act
        var result = await _sessionService.GetSessionTimeoutMessageAsync();

        // Assert
        result.Should().Be(SessionExpiredMessage,
            "Session timeout message must match CONST-III requirement exactly: " +
            "\"Your session expired after 30 minutes. Start a new eligibility check.\"");
    }

    /// <summary>
    /// Test: Session timeout message is consistent across all validation failures.
    /// Verifies: Message returned when session expires absolutely is CONST-III compliant.
    /// </summary>
    [Fact]
    public async Task ValidateSessionAsync_ExpiredSession_CanRetrieveConstIiiMessage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expiredSession = new Session
        {
            Id = sessionId,
            State = SessionState.InProgress,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0",
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{}",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(10),
            LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-40),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5),
            Version = 1
        };

        _mockSessionRepository
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredSession);

        // Act
        var isValid = await _sessionService.ValidateSessionAsync(sessionId);
        var timeoutMessage = await _sessionService.GetSessionTimeoutMessageAsync();

        // Assert
        isValid.Should().BeFalse("Session should be expired");
        timeoutMessage.Should().Be(SessionExpiredMessage,
            "Timeout message for expired session must be CONST-III compliant");
    }

    /// <summary>
    /// Test: Session timeout message is consistent for inactive sessions.
    /// Verifies: Message returned when session becomes inactive is CONST-III compliant.
    /// </summary>
    [Fact]
    public async Task ValidateSessionAsync_InactiveSession_CanRetrieveConstIiiMessage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var inactiveSession = new Session
        {
            Id = sessionId,
            State = SessionState.InProgress,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0",
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{}",
            ExpiresAt = DateTime.UtcNow.AddMinutes(20),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(-2),
            LastActivityAt = DateTime.UtcNow.AddMinutes(-20),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-25),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-20),
            Version = 1
        };

        _mockSessionRepository
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveSession);

        // Act
        var isValid = await _sessionService.ValidateSessionAsync(sessionId);
        var timeoutMessage = await _sessionService.GetSessionTimeoutMessageAsync();

        // Assert
        isValid.Should().BeFalse("Session should be inactive");
        timeoutMessage.Should().Be(SessionExpiredMessage,
            "Timeout message for inactive session must be CONST-III compliant");
    }

    #endregion

    #region GetSessionAsync Tests

    /// <summary>
    /// Test: GetSessionAsync returns valid session when it exists and passes validation.
    /// Verifies: Valid session is retrieved with all properties intact.
    /// </summary>
    [Fact]
    public async Task GetSessionAsync_ValidSession_ReturnsSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var validSession = new Session
        {
            Id = sessionId,
            State = SessionState.InProgress,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0",
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{\"answers\": []}",
            ExpiresAt = DateTime.UtcNow.AddMinutes(20),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(10),
            LastActivityAt = DateTime.UtcNow,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        _mockSessionRepository
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validSession);

        // Act
        var result = await _sessionService.GetSessionAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(sessionId);
        result.State.Should().Be(SessionState.InProgress);
        result.Data.Should().Be("{\"answers\": []}");
    }

    /// <summary>
    /// Test: GetSessionAsync throws InvalidOperationException when session is expired.
    /// Verifies: Expired sessions cannot be retrieved.
    /// </summary>
    [Fact]
    public async Task GetSessionAsync_ExpiredSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expiredSession = new Session
        {
            Id = sessionId,
            State = SessionState.InProgress,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0",
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{}",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(10),
            LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-40),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5),
            Version = 1
        };

        _mockSessionRepository
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredSession);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sessionService.GetSessionAsync(sessionId));
    }

    #endregion

    #region TransitionStateAsync Tests

    /// <summary>
    /// Test: TransitionStateAsync moves session from Pending to InProgress.
    /// Verifies: State machine validates and applies transitions.
    /// </summary>
    [Fact]
    public async Task TransitionStateAsync_PendingToInProgress_Succeeds()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var sessionInProgress = new Session
        {
            Id = sessionId,
            State = SessionState.InProgress,
            IpAddress = "192.168.1.100",
            UserAgent = "Chrome/120.0",
            SessionType = "anonymous",
            EncryptionKeyVersion = 1,
            Data = "{}",
            ExpiresAt = DateTime.UtcNow.AddMinutes(20),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(10),
            LastActivityAt = DateTime.UtcNow,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        _mockSessionRepository
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionInProgress);

        _mockSessionRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionInProgress);

        // Act
        var result = await _sessionService.TransitionStateAsync(
            sessionId,
            SessionState.InProgress);

        // Assert
        result.State.Should().Be(SessionState.InProgress);
    }

    #endregion
}
