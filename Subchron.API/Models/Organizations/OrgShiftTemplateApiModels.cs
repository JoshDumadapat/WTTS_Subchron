namespace Subchron.API.Models.Organizations;

public class OrgShiftTemplateListResponse
{
    public List<OrgShiftTemplateDto> Templates { get; set; } = new();
    public string? DefaultShiftTemplateCode { get; set; }
}

public class OrgShiftTemplateMutationResponse
{
    public bool Ok { get; set; } = true;
    public OrgShiftTemplateDto Template { get; set; } = new();
}
