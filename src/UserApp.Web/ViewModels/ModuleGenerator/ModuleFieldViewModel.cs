namespace UserApp.Web.ViewModels.ModuleGenerator;

public class ModuleFieldViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public int? Length { get; set; }
    public bool IsRequired { get; set; }
    public bool IsNullable { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    public string? EnumValues { get; set; }
    public bool UseCommonTable { get; set; }
    public bool EnumRenderAsCheckbox { get; set; }

    public bool IsRelation { get; set; }
    public string? RelatedEntityName { get; set; }
    public bool IsPivot { get; set; }
}