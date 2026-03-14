using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class SeedOrg111AttendanceAndLeave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @OrgID INT = 111;
DECLARE @Today DATE = CAST(SYSUTCDATETIME() AS DATE);
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

-- Attendance logs: last 10 weekdays for all active employees in org 111.
;WITH nums AS (
    SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
    UNION ALL SELECT 10 UNION ALL SELECT 11 UNION ALL SELECT 12 UNION ALL SELECT 13 UNION ALL SELECT 14
),
candidate_days AS (
    SELECT DATEADD(DAY, -n, @Today) AS d
    FROM nums
),
last_10_weekdays AS (
    SELECT TOP (10) d
    FROM candidate_days
    WHERE DATENAME(WEEKDAY, d) NOT IN ('Saturday', 'Sunday')
    ORDER BY d DESC
),
active_emps AS (
    SELECT EmpID, ROW_NUMBER() OVER (ORDER BY EmpID) AS rn
    FROM Employees
    WHERE OrgID = @OrgID AND IsArchived = 0
)
INSERT INTO AttendanceLogs (OrgID, EmpID, LogDate, TimeIn, TimeOut, MethodIn, MethodOut, GeoLat, GeoLong, GeoStatus, DeviceInfo, Remarks)
SELECT
    @OrgID,
    e.EmpID,
    CAST(d.d AS date),
    DATEADD(MINUTE, (e.rn % 6) * 3, DATEADD(HOUR, CASE WHEN e.rn % 2 = 0 THEN 9 ELSE 22 END, CAST(d.d AS datetime2))),
    DATEADD(MINUTE, (e.rn % 4) * 5, DATEADD(HOUR, CASE WHEN e.rn % 2 = 0 THEN 18 ELSE 31 END, CAST(d.d AS datetime2))),
    CASE WHEN e.rn % 3 = 0 THEN 'QR' ELSE 'Kiosk' END,
    CASE WHEN e.rn % 3 = 0 THEN 'QR' ELSE 'Kiosk' END,
    NULL,
    NULL,
    'IN_RANGE',
    'SeededDevice',
    'BPO seeded attendance log'
FROM active_emps e
CROSS JOIN last_10_weekdays d
WHERE NOT EXISTS (
    SELECT 1
    FROM AttendanceLogs al
    WHERE al.OrgID = @OrgID
      AND al.EmpID = e.EmpID
      AND al.LogDate = CAST(d.d AS date)
);

-- Leave requests: seed 1 request per employee (mix of Pending/Approved/Declined)
;WITH emps AS (
    SELECT EmpID, ROW_NUMBER() OVER (ORDER BY EmpID) AS rn
    FROM Employees
    WHERE OrgID = @OrgID AND IsArchived = 0
),
leave_seed AS (
    SELECT
        e.EmpID,
        CASE WHEN e.rn % 3 = 1 THEN 'Vacation Leave' WHEN e.rn % 3 = 2 THEN 'Sick Leave' ELSE 'Service Incentive Leave' END AS LeaveType,
        DATEADD(DAY, CASE WHEN e.rn % 3 = 1 THEN 10 + (e.rn % 5) ELSE -(3 + (e.rn % 5)) END, @Today) AS StartDate,
        DATEADD(DAY, CASE WHEN e.rn % 3 = 1 THEN 11 + (e.rn % 5) ELSE -(2 + (e.rn % 5)) END, @Today) AS EndDate,
        CASE WHEN e.rn % 3 = 1 THEN 'Pending' WHEN e.rn % 3 = 2 THEN 'Approved' ELSE 'Declined' END AS Status,
        CASE WHEN e.rn % 3 = 1 THEN 'Planned leave for personal matters.'
             WHEN e.rn % 3 = 2 THEN 'Medical rest and consultation.'
             ELSE 'Schedule conflict due to previous leave usage.' END AS Reason
    FROM emps e
)
INSERT INTO LeaveRequests (OrgID, EmpID, LeaveType, StartDate, EndDate, Status, Reason, ReviewedByUserID, ReviewedAt, ReviewNotes, CreatedAt, CreatedByUserID)
SELECT
    @OrgID,
    s.EmpID,
    s.LeaveType,
    CAST(s.StartDate AS date),
    CAST(s.EndDate AS date),
    s.Status,
    s.Reason,
    CASE WHEN s.Status = 'Pending' THEN NULL ELSE 1 END,
    CASE WHEN s.Status = 'Pending' THEN NULL ELSE DATEADD(DAY, -1, @Now) END,
    CASE WHEN s.Status = 'Approved' THEN 'Approved during BPO seed initialization.'
         WHEN s.Status = 'Declined' THEN 'Declined to preserve staffing coverage.'
         ELSE NULL END,
    DATEADD(DAY, -2, @Now),
    1
FROM leave_seed s
WHERE NOT EXISTS (
    SELECT 1
    FROM LeaveRequests lr
    WHERE lr.OrgID = @OrgID
      AND lr.EmpID = s.EmpID
      AND lr.LeaveType = s.LeaveType
      AND lr.StartDate = CAST(s.StartDate AS date)
      AND lr.EndDate = CAST(s.EndDate AS date)
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @OrgID INT = 111;

DELETE FROM LeaveRequests
WHERE OrgID = @OrgID
  AND CreatedByUserID = 1
  AND (
      Reason IN ('Planned leave for personal matters.', 'Medical rest and consultation.', 'Schedule conflict due to previous leave usage.')
      OR ReviewNotes IN ('Approved during BPO seed initialization.', 'Declined to preserve staffing coverage.')
  );

DELETE FROM AttendanceLogs
WHERE OrgID = @OrgID
  AND Remarks = 'BPO seeded attendance log'
  AND DeviceInfo = 'SeededDevice';
");
        }
    }
}
