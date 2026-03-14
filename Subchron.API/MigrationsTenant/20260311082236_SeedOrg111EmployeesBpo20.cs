using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class SeedOrg111EmployeesBpo20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @OrgID INT = 111;
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

DECLARE @seed TABLE
(
    EmpNumber NVARCHAR(40) NOT NULL,
    FirstName NVARCHAR(80) NOT NULL,
    LastName NVARCHAR(80) NOT NULL,
    Gender NVARCHAR(20) NULL,
    EmploymentType NVARCHAR(40) NOT NULL,
    CompensationBasisOverride NVARCHAR(20) NOT NULL,
    BasePayAmount DECIMAL(12,2) NOT NULL,
    AssignedShiftTemplateCode NVARCHAR(60) NULL,
    DateHired DATE NOT NULL,
    Email NVARCHAR(256) NOT NULL
);

INSERT INTO @seed (EmpNumber, FirstName, LastName, Gender, EmploymentType, CompensationBasisOverride, BasePayAmount, AssignedShiftTemplateCode, DateHired, Email)
VALUES
('BPO111-0001','Alden','Reyes','Male','Regular','UseOrgDefault',24000,'BPO-DAY','2024-01-15','alden.reyes@org111.local'),
('BPO111-0002','Bianca','Santos','Female','Regular','UseOrgDefault',24500,'BPO-DAY','2024-01-20','bianca.santos@org111.local'),
('BPO111-0003','Carlo','Dizon','Male','Regular','UseOrgDefault',25000,'BPO-NIGHT','2024-02-01','carlo.dizon@org111.local'),
('BPO111-0004','Dianne','Mendoza','Female','Regular','UseOrgDefault',25250,'BPO-NIGHT','2024-02-03','dianne.mendoza@org111.local'),
('BPO111-0005','Ethan','Flores','Male','Regular','UseOrgDefault',23800,'BPO-DAY','2024-02-10','ethan.flores@org111.local'),
('BPO111-0006','Faith','Cruz','Female','Regular','UseOrgDefault',24200,'BPO-DAY','2024-02-15','faith.cruz@org111.local'),
('BPO111-0007','Gabriel','Navarro','Male','Regular','UseOrgDefault',26000,'BPO-NIGHT','2024-03-01','gabriel.navarro@org111.local'),
('BPO111-0008','Hannah','Garcia','Female','Regular','UseOrgDefault',24800,'BPO-NIGHT','2024-03-06','hannah.garcia@org111.local'),
('BPO111-0009','Ian','Torres','Male','Regular','UseOrgDefault',24150,'BPO-DAY','2024-03-10','ian.torres@org111.local'),
('BPO111-0010','Jessa','Lopez','Female','Regular','UseOrgDefault',24600,'BPO-DAY','2024-03-12','jessa.lopez@org111.local'),
('BPO111-0011','Kevin','Ramos','Male','Regular','UseOrgDefault',25500,'BPO-NIGHT','2024-03-18','kevin.ramos@org111.local'),
('BPO111-0012','Lara','Aquino','Female','Regular','UseOrgDefault',25300,'BPO-NIGHT','2024-03-20','lara.aquino@org111.local'),
('BPO111-0013','Marco','Villanueva','Male','Regular','UseOrgDefault',24400,'BPO-DAY','2024-04-01','marco.villanueva@org111.local'),
('BPO111-0014','Nina','Bautista','Female','Regular','UseOrgDefault',24750,'BPO-DAY','2024-04-03','nina.bautista@org111.local'),
('BPO111-0015','Oscar','Fernandez','Male','Regular','UseOrgDefault',25800,'BPO-NIGHT','2024-04-10','oscar.fernandez@org111.local'),
('BPO111-0016','Paula','Soriano','Female','Regular','UseOrgDefault',25100,'BPO-NIGHT','2024-04-14','paula.soriano@org111.local'),
('BPO111-0017','Quinn','Lim','Male','Regular','UseOrgDefault',24300,'BPO-DAY','2024-04-18','quinn.lim@org111.local'),
('BPO111-0018','Rhea','Valdez','Female','Regular','UseOrgDefault',24950,'BPO-DAY','2024-04-21','rhea.valdez@org111.local'),
('BPO111-0019','Sean','DelosReyes','Male','Regular','UseOrgDefault',26200,'BPO-NIGHT','2024-04-25','sean.delosreyes@org111.local'),
('BPO111-0020','Trisha','Domingo','Female','Regular','UseOrgDefault',25400,'BPO-NIGHT','2024-04-28','trisha.domingo@org111.local');

