using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class SeedOrg111PayComponentsMinimums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @OrgID INT = 111;

-- EARNING RULES (ensure at least 3 canonical rules)
IF EXISTS (SELECT 1 FROM EarningRules WHERE OrgID = @OrgID AND Name = 'Regular OT 125%')
    UPDATE EarningRules
    SET AppliesTo='OT', DayType='RegularDay', HolidayCombo='Standard', RestDayHandling='FollowAttendance', Scope='AllEmployees', ScopeTagsJson='[]', RateType='Multiplier', RateValue=1.25,
        IsTaxable=1, IncludeInBenefitBase=0, RequiresApproval=0, Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME()
    WHERE OrgID=@OrgID AND Name='Regular OT 125%';
ELSE
    INSERT INTO EarningRules (OrgID, Name, AppliesTo, DayType, HolidayCombo, RestDayHandling, Scope, ScopeTagsJson, EffectiveFrom, EffectiveTo, RateType, RateValue, IsTaxable, IncludeInBenefitBase, RequiresApproval, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Regular OT 125%', 'OT', 'RegularDay', 'Standard', 'FollowAttendance', 'AllEmployees', '[]', NULL, NULL, 'Multiplier', 1.25, 1, 0, 0, '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM EarningRules WHERE OrgID = @OrgID AND Name = 'Night Differential 10%')
    UPDATE EarningRules
    SET AppliesTo='NightDiff', DayType='Any', HolidayCombo='Standard', RestDayHandling='FollowAttendance', Scope='AllEmployees', ScopeTagsJson='[]', RateType='Percentage', RateValue=10,
        IsTaxable=1, IncludeInBenefitBase=0, RequiresApproval=0, Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME()
    WHERE OrgID=@OrgID AND Name='Night Differential 10%';
ELSE
    INSERT INTO EarningRules (OrgID, Name, AppliesTo, DayType, HolidayCombo, RestDayHandling, Scope, ScopeTagsJson, EffectiveFrom, EffectiveTo, RateType, RateValue, IsTaxable, IncludeInBenefitBase, RequiresApproval, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Night Differential 10%', 'NightDiff', 'Any', 'Standard', 'FollowAttendance', 'AllEmployees', '[]', NULL, NULL, 'Percentage', 10, 1, 0, 0, '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM EarningRules WHERE OrgID = @OrgID AND Name = 'Regular Holiday 200%')
    UPDATE EarningRules
    SET AppliesTo='Holiday', DayType='RegularHoliday', HolidayCombo='Standard', RestDayHandling='FollowAttendance', Scope='AllEmployees', ScopeTagsJson='[]', RateType='Multiplier', RateValue=2.00,
        IsTaxable=1, IncludeInBenefitBase=1, RequiresApproval=0, Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME()
    WHERE OrgID=@OrgID AND Name='Regular Holiday 200%';
