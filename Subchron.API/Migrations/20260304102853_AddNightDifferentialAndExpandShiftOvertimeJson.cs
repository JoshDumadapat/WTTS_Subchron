using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNightDifferentialAndExpandShiftOvertimeJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NightDifferentialSettingsJson",
                table: "OrganizationSettings",
                type: "NVARCHAR(MAX)",
                nullable: true);

            migrationBuilder.Sql("""
UPDATE OrganizationSettings
SET ShiftTemplatesJson = COALESCE(NULLIF(LTRIM(RTRIM(ShiftTemplatesJson)), ''), '[]')
WHERE ShiftTemplatesJson IS NULL OR LTRIM(RTRIM(ShiftTemplatesJson)) = '';

UPDATE OrganizationSettings
SET OvertimeSettingsJson = COALESCE(NULLIF(LTRIM(RTRIM(OvertimeSettingsJson)), ''),
    '{"enabled":false,"minHoursBeforeOvertime":0,"basis":"AfterShiftEnd","preApprovalRequired":true,"approverRole":"Supervisor","autoApprove":false,"roundToMinutes":15,"minimumBlockMinutes":0,"maxHoursPerDay":null,"maxHoursPerWeek":null,"hardStopEnabled":false,"dayTypes":{"regularDayBehavior":"Default","restDayBehavior":"SeparateBucket","holidayBehavior":"SeparateBucket"}}')
WHERE OvertimeSettingsJson IS NULL OR LTRIM(RTRIM(OvertimeSettingsJson)) = '';

UPDATE OrganizationSettings
SET OvertimeSettingsJson = JSON_MODIFY(OvertimeSettingsJson, '$.bucketRules', JSON_QUERY('[]'))
WHERE ISJSON(OvertimeSettingsJson) = 1 AND JSON_QUERY(OvertimeSettingsJson, '$.bucketRules') IS NULL;

UPDATE OrganizationSettings
SET OvertimeSettingsJson = JSON_MODIFY(OvertimeSettingsJson, '$.scopeRules', JSON_QUERY('[]'))
WHERE ISJSON(OvertimeSettingsJson) = 1 AND JSON_QUERY(OvertimeSettingsJson, '$.scopeRules') IS NULL;

UPDATE OrganizationSettings
SET OvertimeSettingsJson = JSON_MODIFY(OvertimeSettingsJson, '$.approvalSteps', JSON_QUERY('[]'))
WHERE ISJSON(OvertimeSettingsJson) = 1 AND JSON_QUERY(OvertimeSettingsJson, '$.approvalSteps') IS NULL;

UPDATE OrganizationSettings
SET NightDifferentialSettingsJson = '{"enabled":true,"startTime":"22:00","endTime":"06:00","minimumMinutes":0,"excludedAttendanceBuckets":[]}'
WHERE NightDifferentialSettingsJson IS NULL OR LTRIM(RTRIM(NightDifferentialSettingsJson)) = '';

UPDATE OrganizationSettings
SET ShiftTemplatesJson = (
    SELECT
        t.code,
        t.name,
        t.type,
        t.workDays,
        t.fixed,
        t.flexible,
        t.[open],
        COALESCE(t.breaks, JSON_QUERY('[]')) AS breaks,
        COALESCE(t.dayOverrides, JSON_QUERY('[]')) AS dayOverrides,
        t.isActive
    FROM OPENJSON(OrganizationSettings.ShiftTemplatesJson)
    WITH (
        code NVARCHAR(100) '$.code',
        name NVARCHAR(200) '$.name',
        type NVARCHAR(50) '$.type',
        workDays NVARCHAR(MAX) '$.workDays' AS JSON,
        fixed NVARCHAR(MAX) '$.fixed' AS JSON,
        flexible NVARCHAR(MAX) '$.flexible' AS JSON,
        [open] NVARCHAR(MAX) '$.open' AS JSON,
        breaks NVARCHAR(MAX) '$.breaks' AS JSON,
        dayOverrides NVARCHAR(MAX) '$.dayOverrides' AS JSON,
        isActive BIT '$.isActive'
    ) t
    FOR JSON PATH
)
WHERE ISJSON(ShiftTemplatesJson) = 1;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NightDifferentialSettingsJson",
                table: "OrganizationSettings");
        }
    }
}
