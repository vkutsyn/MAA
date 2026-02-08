using FluentAssertions;
using MAA.Domain.Sessions;
using Xunit;

namespace MAA.Tests.Unit;

/// <summary>
/// Unit tests for SessionDataSchema validation and serialization.
/// Tests T13: JSONB schema validation requirements.
/// </summary>
public class SessionDataSchemaTests
{
    [Fact]
    public void Validate_WithValidJson_ShouldPass()
    {
        // Arrange
        var json = @"{
            ""metadata"": {
                ""version"": 1,
                ""createdBy"": ""wizard"",
                ""timeoutMinutes"": 30
            }
        }";

        // Act
        var result = SessionDataSchema.Validate(json);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyJson_ShouldFail()
    {
        // Arrange
        var json = "";

        // Act
        var result = SessionDataSchema.Validate(json);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("cannot be null or empty"));
    }

    [Fact]
    public void Validate_WithMalformedJson_ShouldFail()
    {
        // Arrange
        var json = @"{ ""metadata"": { ""version"": 1, ";

        // Act
        var result = SessionDataSchema.Validate(json);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid JSON format"));
    }

    [Fact]
    public void Validate_WithNegativeTimeout_ShouldFail()
    {
        // Arrange
        var json = @"{
            ""metadata"": {
                ""version"": 1,
                ""timeoutMinutes"": -10
            }
        }";

        // Act
        var result = SessionDataSchema.Validate(json);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("TimeoutMinutes must be greater than 0"));
    }

    [Fact]
    public void Validate_WithNegativeInactivity_ShouldFail()
    {
        // Arrange
        var json = @"{
            ""metadata"": {
                ""version"": 1,
                ""maxInactivityMinutes"": -5
            }
        }";

        // Act
        var result = SessionDataSchema.Validate(json);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxInactivityMinutes must be greater than 0"));
    }

    [Fact]
    public void CreateEmpty_ShouldReturnValidJson()
    {
        // Act
        var json = SessionDataSchema.CreateEmpty();

        // Assert
        json.Should().NotBeNullOrEmpty();
        var validation = SessionDataSchema.Validate(json);
        validation.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Parse_WithValidJson_ShouldReturnSessionData()
    {
        // Arrange
        var json = @"{
            ""metadata"": {
                ""version"": 1,
                ""createdBy"": ""api"",
                ""timeoutMinutes"": 60,
                ""tags"": [""anonymous"", ""wizard""]
            },
            ""properties"": {
                ""referrer"": ""google.com""
            }
        }";

        // Act
        var data = SessionDataSchema.Parse(json);

        // Assert
        data.Should().NotBeNull();
        data!.Metadata.Should().NotBeNull();
        data.Metadata!.Version.Should().Be(1);
        data.Metadata.CreatedBy.Should().Be("api");
        data.Metadata.TimeoutMinutes.Should().Be(60);
        data.Metadata.Tags.Should().Contain("anonymous");
        data.Metadata.Tags.Should().Contain("wizard");
        data.Properties.Should().ContainKey("referrer");
    }

    [Fact]
    public void Parse_WithInvalidJson_ShouldReturnNull()
    {
        // Arrange
        var json = @"{ invalid json }";

        // Act
        var data = SessionDataSchema.Parse(json);

        // Assert
        data.Should().BeNull();
    }

    [Fact]
    public void Serialize_WithSessionData_ShouldProduceValidJson()
    {
        // Arrange
        var data = new SessionData
        {
            Metadata = new SessionMetadata
            {
                Version = 2,
                CreatedBy = "test",
                TimeoutMinutes = 45,
                DeviceId = "device-123",
                Tags = new List<string> { "test", "unit" }
            },
            Properties = new Dictionary<string, object>
            {
                { "customKey", "customValue" }
            }
        };

        // Act
        var json = SessionDataSchema.Serialize(data);

        // Assert
        json.Should().NotBeNullOrEmpty();
        var parsed = SessionDataSchema.Parse(json);
        parsed.Should().NotBeNull();
        parsed!.Metadata!.Version.Should().Be(2);
        parsed.Metadata.CreatedBy.Should().Be("test");
    }

    [Fact]
    public void Serialize_WithNullValues_ShouldOmitNullFields()
    {
        // Arrange
        var data = new SessionData
        {
            Metadata = new SessionMetadata
            {
                Version = 1,
                CreatedBy = "system"
                // Other fields null
            }
        };

        // Act
        var json = SessionDataSchema.Serialize(data);

        // Assert
        json.Should().NotContain("timeoutMinutes");
        json.Should().NotContain("deviceId");
        json.Should().NotContain("tags");
    }

    [Fact]
    public void RoundTrip_SerializeAndParse_ShouldPreserveData()
    {
        // Arrange
        var original = new SessionData
        {
            Metadata = new SessionMetadata
            {
                Version = 3,
                CreatedBy = "roundtrip-test",
                TimeoutMinutes = 120,
                MaxInactivityMinutes = 30,
                DeviceId = "rt-device-456",
                Tags = new List<string> { "test1", "test2" },
                Notes = "Test notes"
            },
            Properties = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 }
            }
        };

        // Act
        var json = SessionDataSchema.Serialize(original);
        var parsed = SessionDataSchema.Parse(json);

        // Assert
        parsed.Should().NotBeNull();
        parsed!.Metadata.Should().NotBeNull();
        parsed.Metadata!.Version.Should().Be(original.Metadata!.Version);
        parsed.Metadata.CreatedBy.Should().Be(original.Metadata.CreatedBy);
        parsed.Metadata.TimeoutMinutes.Should().Be(original.Metadata.TimeoutMinutes);
        parsed.Metadata.MaxInactivityMinutes.Should().Be(original.Metadata.MaxInactivityMinutes);
        parsed.Metadata.DeviceId.Should().Be(original.Metadata.DeviceId);
        parsed.Metadata.Tags.Should().BeEquivalentTo(original.Metadata.Tags);
        parsed.Metadata.Notes.Should().Be(original.Metadata.Notes);
        parsed.Properties.Should().ContainKey("key1");
        parsed.Properties.Should().ContainKey("key2");
    }

    [Fact]
    public void Validate_WithMissingRequiredFields_ShouldStillPass()
    {
        // Arrange - metadata and properties are optional
        var json = @"{}";

        // Act
        var result = SessionDataSchema.Validate(json);

        // Assert
        result.IsValid.Should().BeTrue("empty object should be valid as all fields are optional");
    }

    [Fact]
    public void ValidationResult_GetErrorMessage_ShouldFormatMultipleErrors()
    {
        // Arrange
        var json = @"{
            ""metadata"": {
                ""timeoutMinutes"": -1,
                ""maxInactivityMinutes"": 0
            }
        }";

        // Act
        var result = SessionDataSchema.Validate(json);

        // Assert
        result.IsValid.Should().BeFalse();
        var errorMessage = result.GetErrorMessage();
        errorMessage.Should().Contain("TimeoutMinutes");
        errorMessage.Should().Contain("MaxInactivityMinutes");
        errorMessage.Should().Contain(";");
    }
}
