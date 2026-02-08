namespace MAA.Domain.Sessions;

/// <summary>
/// Represents the lifecycle state of a session.
/// Follows a defined state machine with explicit transitions.
/// </summary>
public enum SessionState
{
    /// <summary>
    /// Session created but no user interaction yet.
    /// Initial state when session is first created.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// User actively answering questions in the wizard.
    /// Transition from Pending on first answer submission.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// User completed wizard and submitted for eligibility evaluation.
    /// Transition from InProgress on final submission.
    /// </summary>
    Submitted = 2,

    /// <summary>
    /// Eligibility evaluation complete, results generated.
    /// Terminal state - no further transitions allowed.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Session timed out or explicitly abandoned by user.
    /// Terminal state - no further transitions allowed.
    /// </summary>
    Abandoned = 4
}
