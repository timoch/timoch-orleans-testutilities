namespace TiMoch.Orleans.TestUtilities;

/// <summary>
/// Exception thrown when PollForConditionAsync times out before the condition is met.
/// </summary>
[Serializable]
public class PollForConditionFailedException : Exception
{
    public PollForConditionFailedException() { }
    public PollForConditionFailedException(string message) : base(message) { }
    public PollForConditionFailedException(string message, Exception innerException) : base(message, innerException) { }
}