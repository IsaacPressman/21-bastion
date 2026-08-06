namespace Bastion.Core.Config;

/// <summary>
/// Thrown when tuning data is missing, malformed, or internally inconsistent.
/// </summary>
/// <remarks>
/// Tuning data is expected to be edited by hand between playtests, frequently and under time
/// pressure. A typo should stop the build or the launch with a specific message, not surface
/// three hours later as a wave that resolves oddly.
/// </remarks>
public sealed class TuningValidationException : Exception
{
    public TuningValidationException(string message) : base(message) { }

    public TuningValidationException(string message, Exception inner) : base(message, inner) { }
}
