namespace Resulto.Tests;

public class ResultErrorTests
{
    [Fact]
    public void Factories_set_type_and_code()
    {
        Assert.Equal(400, ResultError.BadRequest().Code);
        Assert.Equal(404, ResultError.NotFound().Code);
        Assert.Equal(409, ResultError.Conflict().Code);
        Assert.Equal(422, ResultError.Validation().Code);
    }

    [Fact]
    public void Custom_message_overrides_default() => Assert.Equal("nope", ResultError.NotFound("nope").Message);

    [Fact]
    public void Equality_is_by_error_type_only()
    {
        Assert.Equal(ResultError.NotFound("a"), ResultError.NotFound("b"));
        Assert.NotEqual(ResultError.NotFound(), ResultError.Conflict());
        Assert.Equal(ResultError.NotFound().GetHashCode(), ResultError.NotFound("x").GetHashCode());
    }

    [Fact]
    public void Validation_with_fields_carries_details()
    {
        List<ValidationError> errors =
        [
            new("Email", ValidationCodes.Required, "Email is required."),
            new("Email", ValidationCodes.InvalidEmail, "Email is invalid."),
        ];

        ResultError error = ResultError.Validation(errors);

        Assert.Equal(ErrorType.Validation, error.ErrorType);
        Assert.Equal(2, error.ValidationErrors.Count);
    }

    [Fact]
    public void Non_validation_error_has_empty_validation_list() => Assert.Empty(ResultError.NotFound().ValidationErrors);

    [Fact]
    public void Validation_with_null_errors_throws() => Assert.Throws<ArgumentNullException>(() => ResultError.Validation((IReadOnlyList<ValidationError>)null!));
}
