namespace Resulto;

/// <summary>
/// Standard validation code constants to ensure consistency across the codebase.
/// Use these instead of raw strings when creating <see cref="ValidationError"/> instances.
/// </summary>
/// <example>
/// <code>
/// new ValidationError("Email", ValidationCodes.Required, "Email is required.")
/// </code>
/// </example>
public static class ValidationCodes
{
    /// <summary>The field is required and was not provided or is empty.</summary>
    public const string Required = "required";

    /// <summary>The field value does not match the expected format.</summary>
    public const string InvalidFormat = "invalid_format";

    /// <summary>The field value is shorter than the minimum allowed length.</summary>
    public const string TooShort = "too_short";

    /// <summary>The field value exceeds the maximum allowed length.</summary>
    public const string TooLong = "too_long";

    /// <summary>The field value is outside the allowed numeric range.</summary>
    public const string OutOfRange = "out_of_range";

    /// <summary>The field value already exists and must be unique (e.g. duplicate email).</summary>
    public const string Duplicate = "duplicate";

    /// <summary>The field value is not a valid email address.</summary>
    public const string InvalidEmail = "invalid_email";

    /// <summary>The field value is not a valid phone number.</summary>
    public const string InvalidPhone = "invalid_phone";

    /// <summary>The field value is not a valid URL.</summary>
    public const string InvalidUrl = "invalid_url";

    /// <summary>The field value must be a positive number.</summary>
    public const string MustBePositive = "must_be_positive";

    /// <summary>The field value must be a date in the future.</summary>
    public const string MustBeFuture = "must_be_future";

    /// <summary>The field value must be a date in the past.</summary>
    public const string MustBePast = "must_be_past";
}