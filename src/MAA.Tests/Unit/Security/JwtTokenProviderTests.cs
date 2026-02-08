using FluentAssertions;
using MAA.Application.Services;
using MAA.Domain.Sessions;
using MAA.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace MAA.Tests.Unit.Security;

/// <summary>
/// Unit tests for JwtTokenProvider (Phase 5 feature).
/// Tests JWT token generation, validation, claims extraction, and token refresh logic.
/// Does NOT access database - all operations are cryptographic.
/// </summary>
public class JwtTokenProviderTests
{
    private readonly Mock<ILogger<JwtTokenProvider>> _mockLogger;
    private readonly JwtTokenProvider _tokenProvider;
    
    // Token expiration settings
    private const int AccessTokenExpirationMinutes = 60;
    private const int RefreshTokenExpirationDays = 7;
    
    // Test data
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly List<string> _testRoles = new() { UserRole.User.ToString(), UserRole.Analyst.ToString() };

    public JwtTokenProviderTests()
    {
        _mockLogger = new Mock<ILogger<JwtTokenProvider>>();
        
        // Initialize JwtTokenProvider with test configuration
        // In real implementation, this would use IOptions<JwtSettings> from config
        _tokenProvider = new JwtTokenProvider(_mockLogger.Object);
    }

    #region GenerateAccessTokenAsync Tests

