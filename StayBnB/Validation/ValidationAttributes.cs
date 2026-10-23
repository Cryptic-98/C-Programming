using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StayBnB.Validation;

/// <summary>Validates that a date is today or in the future.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class FutureDateAttribute : ValidationAttribute
{
    public FutureDateAttribute() => ErrorMessage = "Date cannot be in the past.";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;

        if (value is DateTime date)
            return date.Date >= DateTime.Today
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage);

        if (value is DateTimeOffset offset)
            return offset.Date >= DateTime.Today
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage);

        return new ValidationResult(ErrorMessage);
    }
}

/// <summary>Validates that a date is strictly after another date property on the same model.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class DateAfterAttribute : ValidationAttribute
{
    private readonly string _otherPropertyName;

    public DateAfterAttribute(string otherPropertyName)
    {
        _otherPropertyName = otherPropertyName;
        ErrorMessage = "Check-out date must be after the check-in date.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var otherProperty = validationContext.ObjectType.GetProperty(_otherPropertyName);
        if (otherProperty is null)
            return new ValidationResult($"Unknown comparison property '{_otherPropertyName}'.");

        var otherValue = otherProperty.GetValue(validationContext.ObjectInstance);

        if (value is null || otherValue is null)
            return ValidationResult.Success;

        if (!TryGetDate(value, out var date) || !TryGetDate(otherValue, out var otherDate))
            return new ValidationResult(ErrorMessage);

        return date.Date > otherDate.Date
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage);
    }

    private static bool TryGetDate(object value, out DateTime date)
    {
        switch (value)
        {
            case DateTime dt:
                date = dt;
                return true;
            case DateTimeOffset dto:
                date = dto.DateTime;
                return true;
            default:
                date = default;
                return false;
        }
    }
}

/// <summary>Requires at least <c>min</c> uploaded files.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MinFilesAttribute : ValidationAttribute
{
    private readonly int _min;

    public MinFilesAttribute(int min)
    {
        _min = min;
        ErrorMessage = $"At least {min} file(s) are required.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;

        var count = 0;

        if (value is IEnumerable<IFormFile> files)
        {
            foreach (var file in files)
            {
                if (file is not null && file.Length > 0)
                    count++;
            }
        }
        else if (value is IFormFile single)
        {
            if (single.Length > 0)
                count = 1;
        }
        else
        {
            return new ValidationResult("A file collection was expected.");
        }

        return count >= _min
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage);
    }
}

/// <summary>Restricts uploads to a whitelist of file extensions.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class AllowedExtensionsAttribute : ValidationAttribute
{
    private readonly string[] _extensions;

    public AllowedExtensionsAttribute(params string[] extensions)
    {
        _extensions = extensions.Select(e => e.ToLowerInvariant()).ToArray();
        ErrorMessage = $"Allowed file types: {string.Join(", ", _extensions)}.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;

        var files = value as IEnumerable<IFormFile> ??
            (value is IFormFile single ? new[] { single } : Array.Empty<IFormFile>());

        foreach (var file in files)
        {
            if (file is null || file.Length == 0)
                continue;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_extensions.Contains(extension))
                return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}

/// <summary>Caps the size of each uploaded file.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MaxFileSizeAttribute : ValidationAttribute
{
    private readonly long _maxBytes;

    public MaxFileSizeAttribute(int megabytes)
    {
        _maxBytes = (long)megabytes * 1024 * 1024;
        ErrorMessage = $"Each file must be smaller than {megabytes} MB.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;

        var files = value as IEnumerable<IFormFile> ??
            (value is IFormFile single ? new[] { single } : Array.Empty<IFormFile>());

        foreach (var file in files)
        {
            if (file is null || file.Length == 0)
                continue;

            if (file.Length > _maxBytes)
                return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}