ELSE
    INSERT INTO EarningRules (OrgID, Name, AppliesTo, DayType, HolidayCombo, RestDayHandling, Scope, ScopeTagsJson, EffectiveFrom, EffectiveTo, RateType, RateValue, IsTaxable, IncludeInBenefitBase, RequiresApproval, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Regular Holiday 200%', 'Holiday', 'RegularHoliday', 'Standard', 'FollowAttendance', 'AllEmployees', '[]', NULL, NULL, 'Multiplier', 2.00, 1, 1, 0, '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

-- ALLOWANCES (ensure at least 3)
IF EXISTS (SELECT 1 FROM OrgAllowanceRules WHERE OrgID = @OrgID AND Name = 'Rice Allowance')
    UPDATE OrgAllowanceRules
    SET AllowanceType='FixedPerPayroll', Category='DeMinimis', Amount=1500, IsTaxable=0, AttendanceDependent=0, ProrateIfPartialPeriod=0,
        EffectiveFrom=NULL, EffectiveTo=NULL, IsActive=1, ScopeTagsJson='[]', ComplianceNotes='', UpdatedAt=SYSUTCDATETIME()
    WHERE OrgID=@OrgID AND Name='Rice Allowance';
ELSE
    INSERT INTO OrgAllowanceRules (OrgID, Name, AllowanceType, Category, Amount, IsTaxable, AttendanceDependent, ProrateIfPartialPeriod, EffectiveFrom, EffectiveTo, IsActive, ScopeTagsJson, ComplianceNotes, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Rice Allowance', 'FixedPerPayroll', 'DeMinimis', 1500, 0, 0, 0, NULL, NULL, 1, '[]', '', SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM OrgAllowanceRules WHERE OrgID = @OrgID AND Name = 'Night Shift Premium Allowance')
    UPDATE OrgAllowanceRules
    SET AllowanceType='FixedPerPayroll', Category='Operational', Amount=1200, IsTaxable=1, AttendanceDependent=1, ProrateIfPartialPeriod=1,
        EffectiveFrom=NULL, EffectiveTo=NULL, IsActive=1, ScopeTagsJson='[]', ComplianceNotes='', UpdatedAt=SYSUTCDATETIME()
    WHERE OrgID=@OrgID AND Name='Night Shift Premium Allowance';
ELSE
    INSERT INTO OrgAllowanceRules (OrgID, Name, AllowanceType, Category, Amount, IsTaxable, AttendanceDependent, ProrateIfPartialPeriod, EffectiveFrom, EffectiveTo, IsActive, ScopeTagsJson, ComplianceNotes, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Night Shift Premium Allowance', 'FixedPerPayroll', 'Operational', 1200, 1, 1, 1, NULL, NULL, 1, '[]', '', SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM OrgAllowanceRules WHERE OrgID = @OrgID AND Name = 'Transportation Allowance')
    UPDATE OrgAllowanceRules
    SET AllowanceType='FixedPerPayroll', Category='Operational', Amount=1000, IsTaxable=1, AttendanceDependent=1, ProrateIfPartialPeriod=1,
        EffectiveFrom=NULL, EffectiveTo=NULL, IsActive=1, ScopeTagsJson='[]', ComplianceNotes='BPO shuttle/transport support', UpdatedAt=SYSUTCDATETIME()
    WHERE OrgID=@OrgID AND Name='Transportation Allowance';
ELSE
    INSERT INTO OrgAllowanceRules (OrgID, Name, AllowanceType, Category, Amount, IsTaxable, AttendanceDependent, ProrateIfPartialPeriod, EffectiveFrom, EffectiveTo, IsActive, ScopeTagsJson, ComplianceNotes, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Transportation Allowance', 'FixedPerPayroll', 'Operational', 1000, 1, 1, 1, NULL, NULL, 1, '[]', 'BPO shuttle/transport support', SYSUTCDATETIME(), SYSUTCDATETIME());

-- DEDUCTIONS (ensure at least 3)
IF EXISTS (SELECT 1 FROM DeductionRules WHERE OrgID = @OrgID AND Name = 'SSS')
    UPDATE DeductionRules
    SET Category='Statutory', DeductionType='Percentage', Amount=4.5, FormulaExpression=NULL, HasEmployerShare=1, HasEmployeeShare=1,
        EmployerSharePercent=50, EmployeeSharePercent=50, AutoCompute=1, ComputeBasedOn='BasicPay', EffectiveFrom=NULL,
        MaxDeductionAmount=NULL, ScopeTagsJson='[]', Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME()
    WHERE OrgID=@OrgID AND Name='SSS';
ELSE
    INSERT INTO DeductionRules (OrgID, Name, Category, DeductionType, Amount, FormulaExpression, HasEmployerShare, HasEmployeeShare, EmployerSharePercent, EmployeeSharePercent, AutoCompute, ComputeBasedOn, EffectiveFrom, MaxDeductionAmount, ScopeTagsJson, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'SSS', 'Statutory', 'Percentage', 4.5, NULL, 1, 1, 50, 50, 1, 'BasicPay', NULL, NULL, '[]', '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM DeductionRules WHERE OrgID = @OrgID AND Name = 'PhilHealth')
    UPDATE DeductionRules
    SET Category='Statutory', DeductionType='Percentage', Amount=2, FormulaExpression=NULL, HasEmployerShare=1, HasEmployeeShare=1,
        EmployerSharePercent=50, EmployeeSharePercent=50, AutoCompute=1, ComputeBasedOn='BasicPay', EffectiveFrom=NULL,
        MaxDeductionAmount=NULL, ScopeTagsJson='[]', Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME()
    WHERE OrgID=@OrgID AND Name='PhilHealth';
ELSE
    INSERT INTO DeductionRules (OrgID, Name, Category, DeductionType, Amount, FormulaExpression, HasEmployerShare, HasEmployeeShare, EmployerSharePercent, EmployeeSharePercent, AutoCompute, ComputeBasedOn, EffectiveFrom, MaxDeductionAmount, ScopeTagsJson, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'PhilHealth', 'Statutory', 'Percentage', 2, NULL, 1, 1, 50, 50, 1, 'BasicPay', NULL, NULL, '[]', '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM DeductionRules WHERE OrgID = @OrgID AND Name = 'Pag-IBIG')
    UPDATE DeductionRules
    SET Category='Statutory', DeductionType='Percentage', Amount=2, FormulaExpression=NULL, HasEmployerShare=1, HasEmployeeShare=1,
        EmployerSharePercent=50, EmployeeSharePercent=50, AutoCompute=1, ComputeBasedOn='BasicPay', EffectiveFrom=NULL,
        MaxDeductionAmount=NULL, ScopeTagsJson='[]', Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME()
    WHERE OrgID=@OrgID AND Name='Pag-IBIG';
ELSE
    INSERT INTO DeductionRules (OrgID, Name, Category, DeductionType, Amount, FormulaExpression, HasEmployerShare, HasEmployeeShare, EmployerSharePercent, EmployeeSharePercent, AutoCompute, ComputeBasedOn, EffectiveFrom, MaxDeductionAmount, ScopeTagsJson, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Pag-IBIG', 'Statutory', 'Percentage', 2, NULL, 1, 1, 50, 50, 1, 'BasicPay', NULL, NULL, '[]', '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @OrgID INT = 111;
DELETE FROM EarningRules WHERE OrgID = @OrgID AND Name IN ('Regular OT 125%', 'Night Differential 10%', 'Regular Holiday 200%');
DELETE FROM OrgAllowanceRules WHERE OrgID = @OrgID AND Name IN ('Rice Allowance', 'Night Shift Premium Allowance', 'Transportation Allowance');
DELETE FROM DeductionRules WHERE OrgID = @OrgID AND Name IN ('SSS', 'PhilHealth', 'Pag-IBIG');
");
        }
    }
}
