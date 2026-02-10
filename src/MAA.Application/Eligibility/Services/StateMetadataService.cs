using MAA.Application.Eligibility.DTOs;

namespace MAA.Application.Eligibility.Services;

/// <summary>
/// State metadata service implementation for the eligibility wizard.
/// </summary>
/// <remarks>
/// MVP implementation with hardcoded pilot states.
/// Future: Integrate with state database or configuration service.
/// </remarks>
public class StateMetadataService : IStateMetadataService
{
    // Hardcoded pilot states for MVP (Texas, California)
    private static readonly List<StateInfoDto> PilotStates = new()
    {
        new StateInfoDto { Code = "TX", Name = "Texas", IsPilot = true },
        new StateInfoDto { Code = "CA", Name = "California", IsPilot = true }
    };

    // Simplified ZIP to state mapping for MVP
    // Format: ZIP prefix (first 3 digits) -> state code
    private static readonly Dictionary<string, string> ZipToStateMap = new()
    {
        // Texas ZIP ranges: 750-799, 885
        ["750"] = "TX",
        ["751"] = "TX",
        ["752"] = "TX",
        ["753"] = "TX",
        ["754"] = "TX",
        ["755"] = "TX",
        ["756"] = "TX",
        ["757"] = "TX",
        ["758"] = "TX",
        ["759"] = "TX",
        ["760"] = "TX",
        ["761"] = "TX",
        ["762"] = "TX",
        ["763"] = "TX",
        ["764"] = "TX",
        ["765"] = "TX",
        ["766"] = "TX",
        ["767"] = "TX",
        ["768"] = "TX",
        ["769"] = "TX",
        ["770"] = "TX",
        ["771"] = "TX",
        ["772"] = "TX",
        ["773"] = "TX",
        ["774"] = "TX",
        ["775"] = "TX",
        ["776"] = "TX",
        ["777"] = "TX",
        ["778"] = "TX",
        ["779"] = "TX",
        ["780"] = "TX",
        ["781"] = "TX",
        ["782"] = "TX",
        ["783"] = "TX",
        ["784"] = "TX",
        ["785"] = "TX",
        ["786"] = "TX",
        ["787"] = "TX",
        ["788"] = "TX",
        ["789"] = "TX",
        ["790"] = "TX",
        ["791"] = "TX",
        ["792"] = "TX",
        ["793"] = "TX",
        ["794"] = "TX",
        ["795"] = "TX",
        ["796"] = "TX",
        ["797"] = "TX",
        ["798"] = "TX",
        ["799"] = "TX",
        ["885"] = "TX",

        // California ZIP ranges: 900-961
        ["900"] = "CA",
        ["901"] = "CA",
        ["902"] = "CA",
        ["903"] = "CA",
        ["904"] = "CA",
        ["905"] = "CA",
        ["906"] = "CA",
        ["907"] = "CA",
        ["908"] = "CA",
        ["910"] = "CA",
        ["911"] = "CA",
        ["912"] = "CA",
        ["913"] = "CA",
        ["914"] = "CA",
        ["915"] = "CA",
        ["916"] = "CA",
        ["917"] = "CA",
        ["918"] = "CA",
        ["919"] = "CA",
        ["920"] = "CA",
        ["921"] = "CA",
        ["922"] = "CA",
        ["923"] = "CA",
        ["924"] = "CA",
        ["925"] = "CA",
        ["926"] = "CA",
        ["927"] = "CA",
        ["928"] = "CA",
        ["930"] = "CA",
        ["931"] = "CA",
        ["932"] = "CA",
        ["933"] = "CA",
        ["934"] = "CA",
        ["935"] = "CA",
        ["936"] = "CA",
        ["937"] = "CA",
        ["938"] = "CA",
        ["939"] = "CA",
        ["940"] = "CA",
        ["941"] = "CA",
        ["942"] = "CA",
        ["943"] = "CA",
        ["944"] = "CA",
        ["945"] = "CA",
        ["946"] = "CA",
        ["947"] = "CA",
        ["948"] = "CA",
        ["949"] = "CA",
        ["950"] = "CA",
        ["951"] = "CA",
        ["952"] = "CA",
        ["953"] = "CA",
        ["954"] = "CA",
        ["955"] = "CA",
        ["956"] = "CA",
        ["957"] = "CA",
        ["958"] = "CA",
        ["959"] = "CA",
        ["960"] = "CA",
        ["961"] = "CA"
    };

    /// <summary>
    /// Gets all pilot states for the eligibility wizard.
    /// </summary>
    public Task<List<StateInfoDto>> GetAllStatesAsync()
    {
        return Task.FromResult(PilotStates.ToList());
    }

    /// <summary>
    /// Looks up state by ZIP code using partial matching.
    /// </summary>
    public Task<StateLookupResponse?> LookupStateByZipAsync(string zip)
    {
        if (string.IsNullOrWhiteSpace(zip) || zip.Length != 5)
        {
            return Task.FromResult<StateLookupResponse?>(null);
        }

        // Extract first 3 digits for lookup
        var zipPrefix = zip.Substring(0, 3);

        if (ZipToStateMap.TryGetValue(zipPrefix, out var stateCode))
        {
            var state = PilotStates.FirstOrDefault(s => s.Code == stateCode);
            if (state != null)
            {
                return Task.FromResult<StateLookupResponse?>(new StateLookupResponse
                {
                    Code = state.Code,
                    Name = state.Name,
                    Source = "zip"
                });
            }
        }

        return Task.FromResult<StateLookupResponse?>(null);
    }
}
