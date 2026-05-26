namespace Resulto;

/// <summary>
/// Represents a single field-level validation error with a machine-readable code.
/// Used by API clients (e.g. React forms) to display errors next to individual input fields.
/// </summary>
/// <param name="Field">The name of the field that failed validation (e.g. "Email", "Salary").</param>
/// <param name="Code">A machine-readable error code from <see cref="ValidationCodes"/> (e.g. "required", "out_of_range").</param>
/// <param name="Message">A human-readable, localized description of the error.</param>
public sealed record ValidationError(string Field, string Code, string Message);