using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;
using Subchron.API.Models.Entities;
using Subchron.API.Services;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/scan-stations")]
[Authorize]
public class ScanStationsController : ControllerBase
{
    private const bool EnableRapidScanTestMode = true;
    private readonly TenantDbContext _db;
    private readonly IAuditService _audit;

    public ScanStationsController(TenantDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet("current")]
    public async Task<ActionResult<List<ScanStationDto>>> GetCurrent()
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var data = await _db.ScanStations.AsNoTracking()
            .Where(x => x.OrgID == orgId.Value)
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.StationName)
            .Join(_db.Locations.AsNoTracking(), s => s.LocationID, l => l.LocationID, (s, l) => new ScanStationDto
            {
                ScanStationID = s.ScanStationID,
                StationCode = s.StationCode,
                StationName = s.StationName,
                LocationID = s.LocationID,
                LocationName = l.LocationName,
                QrEnabled = s.QrEnabled,
                IdEntryEnabled = s.IdEntryEnabled,
                ScheduleMode = s.ScheduleMode,
                IsActive = s.IsActive
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("current/{id:int}")]
    public async Task<ActionResult<ScanStationDto>> GetOne(int id)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var item = await _db.ScanStations.AsNoTracking()
            .Where(x => x.OrgID == orgId.Value && x.ScanStationID == id)
            .Join(_db.Locations.AsNoTracking(), s => s.LocationID, l => l.LocationID, (s, l) => new ScanStationDto
            {
                ScanStationID = s.ScanStationID,
                StationCode = s.StationCode,
                StationName = s.StationName,
                LocationID = s.LocationID,
                LocationName = l.LocationName,
                QrEnabled = s.QrEnabled,
                IdEntryEnabled = s.IdEntryEnabled,
                ScheduleMode = s.ScheduleMode,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync();

        if (item is null)
            return NotFound(new { ok = false, message = "Station not found." });

        return Ok(item);
    }

    [HttpPost("current")]
    public async Task<ActionResult<ScanStationDto>> Create([FromBody] CreateScanStationRequest req)
    {
        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue)
            return Forbid();

        var config = await GetAttendanceConfigAsync(orgId.Value);
        if (IsBiometricOnly(config.PrimaryMode))
            return BadRequest(new { ok = false, message = "Attendance capture is set to Biometric + Geofencing. Scan Station is unavailable in this mode." });

        var validation = await ValidateCreateUpdate(req, orgId.Value, null);
        if (!validation.ok)
            return validation.result!;

        var station = new ScanStation
        {
            OrgID = orgId.Value,
            LocationID = req.LocationID,
            StationCode = BuildStationCode(req.StationName),
            StationName = req.StationName.Trim(),
            QrEnabled = req.QrEnabled,
            IdEntryEnabled = config.AllowManualEntry,
            ScheduleMode = NormalizeScheduleMode(req.ScheduleMode),
            IsActive = true,
            CreatedByUserID = userId,
            UpdatedByUserID = userId,
            UpdatedAt = DateTime.UtcNow
        };

        if (await _db.ScanStations.AnyAsync(x => x.OrgID == orgId.Value && x.StationCode == station.StationCode))
            station.StationCode = BuildStationCode(req.StationName + "-" + Guid.NewGuid().ToString("N")[..4]);

        _db.ScanStations.Add(station);
        await _db.SaveChangesAsync();
        await TryAuditTenantAsync(orgId.Value, userId, "ScanStationCreated", nameof(ScanStation), station.ScanStationID,
            $"Created station '{station.StationName}' for location #{station.LocationID}. QR={(station.QrEnabled ? "ON" : "OFF")}, IDEntry={(station.IdEntryEnabled ? "ON" : "OFF")}.");

        var locationName = await _db.Locations.Where(x => x.LocationID == station.LocationID).Select(x => x.LocationName).FirstAsync();
        return Ok(new ScanStationDto
        {
            ScanStationID = station.ScanStationID,
            StationCode = station.StationCode,
            StationName = station.StationName,
            LocationID = station.LocationID,
            LocationName = locationName,
            QrEnabled = station.QrEnabled,
            IdEntryEnabled = station.IdEntryEnabled,
            ScheduleMode = station.ScheduleMode,
            IsActive = station.IsActive
        });
    }

    [HttpPut("current/{id:int}")]
    public async Task<ActionResult<ScanStationDto>> Update(int id, [FromBody] UpdateScanStationRequest req)
    {
        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue)
            return Forbid();

        var station = await _db.ScanStations.FirstOrDefaultAsync(x => x.OrgID == orgId.Value && x.ScanStationID == id);
        if (station is null)
            return NotFound(new { ok = false, message = "Station not found." });

        var config = await GetAttendanceConfigAsync(orgId.Value);
        if (IsBiometricOnly(config.PrimaryMode))
            return BadRequest(new { ok = false, message = "Attendance capture is set to Biometric + Geofencing. Scan Station is unavailable in this mode." });

        var validation = await ValidateCreateUpdate(new CreateScanStationRequest
        {
            LocationID = req.LocationID ?? station.LocationID,
            StationName = req.StationName ?? station.StationName,
            QrEnabled = req.QrEnabled ?? station.QrEnabled,
            ScheduleMode = req.ScheduleMode ?? station.ScheduleMode
        }, orgId.Value, id);
        if (!validation.ok)
            return validation.result!;

        if (req.LocationID.HasValue) station.LocationID = req.LocationID.Value;
        if (!string.IsNullOrWhiteSpace(req.StationName)) station.StationName = req.StationName.Trim();
        if (req.QrEnabled.HasValue) station.QrEnabled = req.QrEnabled.Value;
        if (!string.IsNullOrWhiteSpace(req.ScheduleMode)) station.ScheduleMode = NormalizeScheduleMode(req.ScheduleMode);
        station.IdEntryEnabled = config.AllowManualEntry;
        station.UpdatedByUserID = userId;
        station.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await TryAuditTenantAsync(orgId.Value, userId, "ScanStationUpdated", nameof(ScanStation), station.ScanStationID,
            $"Updated station '{station.StationName}'. Location #{station.LocationID}, QR={(station.QrEnabled ? "ON" : "OFF")}, IDEntry={(station.IdEntryEnabled ? "ON" : "OFF")}, Mode={station.ScheduleMode}.");

        var locationName = await _db.Locations.Where(x => x.LocationID == station.LocationID).Select(x => x.LocationName).FirstAsync();
        return Ok(new ScanStationDto
        {
            ScanStationID = station.ScanStationID,
            StationCode = station.StationCode,
            StationName = station.StationName,
            LocationID = station.LocationID,
            LocationName = locationName,
            QrEnabled = station.QrEnabled,
            IdEntryEnabled = station.IdEntryEnabled,
            ScheduleMode = station.ScheduleMode,
            IsActive = station.IsActive
        });
    }

    [HttpPatch("current/{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] SetScanStationStatusRequest req)
    {
        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue)
            return Forbid();

        var station = await _db.ScanStations.FirstOrDefaultAsync(x => x.OrgID == orgId.Value && x.ScanStationID == id);
        if (station is null)
            return NotFound(new { ok = false, message = "Station not found." });

        station.IsActive = req.IsActive;
        station.UpdatedByUserID = userId;
        station.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await TryAuditTenantAsync(orgId.Value, userId, "ScanStationStatusChanged", nameof(ScanStation), station.ScanStationID,
            $"Station '{station.StationName}' was {(station.IsActive ? "activated" : "deactivated")}.");
        return Ok(new { ok = true });
    }

    [HttpPost("current/{id:int}/validate-location")]
    public async Task<ActionResult<LocationValidationResponse>> ValidateLocation(int id, [FromBody] ValidateLocationRequest req)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var config = await GetAttendanceConfigAsync(orgId.Value);
        if (IsBiometricOnly(config.PrimaryMode))
        {
            await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanStationLocationValidationRejected", nameof(ScanStation), id,
                "Location validation rejected because primary mode is Biometric.");
            return BadRequest(new { ok = false, message = "Attendance capture is set to Biometric + Geofencing. Scan Station is unavailable in this mode." });
        }

        var station = await _db.ScanStations.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == orgId.Value && x.ScanStationID == id && x.IsActive);
        if (station is null)
            return NotFound(new { ok = false, message = "Station not found or inactive." });

        var location = await _db.Locations.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == orgId.Value && x.LocationID == station.LocationID);
        if (location is null || !location.IsActive)
            return BadRequest(new { ok = false, message = "Assigned site is not available." });

        var distance = ComputeDistanceMeters((double)location.GeoLat, (double)location.GeoLong, (double)req.Latitude, (double)req.Longitude);
        var inside = distance <= location.RadiusMeters;
        return Ok(new LocationValidationResponse
        {
            Ok = true,
            InsideRadius = inside,
            DistanceMeters = Math.Round(distance, 2),
            RadiusMeters = location.RadiusMeters,
            Message = inside ? "Station is within the assigned location." : "This scan station is not in the specific location set for this site."
        });
    }

    [HttpPost("current/{id:int}/scan")]
    public async Task<IActionResult> Scan(int id, [FromBody] ScanCaptureRequest req)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        if (req is null)
        {
            await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanStationScanRejected", nameof(ScanStation), id, "Rejected scan: invalid payload.");
            return BadRequest(new { ok = false, message = "Invalid scan payload." });
        }

        var hasQrData = !string.IsNullOrWhiteSpace(req.QrData);
        var hasIdInput = !string.IsNullOrWhiteSpace(req.EmployeeIdInput);
        if (!hasQrData && !hasIdInput)
        {
            await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanStationScanRejected", nameof(ScanStation), id, "Rejected scan: no QR or ID payload.");
            return BadRequest(new { ok = false, message = "Please scan a valid QR code or enter a valid employee ID." });
        }

        if (hasQrData && hasIdInput)
        {
            await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanStationScanRejected", nameof(ScanStation), id, "Rejected scan: QR and ID were both provided.");
            return BadRequest(new { ok = false, message = "Submit either QR scan or manual ID entry, not both at the same time." });
        }

        var config = await GetAttendanceConfigAsync(orgId.Value);
        if (IsBiometricOnly(config.PrimaryMode))
        {
            await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanStationScanRejected", nameof(ScanStation), id, "Rejected scan: primary mode is Biometric.");
            return BadRequest(new { ok = false, message = "Attendance capture is set to Biometric + Geofencing. Scan Station is unavailable in this mode." });
        }

        var testMode = EnableRapidScanTestMode && req.TestMode;

        var station = await _db.ScanStations.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == orgId.Value && x.ScanStationID == id && x.IsActive);
        if (station is null)
            return NotFound(new { ok = false, message = "Station not found or inactive." });

        var location = await _db.Locations.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == orgId.Value && x.LocationID == station.LocationID);
        if (location is null || !location.IsActive)
            return BadRequest(new { ok = false, message = "Assigned site is not available." });

        if (hasQrData && !station.QrEnabled)
        {
            await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanStationScanRejected", nameof(ScanStation), id, "Rejected scan: QR disabled on station.");
            return BadRequest(new { ok = false, message = "QR scan is disabled for this station." });
        }

        var distance = ComputeDistanceMeters((double)location.GeoLat, (double)location.GeoLong, (double)req.Latitude, (double)req.Longitude);
        if (config.EnforceGeofence && distance > location.RadiusMeters)
        {
            await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanStationScanRejected", nameof(ScanStation), id,
                $"Rejected scan: outside geofence (distance {Math.Round(distance, 2)}m > radius {location.RadiusMeters}m).");
            return BadRequest(new { ok = false, message = "This scan station is not in the specific location set for this site." });
        }

        Employee? employee = null;
        if (hasQrData)
        {
            var token = ExtractQrToken(req.QrData);
            if (string.IsNullOrWhiteSpace(token) || token.Length < 16)
            {
                await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanStationScanRejected", nameof(ScanStation), id, "Rejected scan: invalid QR token format.");
                return BadRequest(new { ok = false, message = "Invalid QR code. Please use a valid employee attendance QR." });
            }

            if (!string.IsNullOrWhiteSpace(token))
                employee = await _db.Employees.FirstOrDefaultAsync(e => e.OrgID == orgId.Value && e.AttendanceQrToken == token && !e.IsArchived);

            if (employee is null)
            {
                await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanStationScanRejected", nameof(ScanStation), id, "Rejected scan: QR token does not map to employee.");
                return BadRequest(new { ok = false, message = "Invalid QR code. Employee record was not found." });
            }
        }

        if (employee is null && hasIdInput)
        {
            if (!config.AllowManualEntry || !station.IdEntryEnabled)
            {
                await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanStationScanRejected", nameof(ScanStation), id, "Rejected scan: manual entry disabled by org settings.");
                return BadRequest(new { ok = false, message = "Manual ID entry is disabled by organization settings." });
            }

            var idInput = req.EmployeeIdInput.Trim();
            employee = await _db.Employees.FirstOrDefaultAsync(e => e.OrgID == orgId.Value && !e.IsArchived && (e.EmpNumber == idInput || e.EmpID.ToString() == idInput));
        }

        if (employee is null)
        {
            await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanStationScanRejected", nameof(ScanStation), id, "Rejected scan: employee not found.");
            return BadRequest(new { ok = false, message = "Employee not found for this scan." });
        }

        var now = ResolveCaptureTimestamp(req.DeviceTimestamp);
        var today = DateOnly.FromDateTime(now);

        var hasShiftWindow = TryResolveShiftWindow(orgId.Value, employee, now, config, out var shiftWindow, out var windowMessage);
        if (!hasShiftWindow && !testMode)
        {
            await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanStationScanRejected", nameof(ScanStation), id,
                $"Rejected scan for employee #{employee.EmpID}: {windowMessage ?? "No shift configured."}");
            return BadRequest(new { ok = false, message = windowMessage ?? "No shift configured for this employee today." });
        }

        if (!testMode)
            await ApplyAutoClockOutForEmployeeAsync(orgId.Value, employee.EmpID, config);

        var openLog = await _db.AttendanceLogs
            .Where(a => a.OrgID == orgId.Value && a.EmpID == employee.EmpID && a.LogDate == today)
            .OrderByDescending(a => a.AttendanceID)
            .FirstOrDefaultAsync();

        var canTimeInFrom = hasShiftWindow
            ? shiftWindow.Start.AddMinutes(-(config.EarliestClockInMinutes ?? 0))
            : now;
        var canTimeInUntil = hasShiftWindow
            ? shiftWindow.Start.AddMinutes(config.LatestClockInMinutes ?? 0)
            : now;

        if (openLog is null || !openLog.TimeIn.HasValue || openLog.TimeOut.HasValue)
        {
            var lateMinutes = hasShiftWindow ? Math.Max(0, (int)Math.Floor((now - canTimeInUntil).TotalMinutes)) : 0;
            var earlyMinutes = hasShiftWindow ? Math.Max(0, (int)Math.Floor((canTimeInFrom - now).TotalMinutes)) : 0;
            var timingTag = lateMinutes > 0
                ? $"LATE_{lateMinutes}m"
                : (earlyMinutes > 0 ? $"EARLY_{earlyMinutes}m" : null);

            var log = new AttendanceLog
            {
                OrgID = orgId.Value,
                EmpID = employee.EmpID,
                LogDate = today,
                TimeIn = now,
                MethodIn = string.IsNullOrWhiteSpace(req.EmployeeIdInput) ? "QR" : "ManualID",
                GeoLat = req.Latitude,
                GeoLong = req.Longitude,
                GeoStatus = distance <= location.RadiusMeters ? "INSIDE" : "OUTSIDE",
                DeviceInfo = req.DeviceInfo?.Trim(),
                Remarks = string.IsNullOrWhiteSpace(timingTag)
                    ? ("Station: " + station.StationName)
                    : ("Station: " + station.StationName + " | " + timingTag)
            };
            _db.AttendanceLogs.Add(log);
            await _db.SaveChangesAsync();
            await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanAttendanceRecorded", nameof(AttendanceLog), log.AttendanceID,
                $"Time-in recorded for employee #{employee.EmpID} at station '{station.StationName}' via {(string.IsNullOrWhiteSpace(req.EmployeeIdInput) ? "QR" : "ManualID")}.");
            var msg = lateMinutes > 0
                ? $"Time in recorded at {now:hh:mm tt}. Marked late by {lateMinutes} minute(s)."
                : (earlyMinutes > 0
                    ? $"Time in recorded at {now:hh:mm tt}."
                    : $"Time in recorded at {now:hh:mm tt}.");
            return Ok(new { ok = true, action = "TIME_IN", lateMinutes, earlyMinutes, message = msg });
        }

        if (config.PreventDoubleClockIn && openLog.TimeIn.HasValue && !openLog.TimeOut.HasValue)
        {
            var minutesSinceIn = (now - openLog.TimeIn.Value).TotalMinutes;
            if (!testMode && minutesSinceIn >= 0 && minutesSinceIn < 1.5)
            {
                await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanStationScanRejected", nameof(AttendanceLog), openLog.AttendanceID,
                    $"Rejected duplicate time-in scan for employee #{employee.EmpID}; open log still active.");
                return BadRequest(new { ok = false, message = "You are already timed in. Please wait before scanning again to avoid duplicate time-in." });
            }
        }

        if (!testMode && hasShiftWindow && now < shiftWindow.Start)
        {
            await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanStationScanRejected", nameof(AttendanceLog), openLog.AttendanceID,
                $"Rejected time-out for employee #{employee.EmpID}: attempted before shift start {shiftWindow.Start:hh:mm tt}.");
            return BadRequest(new { ok = false, message = $"Clock-out is available after shift start at {shiftWindow.Start:hh:mm tt}." });
        }

        openLog.TimeOut = now;
        openLog.MethodOut = string.IsNullOrWhiteSpace(req.EmployeeIdInput) ? "QR" : "ManualID";
        openLog.GeoLat = req.Latitude;
        openLog.GeoLong = req.Longitude;
        openLog.GeoStatus = distance <= location.RadiusMeters ? "INSIDE" : "OUTSIDE";
        openLog.DeviceInfo = req.DeviceInfo?.Trim();
        if (string.IsNullOrWhiteSpace(openLog.Remarks))
            openLog.Remarks = "Station: " + station.StationName;
        await _db.SaveChangesAsync();

        var overtimeHours = (!testMode && hasShiftWindow)
            ? await TryCreateOrUpdateAutoOvertimeAsync(orgId.Value, employee.EmpID, shiftWindow.End, now, openLog.MethodOut ?? "QR")
            : 0m;
        if (testMode)
            overtimeHours = await ApplyTestModeOutcomesAsync(orgId.Value, employee.EmpID, openLog, now, overtimeHours);
        await TryAuditTenantAsync(orgId.Value, GetUserId(), "ScanAttendanceRecorded", nameof(AttendanceLog), openLog.AttendanceID,
            $"Time-out recorded for employee #{employee.EmpID} at station '{station.StationName}' via {(string.IsNullOrWhiteSpace(req.EmployeeIdInput) ? "QR" : "ManualID")}.");
        var message = overtimeHours > 0
            ? $"Time out recorded at {now:hh:mm tt}. Overtime detected: {overtimeHours:0.##} hour(s)."
            : $"Time out recorded at {now:hh:mm tt}.";
        return Ok(new { ok = true, action = "TIME_OUT", overtimeHours, message });
    }

    private async Task<(bool ok, ActionResult? result)> ValidateCreateUpdate(CreateScanStationRequest req, int orgId, int? existingId)
    {
        if (req is null)
            return (false, BadRequest(new { ok = false, message = "Invalid payload." }));
        var name = (req.StationName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return (false, BadRequest(new { ok = false, message = "Station name is required." }));
        if (name.Length > 120)
            return (false, BadRequest(new { ok = false, message = "Station name is too long." }));

        var location = await _db.Locations.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == orgId && x.LocationID == req.LocationID);
        if (location is null)
            return (false, BadRequest(new { ok = false, message = "Please choose a valid site." }));

        var duplicateName = await _db.ScanStations.AnyAsync(x => x.OrgID == orgId && x.StationName.ToLower() == name.ToLower() && (!existingId.HasValue || x.ScanStationID != existingId.Value));
        if (duplicateName)
            return (false, Conflict(new { ok = false, message = "A station with this name already exists." }));

        return (true, null);
    }

    private async Task<OrgAttendanceConfig> GetAttendanceConfigAsync(int orgId)
    {
        var cfg = await _db.OrgAttendanceConfigs.FirstOrDefaultAsync(x => x.OrgID == orgId);
        if (cfg is not null)
            return cfg;
        return new OrgAttendanceConfig
        {
            OrgID = orgId,
            PrimaryMode = "QR",
            AllowManualEntry = false,
            EnforceGeofence = true,
            EarliestClockInMinutes = 60,
            LatestClockInMinutes = 15,
            PreventDoubleClockIn = true
        };
    }

    private bool TryResolveShiftWindow(int orgId, Employee employee, DateTime nowUtc, OrgAttendanceConfig config, out ShiftWindow window, out string? message)
    {
        var date = nowUtc.Date;
        var assignment = _db.ShiftAssignments.AsNoTracking()
            .FirstOrDefault(s => s.OrgID == orgId && s.EmpID == employee.EmpID && s.AssignmentDate == date);
        if (assignment is not null)
        {
            var start = date.Add(assignment.StartTime);
            var end = date.Add(assignment.EndTime);
            if (end <= start) end = end.AddDays(1);
            window = new ShiftWindow(start, end);
            message = null;
            return true;
        }

        var templateCode = !string.IsNullOrWhiteSpace(employee.AssignedShiftTemplateCode)
            ? employee.AssignedShiftTemplateCode
            : _db.Departments.AsNoTracking().Where(d => d.DepID == employee.DepartmentID).Select(d => d.DefaultShiftTemplateCode).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(templateCode))
            templateCode = config.DefaultShiftTemplateCode;

        if (string.IsNullOrWhiteSpace(templateCode))
        {
            window = default;
            message = "No shift configured for this employee today.";
            return false;
        }

        var template = _db.OrgShiftTemplates.AsNoTracking()
            .Include(t => t.WorkDays)
            .FirstOrDefault(t => t.OrgID == orgId && t.Code == templateCode && t.IsActive);
        if (template is null || string.IsNullOrWhiteSpace(template.FixedStartTime) || string.IsNullOrWhiteSpace(template.FixedEndTime))
        {
            window = default;
            message = "Shift template is not fully configured.";
            return false;
        }

        var dayCode = date.DayOfWeek.ToString()[..3].ToUpperInvariant();
        if (template.WorkDays.Count > 0 && !template.WorkDays.Any(x => x.DayCode.Trim().ToUpperInvariant().StartsWith(dayCode, StringComparison.Ordinal)))
        {
            window = default;
            message = "No assigned shift for today.";
            return false;
        }

        if (!TimeSpan.TryParse(template.FixedStartTime, out var startTs) || !TimeSpan.TryParse(template.FixedEndTime, out var endTs))
        {
            window = default;
            message = "Shift template time format is invalid.";
            return false;
        }

        var shiftStart = date.Add(startTs);
        var shiftEnd = date.Add(endTs);
        if (shiftEnd <= shiftStart) shiftEnd = shiftEnd.AddDays(1);
        window = new ShiftWindow(shiftStart, shiftEnd);
        message = null;
        return true;
    }

    private static string ExtractQrToken(string rawQr)
    {
        var value = (rawQr ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return value;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return value;
        return segments[^1];
    }

    private static string NormalizeScheduleMode(string? mode)
    {
        var value = (mode ?? "Always").Trim().ToLowerInvariant();
        return value switch
        {
            "scheduled" => "Scheduled",
            "manual" => "Manual",
            _ => "Always"
        };
    }

    private static string BuildStationCode(string input)
    {
        var chars = (input ?? "Station").ToUpperInvariant().Where(c => char.IsLetterOrDigit(c)).Take(16).ToArray();
        var core = new string(chars);
        if (string.IsNullOrWhiteSpace(core))
            core = "STATION";
        return core + "-" + DateTime.UtcNow.ToString("HHmmss");
    }

    private static double ComputeDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return 6371000 * c;
    }

    private static double ToRad(double value) => value * Math.PI / 180.0;

    private async Task ApplyAutoClockOutForEmployeeAsync(int orgId, int empId, OrgAttendanceConfig config)
    {
        if (!config.AutoClockOutEnabled || !config.AutoClockOutMaxHours.HasValue || config.AutoClockOutMaxHours.Value <= 0)
            return;

        var now = DateTime.UtcNow;
        var max = TimeSpan.FromHours((double)config.AutoClockOutMaxHours.Value);
        var openLogs = await _db.AttendanceLogs
            .Where(x => x.OrgID == orgId && x.EmpID == empId && x.TimeIn.HasValue && !x.TimeOut.HasValue)
            .ToListAsync();

        if (openLogs.Count == 0)
            return;

        foreach (var row in openLogs)
        {
            if (!row.TimeIn.HasValue)
                continue;
            var autoOutAt = row.TimeIn.Value.Add(max);
            if (autoOutAt > now)
                continue;

            row.TimeOut = autoOutAt;
            row.MethodOut = "AUTO";
            row.Remarks = string.IsNullOrWhiteSpace(row.Remarks)
                ? "Auto clock-out applied by organization policy."
                : row.Remarks + " | Auto clock-out applied by organization policy.";

            if (TryResolveShiftWindow(orgId, await _db.Employees.AsNoTracking().FirstAsync(e => e.EmpID == empId), autoOutAt, config, out var shiftWindow, out _))
                await TryCreateOrUpdateAutoOvertimeAsync(orgId, empId, shiftWindow.End, autoOutAt, "AUTO");
        }

        await _db.SaveChangesAsync();
    }

    private async Task<decimal> TryCreateOrUpdateAutoOvertimeAsync(int orgId, int empId, DateTime scheduledEnd, DateTime actualEnd, string methodOut)
    {
        var policy = await _db.OrgAttendanceOvertimePolicies.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == orgId);
        if (policy is null || !policy.Enabled)
            return 0m;

        var overtimeMinutes = (actualEnd - scheduledEnd).TotalMinutes - policy.MicroOtBufferMinutes;
        if (overtimeMinutes <= 0)
            return 0m;

        var roundedMinutes = ApplyRounding(overtimeMinutes, policy.RoundingMinutes, policy.RoundingDirection);
        if (roundedMinutes < Math.Max(1, policy.MinimumBlockMinutes))
            return 0m;

        var totalHours = Math.Round((decimal)roundedMinutes / 60m, 2);
        var date = DateOnly.FromDateTime(actualEnd);
        var existing = await _db.OvertimeRequests
            .FirstOrDefaultAsync(x => x.OrgID == orgId && x.EmpID == empId && x.OTDate == date && (x.Status == "Pending" || x.Status == "SystemGenerated" || x.Status == "NeedsReview"));

        var status = policy.AutoApprove
            ? "Approved"
            : string.Equals(methodOut, "AUTO", StringComparison.OrdinalIgnoreCase) ? "NeedsReview" : "SystemGenerated";

        if (existing is null)
        {
            var row = new OvertimeRequest
            {
                OrgID = orgId,
                EmpID = empId,
                OTDate = date,
                StartTime = scheduledEnd,
                EndTime = actualEnd,
                TotalHours = totalHours,
                Reason = $"AUTO: Computed from attendance clock-out ({methodOut}).",
                Status = status,
                ApprovedAt = status == "Approved" ? DateTime.UtcNow : null,
                ApprovedByUserID = status == "Approved" ? GetUserId() : null
            };
            _db.OvertimeRequests.Add(row);
        }
        else
        {
            existing.StartTime = scheduledEnd;
            existing.EndTime = actualEnd;
            existing.TotalHours = totalHours;
            existing.Reason = $"AUTO: Computed from attendance clock-out ({methodOut}).";
            existing.Status = status;
            existing.ApprovedAt = status == "Approved" ? DateTime.UtcNow : null;
            existing.ApprovedByUserID = status == "Approved" ? GetUserId() : null;
        }

        await _db.SaveChangesAsync();
        await TryAuditTenantAsync(orgId, GetUserId(), "OvertimeAutoGenerated", nameof(OvertimeRequest), empId,
            $"Auto overtime generated for employee #{empId}: {totalHours:0.##} hours ({methodOut}).");
        return totalHours;
    }

    private static int ApplyRounding(double minutes, int roundingMinutes, string? direction)
    {
        if (roundingMinutes <= 0)
            return (int)Math.Round(minutes, MidpointRounding.AwayFromZero);

        var blocks = minutes / roundingMinutes;
        var mode = (direction ?? "NEAREST").Trim().ToUpperInvariant();
        var roundedBlocks = mode switch
        {
            "UP" => Math.Ceiling(blocks),
            "DOWN" => Math.Floor(blocks),
            _ => Math.Round(blocks, MidpointRounding.AwayFromZero)
        };
        return (int)(roundedBlocks * roundingMinutes);
    }

    private async Task<decimal> ApplyTestModeOutcomesAsync(int orgId, int empId, AttendanceLog openLog, DateTime actualOut, decimal currentOvertimeHours)
    {
        if (!openLog.TimeIn.HasValue)
            return currentOvertimeHours;

        var elapsedSeconds = (actualOut - openLog.TimeIn.Value).TotalSeconds;
        var overtimeHours = currentOvertimeHours;

        if (elapsedSeconds >= 20)
        {
            var testHours = Math.Round((decimal)elapsedSeconds / 3600m, 4);
            var date = DateOnly.FromDateTime(actualOut);
            var existing = await _db.OvertimeRequests
                .FirstOrDefaultAsync(x => x.OrgID == orgId && x.EmpID == empId && x.OTDate == date && x.Reason != null && x.Reason.StartsWith("AUTO_TEST:", StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                _db.OvertimeRequests.Add(new OvertimeRequest
                {
                    OrgID = orgId,
                    EmpID = empId,
                    OTDate = date,
                    StartTime = openLog.TimeIn.Value,
                    EndTime = actualOut,
                    TotalHours = testHours,
                    Reason = "AUTO_TEST: Overtime generated for rapid scan test mode.",
                    Status = "SystemGenerated"
                });
            }
            else
            {
                existing.StartTime = openLog.TimeIn.Value;
                existing.EndTime = actualOut;
                existing.TotalHours = testHours;
                existing.Status = "SystemGenerated";
            }

            overtimeHours = Math.Max(overtimeHours, testHours);
        }

        if (elapsedSeconds < 14)
        {
            var tag = "AUTO_TEST_UNDERTIME";
            if (string.IsNullOrWhiteSpace(openLog.Remarks))
                openLog.Remarks = tag;
            else if (!openLog.Remarks.Contains(tag, StringComparison.OrdinalIgnoreCase))
                openLog.Remarks += " | " + tag;
        }

        await _db.SaveChangesAsync();
        return overtimeHours;
    }

    private static DateTime ResolveCaptureTimestamp(string? deviceTimestamp)
    {
        if (!string.IsNullOrWhiteSpace(deviceTimestamp))
        {
            if (DateTime.TryParse(deviceTimestamp, out var parsedDateTime))
                return DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Unspecified);

            if (DateTimeOffset.TryParse(deviceTimestamp, out var parsedOffset))
                return DateTime.SpecifyKind(parsedOffset.DateTime, DateTimeKind.Unspecified);
        }
        return DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
    }

    private static bool IsBiometricOnly(string? mode)
        => string.Equals((mode ?? string.Empty).Trim(), "Biometric", StringComparison.OrdinalIgnoreCase);

    private int? GetUserOrgId()
    {
        var claim = User.FindFirstValue("orgId");
        return int.TryParse(claim, out var id) ? id : null;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private readonly record struct ShiftWindow(DateTime Start, DateTime End);

    private async Task TryAuditTenantAsync(int orgId, int? userId, string action, string? entityName, int? entityId, string? details)
    {
        try
        {
            await _audit.LogTenantAsync(orgId, userId, action, entityName, entityId, details,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }
        catch
        {
            // do not fail business flow on audit issues
        }
    }

    public class ScanStationDto
    {
        public int ScanStationID { get; set; }
        public string StationCode { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public int LocationID { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public bool QrEnabled { get; set; }
        public bool IdEntryEnabled { get; set; }
        public string ScheduleMode { get; set; } = "Always";
        public bool IsActive { get; set; }
    }

    public class CreateScanStationRequest
    {
        public int LocationID { get; set; }
        public string StationName { get; set; } = string.Empty;
        public bool QrEnabled { get; set; } = true;
        public string ScheduleMode { get; set; } = "Always";
    }

    public class UpdateScanStationRequest
    {
        public int? LocationID { get; set; }
        public string? StationName { get; set; }
        public bool? QrEnabled { get; set; }
        public string? ScheduleMode { get; set; }
    }

    public class SetScanStationStatusRequest
    {
        public bool IsActive { get; set; }
    }

    public class ValidateLocationRequest
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }

    public class LocationValidationResponse
    {
        public bool Ok { get; set; }
        public bool InsideRadius { get; set; }
        public double DistanceMeters { get; set; }
        public int RadiusMeters { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ScanCaptureRequest
    {
        public string? QrData { get; set; }
        public string? EmployeeIdInput { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? DeviceInfo { get; set; }
        public string? DeviceTimestamp { get; set; }
        public bool TestMode { get; set; }
    }
}
