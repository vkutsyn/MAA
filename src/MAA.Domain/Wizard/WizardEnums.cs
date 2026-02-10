namespace MAA.Domain.Wizard;

/// <summary>
/// Status of a step answer submission.
/// </summary>
public enum AnswerStatus
{
    Draft = 0,
    Submitted = 1
}

/// <summary>
/// Status of a wizard step in the session progress timeline.
/// </summary>
public enum StepStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    RequiresRevalidation = 3
}
