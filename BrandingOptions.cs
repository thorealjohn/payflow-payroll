namespace itpayroll;

public class BrandingOptions
{
    public const string SectionName = "Branding";

    /// <summary>Path under wwwroot, e.g. ~/images/logo.png or ~/images/my-brand.svg</summary>
    public string LogoPath { get; set; } = "~/images/logo.jpg";
}