INSERT INTO Employees
(
    OrgID, UserID, DepartmentID, AssignedShiftTemplateCode, AssignedLocationId, EmpNumber,
    LastName, FirstName, MiddleName, BirthDate, Gender, Role, EmploymentType,
    CompensationBasisOverride, BasePayAmount, CustomUnitLabel, CustomWorkHours,
    WorkState, DateHired, AddressLine1, AddressLine2, City, StateProvince, PostalCode, Country,
    Phone, PhoneNormalized, Email, EmergencyContactName, EmergencyContactPhone, EmergencyContactRelation,
    IsArchived, AttendanceQrToken, AttendanceQrIssuedAt, AvatarUrl, IdPictureUrl, SignatureUrl,
    CreatedAt, UpdatedAt, CreatedByUserId, UpdatedByUserId
)
SELECT
    @OrgID, NULL, NULL, s.AssignedShiftTemplateCode, NULL, s.EmpNumber,
    s.LastName, s.FirstName, NULL, NULL, s.Gender, 'Employee', s.EmploymentType,
    s.CompensationBasisOverride, s.BasePayAmount, NULL, NULL,
    'Active', s.DateHired, NULL, NULL, 'Quezon City', 'Metro Manila', '1100', 'Philippines',
    NULL, NULL, s.Email, NULL, NULL, NULL,
    0, NULL, NULL,
    CONCAT('https://ui-avatars.com/api/?name=', REPLACE(s.FirstName + ' ' + s.LastName, ' ', '+'), '&background=0F766E&color=fff&bold=true'),
    CONCAT('https://ui-avatars.com/api/?name=', REPLACE(s.FirstName + ' ' + s.LastName, ' ', '+'), '&background=0F766E&color=fff&bold=true'),
    '/images/signatures/sample-signature.svg',
    @Now, @Now, 1, 1
FROM @seed s
WHERE NOT EXISTS (
    SELECT 1 FROM Employees e WHERE e.OrgID = @OrgID AND e.EmpNumber = s.EmpNumber
);

-- Auto-attach active statutory deductions for seeded employees
DECLARE @ruleIds TABLE (DeductionRuleID INT PRIMARY KEY);
INSERT INTO @ruleIds(DeductionRuleID)
SELECT DeductionRuleID
FROM DeductionRules
WHERE OrgID = @OrgID AND IsActive = 1 AND Name IN ('SSS', 'PhilHealth', 'Pag-IBIG');

INSERT INTO EmployeeDeductionProfiles (OrgID, EmpID, DeductionRuleID, Mode, Value, IsActive, Notes, CreatedAt, UpdatedAt)
SELECT
    @OrgID,
    e.EmpID,
    r.DeductionRuleID,
    'UseRule',
    NULL,
    1,
    'Seeded default statutory deduction',
    @Now,
    @Now
FROM Employees e
CROSS JOIN @ruleIds r
WHERE e.OrgID = @OrgID
  AND e.EmpNumber LIKE 'BPO111-%'
  AND NOT EXISTS (
      SELECT 1
      FROM EmployeeDeductionProfiles p
      WHERE p.OrgID = @OrgID
        AND p.EmpID = e.EmpID
        AND p.DeductionRuleID = r.DeductionRuleID
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @OrgID INT = 111;

DELETE p
FROM EmployeeDeductionProfiles p
INNER JOIN Employees e ON e.EmpID = p.EmpID
WHERE e.OrgID = @OrgID
  AND e.EmpNumber LIKE 'BPO111-%';

DELETE FROM Employees
WHERE OrgID = @OrgID
  AND EmpNumber LIKE 'BPO111-%';
");
        }
    }
}
