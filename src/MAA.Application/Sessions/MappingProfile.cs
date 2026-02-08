using AutoMapper;
using MAA.Application.Sessions.DTOs;
using MAA.Domain.Sessions;

namespace MAA.Application.Sessions;

/// <summary>
/// AutoMapper configuration profile for Session and Answer entities.
/// Maps between domain entities and DTOs while handling encryption/decryption logic.
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>
    /// Configures all entity-to-DTO mappings.
    /// </summary>
    public MappingProfile()
    {
        ConfigureSessionMappings();
        ConfigureSessionAnswerMappings();
    }

    /// <summary>
    /// Configures Session entity mappings.
    /// </summary>
    private void ConfigureSessionMappings()
    {
        // Session -> SessionDto
        CreateMap<Session, SessionDto>()
            .ForMember(
                dest => dest.State,
                opt => opt.MapFrom(src => src.State.ToString()))
            .ReverseMap()
            .ForMember(
                dest => dest.State,
                opt => opt.MapFrom(src => Enum.Parse<SessionState>(src.State)));

        // CreateSessionDto -> Session
        CreateMap<CreateSessionDto, Session>()
            .ForMember(
                dest => dest.Id,
                opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(
                dest => dest.State,
                opt => opt.MapFrom(_ => SessionState.Pending))
            .ForMember(
                dest => dest.SessionType,
                opt => opt.MapFrom(_ => "anonymous"))
            .ForMember(
                dest => dest.CreatedAt,
                opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(
                dest => dest.UpdatedAt,
                opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(
                dest => dest.LastActivityAt,
                opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(
                dest => dest.Version,
                opt => opt.MapFrom(_ => 1))
            // ExpiresAt and InactivityTimeoutAt will be set by service layer
            // as they depend on configuration values for timeout durations
            .ForMember(
                dest => dest.ExpiresAt,
                opt => opt.Ignore())
            .ForMember(
                dest => dest.InactivityTimeoutAt,
                opt => opt.Ignore());
    }

    /// <summary>
    /// Configures SessionAnswer entity mappings.
    /// Note: Encrypted values are never exposed in DTOs for security.
    /// </summary>
    private void ConfigureSessionAnswerMappings()
    {
        // SessionAnswer -> SessionAnswerDto
        CreateMap<SessionAnswer, SessionAnswerDto>()
            .ForMember(
                dest => dest.AnswerPlain,
                opt => opt.MapFrom(src => src.IsPii ? null : src.AnswerPlain));

        // SessionAnswerDto -> SessionAnswer
        CreateMap<SessionAnswerDto, SessionAnswer>()
            .ForMember(
                dest => dest.Id,
                opt => opt.MapFrom((_, _) => Guid.NewGuid()))
            .ForMember(
                dest => dest.CreatedAt,
                opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(
                dest => dest.UpdatedAt,
                opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(
                dest => dest.Version,
                opt => opt.MapFrom(_ => 1))
            // Encrypted values must be set by encryption service, not mapper
            .ForMember(
                dest => dest.AnswerEncrypted,
                opt => opt.Ignore())
            .ForMember(
                dest => dest.AnswerHash,
                opt => opt.Ignore());
    }
}
