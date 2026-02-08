using FluentAssertions;
using MAA.Domain.Sessions;
using Xunit;

namespace MAA.Tests.Unit;

/// <summary>
/// Unit tests for Session domain entity validation and business logic.
/// Tests cover: validation rules, state transitions, versioning, expiration checks.
/// </summary>
[Trait("Category", "Unit")]
public class SessionTests
{
    [Fact]
    public void NewSession_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var session = new Session();

        // Assert
        session.Id.Should().Be(Guid.Empty);
        session.State.Should().Be(SessionState.Pending);
        session.IsRevoked.Should().BeFalse();
        session.Version.Should().Be(0);
    }

    [Fact]
    public void Session_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var session = new Session
        {
            Id = sessionId,
            State = SessionState.InProgress,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            ExpiresAt = now.AddHours(1),
            InactivityTimeoutAt = now.AddMinutes(30),
            LastActivityAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };

        // Assert
        session.Id.Should().Be(sessionId);
        session.State.Should().Be(SessionState.InProgress);
        session.IpAddress.Should().Be("192.168.1.1");
        session.UserAgent.Should().Be("Mozilla/5.0");
        session.Version.Should().Be(1);
    }

    #region Validation Tests

    [Fact]
    public void Validate_WithEmptyId_ShouldThrowException()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.Empty,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Act
        var action = () => session.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Session ID cannot be empty");
    }

    [Fact]
    public void Validate_WithExpiredExpiresAt_ShouldThrowException()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Act
        var action = () => session.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Session ExpiresAt must be in the future");
    }

    [Fact]
    public void Validate_WithExpiredInactivityTimeout_ShouldThrowException()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(-5), // Expired
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Act
        var action = () => session.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Session InactivityTimeoutAt must be in the future");
    }

    [Fact]
    public void Validate_WithNegativeVersion_ShouldThrowException()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            Version = -1
        };

        // Act
        var action = () => session.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Version cannot be negative");
    }

    [Fact]
    public void Validate_WithEmptyIpAddress_ShouldThrowException()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "",
            UserAgent = "Mozilla/5.0"
        };

        // Act
        var action = () => session.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("IpAddress is required");
    }

    [Fact]
    public void Validate_WithWhitespaceIpAddress_ShouldThrowException()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "   ",
            UserAgent = "Mozilla/5.0"
        };

        // Act
        var action = () => session.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("IpAddress is required");
    }

    [Fact]
    public void Validate_WithEmptyUserAgent_ShouldThrowException()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = ""
        };

        // Act
        var action = () => session.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("UserAgent is required");
    }

    [Fact]
    public void Validate_CompleteValidSession_ShouldPass()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            Version = 0
        };

        // Act
        var action = () => session.Validate();

        // Assert
        action.Should().NotThrow();
    }

    #endregion

    #region State Transition Tests

    [Fact]
    public void CanTransitionTo_FromPendingToInProgress_ShouldReturnTrue()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Act
        var result = session.CanTransitionTo(SessionState.InProgress);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanTransitionTo_FromPendingToCompleted_ShouldReturnFalse()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Act
        var result = session.CanTransitionTo(SessionState.Completed);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TransitionTo_ValidTransition_ShouldUpdateStateAndVersion()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            Version = 0
        };

        // Act
        session.TransitionTo(SessionState.InProgress);

        // Assert
        session.State.Should().Be(SessionState.InProgress);
        session.Version.Should().Be(1);
    }

    [Fact]
    public void TransitionTo_InvalidTransition_ShouldThrowException()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Act
        var action = () => session.TransitionTo(SessionState.Completed);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid state transition*");
    }

    [Fact]
    public void TransitionTo_ToAbandoned_ShouldSucceedFromAnyState()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.InProgress,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Act
        var action = () => session.TransitionTo(SessionState.Abandoned);

        // Assert
        action.Should().NotThrow();
        session.State.Should().Be(SessionState.Abandoned);
    }

    [Fact]
    public void TransitionTo_CompleteWorkflow_ShouldSucceed()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            Version = 0
        };

        // Act & Assert - Pending → InProgress
        session.TransitionTo(SessionState.InProgress);
        session.State.Should().Be(SessionState.InProgress);
        session.Version.Should().Be(1);

        // Act & Assert - InProgress → Submitted
        session.TransitionTo(SessionState.Submitted);
        session.State.Should().Be(SessionState.Submitted);
        session.Version.Should().Be(2);

        // Act & Assert - Submitted → Completed
        session.TransitionTo(SessionState.Completed);
        session.State.Should().Be(SessionState.Completed);
        session.Version.Should().Be(3);
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void IsValid_WhenNotRevokedAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.InProgress,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            IsRevoked = false
        };

        // Act
        var result = session.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenRevoked_ShouldReturnFalse()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.InProgress,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            IsRevoked = true
        };

        // Act
        var result = session.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.InProgress,
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            IsRevoked = false
        };

        // Act
        var result = session.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenInactive_ShouldReturnFalse()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.InProgress,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(-5),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            IsRevoked = false
        };

        // Act
        var result = session.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenAbandoned_ShouldReturnFalse()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.Abandoned,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            IsRevoked = false
        };

        // Act
        var result = session.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ResetInactivityTimeout Tests

    [Fact]
    public void ResetInactivityTimeout_ShouldUpdateTimestampsAndVersion()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.InProgress,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            LastActivityAt = DateTime.UtcNow.AddMinutes(-10),
            Version = 0
        };
        var beforeReset = DateTime.UtcNow;

        // Act
        session.ResetInactivityTimeout(30);

        // Assert
        session.InactivityTimeoutAt.Should().BeOnOrAfter(beforeReset.AddMinutes(30));
        session.LastActivityAt.Should().BeOnOrAfter(beforeReset);
        session.Version.Should().Be(1);
    }

    [Fact]
    public void ResetInactivityTimeout_WithDifferentTimeout_ShouldWork()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.InProgress,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(30),
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            Version = 0
        };
        var beforeReset = DateTime.UtcNow;

        // Act
        session.ResetInactivityTimeout(15);

        // Assert
        session.InactivityTimeoutAt.Should().BeCloseTo(beforeReset.AddMinutes(15), TimeSpan.FromSeconds(1));
        session.Version.Should().Be(1);
    }

    #endregion
}
