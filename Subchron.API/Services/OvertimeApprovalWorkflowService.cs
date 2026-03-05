using Subchron.API.Models.Organizations;

namespace Subchron.API.Services;

public interface IOvertimeApprovalWorkflowService
{
    OvertimeApprovalEvaluationResult Evaluate(OvertimeApprovalEvaluationContext context);
}

public class OvertimeApprovalWorkflowService : IOvertimeApprovalWorkflowService
{
    public OvertimeApprovalEvaluationResult Evaluate(OvertimeApprovalEvaluationContext context)
    {
        var result = new OvertimeApprovalEvaluationResult();
        var overtime = context.OvertimeSettings ?? new OrgOvertimeSettingsDto();

        if (!overtime.Enabled)
        {
            result.Eligible = false;
            result.RejectionReason = "Overtime is disabled for the organization.";
            return result;
        }

        if (!IsEligibleByScope(context, overtime.ScopeRules))
        {
            result.Eligible = false;
            result.RejectionReason = "Employee does not match overtime scope eligibility rules.";
            return result;
        }

        result.Eligible = true;
        result.Steps = ResolveSteps(overtime, context.FilingMode);
        return result;
    }

    private static bool IsEligibleByScope(OvertimeApprovalEvaluationContext context, List<OrgOvertimeScopeRuleDto>? rules)
    {
        if (rules == null || rules.Count == 0)
            return true;

        foreach (var rule in rules)
        {
            var values = new HashSet<string>((rule.Values ?? new List<string>()).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()), StringComparer.OrdinalIgnoreCase);
            if (values.Count == 0)
                continue;

            var currentValue = ResolveValue(context, rule.ScopeType);
            var contains = currentValue != null && values.Contains(currentValue);

            if (rule.Include && !contains)
                return false;
            if (!rule.Include && contains)
                return false;
        }

        return true;
    }

    private static string? ResolveValue(OvertimeApprovalEvaluationContext context, string scopeType)
    {
        if (string.Equals(scopeType, "Department", StringComparison.OrdinalIgnoreCase))
            return context.DepartmentId?.ToString();

        if (string.Equals(scopeType, "EmploymentType", StringComparison.OrdinalIgnoreCase))
            return context.EmploymentType;

        if (string.Equals(scopeType, "Role", StringComparison.OrdinalIgnoreCase))
            return context.EmployeeRole;

        if (string.Equals(scopeType, "Site", StringComparison.OrdinalIgnoreCase))
            return context.SiteCode;

        return null;
    }

    private static List<OrgOvertimeApprovalStepDto> ResolveSteps(OrgOvertimeSettingsDto overtime, string? filingMode)
    {
        if (overtime.AutoApprove)
            return new List<OrgOvertimeApprovalStepDto>();

        if (string.Equals(filingMode, "MANUAL", StringComparison.OrdinalIgnoreCase))
        {
            if (overtime.ApprovalSteps != null && overtime.ApprovalSteps.Count > 0)
                return overtime.ApprovalSteps.OrderBy(s => s.Order).ToList();

            return new List<OrgOvertimeApprovalStepDto>
            {
                new()
                {
                    Order = 1,
                    Role = string.IsNullOrWhiteSpace(overtime.ApproverRole) ? "Supervisor" : overtime.ApproverRole,
                    Required = true
                }
            };
        }

        if (overtime.ApprovalSteps != null && overtime.ApprovalSteps.Count > 0)
            return overtime.ApprovalSteps.OrderBy(s => s.Order).ToList();

        if (!overtime.PreApprovalRequired)
            return new List<OrgOvertimeApprovalStepDto>();

        return new List<OrgOvertimeApprovalStepDto>
        {
            new()
            {
                Order = 1,
                Role = string.IsNullOrWhiteSpace(overtime.ApproverRole) ? "Supervisor" : overtime.ApproverRole,
                Required = true
            }
        };
    }
}