    /// <summary>
    /// Test: GenerateAccessTokenAsync creates a valid JWT access token with correct claims.
    /// Verifies: Token contains user ID, roles, and correct expiration (1 hour).
    /// </summary>
    [Fact]
    public async Task GenerateAccessTokenAsync_GeneratesValidToken_WithCorrectClaims()
    {
        // Act
        var token = await _tokenProvider.GenerateAccessTokenAsync(_testUserId, _testRoles);

        // Assert
        token.Should().NotBeNullOrEmpty("Access token should be generated");
        
        // Parse token (without validation, for unit test)
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        // Verify claims
        jwtToken.Claims.Should().Contain(c => c.Type == "sub" && c.Value == _testUserId.ToString(),
            "Token should contain user ID (sub) claim");
        jwtToken.Claims.Should().Contain(c => c.Type == "role",
            "Token should contain role claim");
        
        // Verify expiration is approximately 1 hour from now
        var expirationTime = jwtToken.ValidTo;
        expirationTime.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes),
            TimeSpan.FromMinutes(1), "Access token should expire in 1 hour");
    }

    /// <summary>
    /// Test: GenerateAccessTokenAsync includes all user roles in token.
    /// Verifies: Multiple roles are encoded in role claim array.
    /// </summary>
    [Fact]
    public async Task GenerateAccessTokenAsync_IncludesAllRoles_InTokenClaims()
    {
        // Arrange
        var roles = new[] { UserRole.User.ToString(), UserRole.Admin.ToString() };

        // Act
        var token = await _tokenProvider.GenerateAccessTokenAsync(_testUserId, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var roleClaims = jwtToken.Claims.Where(c => c.Type == "role").ToList();
        roleClaims.Should().HaveCount(roles.Length, "Token should contain all role claims");
        roleClaims.Should().OnlyContain(c => roles.Contains(c.Value),
            "Token should only contain provided roles");
    }

    /// <summary>
    /// Test: GenerateAccessTokenAsync with empty roles list still generates valid token.
    /// Verifies: Token can be generated for users without explicit roles.
    /// </summary>
    [Fact]
    public async Task GenerateAccessTokenAsync_WithEmptyRoles_GeneratesValidToken()
    {
        // Arrange
        var emptyRoles = Array.Empty<string>();

        // Act
        var token = await _tokenProvider.GenerateAccessTokenAsync(_testUserId, emptyRoles);

        // Assert
        token.Should().NotBeNullOrEmpty("Token should be generated even with no roles");
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var roleClaims = jwtToken.Claims.Where(c => c.Type == "role").ToList();
        roleClaims.Should().BeEmpty("Token should have no role claims when roles list is empty");
    }

    #endregion

    #region GenerateRefreshTokenAsync Tests

    /// <summary>
    /// Test: GenerateRefreshTokenAsync creates a valid JWT refresh token with 7-day expiration.
    /// Verifies: Token contains user ID and expires in 7 days.
    /// </summary>
    [Fact]
    public async Task GenerateRefreshTokenAsync_GeneratesValidToken_WithSevenDayExpiration()
    {
        // Act
        var token = await _tokenProvider.GenerateRefreshTokenAsync(_testUserId);

        // Assert
        token.Should().NotBeNullOrEmpty("Refresh token should be generated");
        
        // Parse token
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        // Verify user ID claim
        jwtToken.Claims.Should().Contain(c => c.Type == "sub" && c.Value == _testUserId.ToString(),
            "Refresh token should contain user ID (sub) claim");
        
        // Verify expiration is approximately 7 days from now
        var expirationTime = jwtToken.ValidTo;
        expirationTime.Should().BeCloseTo(DateTime.UtcNow.AddDays(RefreshTokenExpirationDays),
            TimeSpan.FromHours(1), "Refresh token should expire in 7 days");
    }

    #endregion

    #region ValidateTokenAsync Tests

    /// <summary>
    /// Test: ValidateTokenAsync returns true for valid token.
    /// Verifies: Token signature and expiration are validated correctly.
    /// </summary>
    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var token = await _tokenProvider.GenerateAccessTokenAsync(_testUserId, _testRoles);

        // Act
        var isValid = await _tokenProvider.ValidateTokenAsync(token);

        // Assert
        isValid.Should().BeTrue("Valid token should pass validation");
    }

    /// <summary>
    /// Test: ValidateTokenAsync returns false for invalid token format.
    /// Verifies: Malformed tokens are rejected.
    /// </summary>
    [Fact]
    public async Task ValidateTokenAsync_WithMalformedToken_ReturnsFalse()
    {
        // Arrange
        var invalidToken = "not.a.valid.jwt.token";

        // Act
        var isValid = await _tokenProvider.ValidateTokenAsync(invalidToken);

        // Assert
        isValid.Should().BeFalse("Invalid token format should fail validation");
    }

    /// <summary>
    /// Test: ValidateTokenAsync returns false for expired token.
    /// Verifies: Expired tokens are rejected.
    /// </summary>
    [Fact]
    public async Task ValidateTokenAsync_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        // Create a token that's already expired (would be done by mocking time or using a test token)
        // This test requires token generation with custom expiration
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE1MTYyMzkwMjJ9.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        // Act
        var isValid = await _tokenProvider.ValidateTokenAsync(expiredToken);

        // Assert
        isValid.Should().BeFalse("Expired token should fail validation");
    }

    #endregion

    #region GetUserIdFromToken Tests

    /// <summary>
    /// Test: GetUserIdFromToken extracts user ID from valid token.
    /// Verifies: Correct claim is extracted and parsed.
    /// </summary>
    [Fact]
    public async Task GetUserIdFromToken_WithValidToken_ReturnsCorrectUserId()
    {
        // Arrange
        var token = await _tokenProvider.GenerateAccessTokenAsync(_testUserId, _testRoles);

        // Act
        var extractedUserId = _tokenProvider.GetUserIdFromToken(token);

        // Assert
        extractedUserId.Should().Be(_testUserId, "Should extract correct user ID from token");
    }

    /// <summary>
    /// Test: GetUserIdFromToken throws exception for invalid token.
    /// Verifies: Invalid tokens are rejected gracefully.
    /// </summary>
    [Fact]
    public void GetUserIdFromToken_WithInvalidToken_ThrowsException()
    {
        // Arrange
        var invalidToken = "not.a.valid.jwt";

        // Act & Assert
        var action = () => _tokenProvider.GetUserIdFromToken(invalidToken);
        action.Should().Throw<Exception>("Invalid token should throw exception");
    }

    #endregion

    #region GetRolesFromToken Tests

    /// <summary>
    /// Test: GetRolesFromToken extracts all roles from valid token.
    /// Verifies: All role claims are extracted correctly.
    /// </summary>
    [Fact]
    public async Task GetRolesFromToken_WithValidToken_ReturnsAllRoles()
    {
        // Arrange
        var expectedRoles = new[] { UserRole.User.ToString(), UserRole.Admin.ToString() };
        var token = await _tokenProvider.GenerateAccessTokenAsync(_testUserId, expectedRoles);

        // Act
        var extractedRoles = _tokenProvider.GetRolesFromToken(token).ToList();

        // Assert
        extractedRoles.Should().HaveCount(2, "Should extract all roles from token");
        extractedRoles.Should().Contain(expectedRoles, "Should extract correct roles");
    }

    /// <summary>
    /// Test: GetRolesFromToken returns empty list when token has no roles.
    /// Verifies: Tokens without role claims are handled gracefully.
    /// </summary>
    [Fact]
    public async Task GetRolesFromToken_WithNoRoles_ReturnsEmptyList()
    {
        // Arrange
        var emptyRoles = Array.Empty<string>();
        var token = await _tokenProvider.GenerateAccessTokenAsync(_testUserId, emptyRoles);

        // Act
        var extractedRoles = _tokenProvider.GetRolesFromToken(token).ToList();

        // Assert
        extractedRoles.Should().BeEmpty("Should return empty list when token has no roles");
    }

    #endregion

    #region Token Refresh Logic Tests

    /// <summary>
    /// Test: GenerateAccessTokenAsync creates new token with latest user info.
    /// Verifies: Refresh token flow can generate new access token with updated roles.
    /// </summary>
    [Fact]
    public async Task TokenRefreshFlow_GeneratesNewAccessToken_WithUpdatedRoles()
    {
        // Arrange
        var initialToken = await _tokenProvider.GenerateAccessTokenAsync(_testUserId, new[] { UserRole.User.ToString() });
        var updatedRoles = new[] { UserRole.User.ToString(), UserRole.Analyst.ToString() };

        // Act
        var newToken = await _tokenProvider.GenerateAccessTokenAsync(_testUserId, updatedRoles);

        // Assert
        newToken.Should().NotBe(initialToken, "New token should be different from initial");
        
        var handler = new JwtSecurityTokenHandler();
        var newJwt = handler.ReadJwtToken(newToken);
        
        var newRoles = newJwt.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();
        newRoles.Should().HaveCount(2, "New token should have updated roles");
        newRoles.Should().Contain(UserRole.Analyst.ToString(), "New token should include Analyst role");
    }

    #endregion

    #region Edge Cases Tests

    /// <summary>
    /// Test: GenerateAccessTokenAsync with very long role list.
    /// Verifies: Token can handle reasonable number of roles.
    /// </summary>
    [Fact]
    public async Task GenerateAccessTokenAsync_WithManyRoles_GeneratesValidToken()
    {
        // Arrange
        var manyRoles = new[] { 
            UserRole.User.ToString(), 
            UserRole.Analyst.ToString(), 
            UserRole.Reviewer.ToString(), 
            UserRole.Admin.ToString() 
        };

        // Act
        var token = await _tokenProvider.GenerateAccessTokenAsync(_testUserId, manyRoles);

        // Assert
        token.Should().NotBeNullOrEmpty("Token should be generated with many roles");
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var roleClaims = jwtToken.Claims.Where(c => c.Type == "role").ToList();
        roleClaims.Should().HaveCount(manyRoles.Length);
    }

    #endregion
}
