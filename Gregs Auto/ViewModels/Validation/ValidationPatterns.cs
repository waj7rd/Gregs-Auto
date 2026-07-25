namespace Gregs_Auto.ViewModels.Validation;

// Shared regex patterns so the Create and Edit forms can't drift apart.
public static class ValidationPatterns
{
    // Digits plus the usual separators — permissive about formatting, but
    // rejects letters and other junk.
    public const string Phone = @"^[\d\s\-\(\)\+\.]{7,30}$";
    public const string PhoneMessage = "Phone can only contain digits, spaces, and - ( ) + . characters.";

    // Real VINs are exactly 17 characters and never use I, O, or Q
    // (they'd be mistaken for 1 and 0).
    public const string Vin = @"^[A-HJ-NPR-Za-hj-npr-z0-9]{17}$";
    public const string VinMessage = "VIN must be exactly 17 characters and cannot contain I, O, or Q.";
}
