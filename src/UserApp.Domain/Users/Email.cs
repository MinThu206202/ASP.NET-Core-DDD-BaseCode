using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace UserApp.Domain.Users;

public sealed record Email
{
    private static readonly Regex Pattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public string Value { get; }

    [JsonConstructor]
    private Email(string value) => Value = value;

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Pattern.IsMatch(value))
            throw new ArgumentException("Invalid email address", nameof(value));
        return new Email(value.Trim().ToLowerInvariant());
    }

    public override string ToString() => Value;
}
