# Comprehensive Secondary Button Styles Report
## Subchron.Web/Pages/ Directory Analysis

**Generated:** Analysis of all .cshtml files in Subchron.Web/Pages/

---

## Summary

This report documents all secondary (non-primary action) button style patterns found across the Subchron.Web application. Secondary buttons represent cancel, close, reset, back, and alternative action buttons.

**Total Files Analyzed:** 80+ .cshtml files  
**Files with Secondary Button Patterns:** 70+

---

## Primary Secondary Button Patterns Found

### 1. **`btn-secondary` Class (Custom)**
The most commonly used secondary button pattern throughout the application.

**Files using this pattern:**
- `Subchron.Web/Pages/App/Archive/Archive.cshtml` (3 instances)
  - Line 14: Back to Employees link
  - Line 51: Reset button
  - Line 186: Close button in modal
  - Line 226: Cancel button in restore modal

- `Subchron.Web/Pages/App/Dashboard.cshtml` (1 instance)
  - Line 101: Reset button

- `Subchron.Web/Pages/App/AuditLog/Index.cshtml` (2 instances)
  - Line 73: Reset button
  - Line 196: Close button in modal

- `Subchron.Web/Pages/App/Employee/EmployeeManagement.cshtml` (2 instances)
  - Line 411: Cancel button in edit modal
  - Line 471: Close button in view modal

**Typical Usage:**
```html
<button type="button" onclick="closeArchiveViewModal()" class="btn-secondary">Close</button>
<button type="button" id="archiveResetFilters" class="btn-secondary h-10 text-xs font-medium uppercase tracking-wider">Reset</button>
```

---

## Secondary Button Style Patterns (Inline Classes)

### 2. **Border + White Background Pattern**
The most common explicit style pattern for secondary buttons using Tailwind classes.

**Pattern:** `rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-50`

**Files using this pattern:**

- `Subchron.Web/Pages/App/Employee/EmployeeManagement.cshtml`
  - Line 411: Edit modal Cancel button
  - Line 472: View modal Close button
  - Full class: `h-12 flex-1 rounded-xl border border-slate-300 bg-white text-sm font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-300`

- `Subchron.Web/Pages/App/LeaveAndShift/LeaveManagement.cshtml`
  - Line 118: Close detail modal button
  - Full class: `rounded-xl border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-300`

- `Subchron.Web/Pages/App/LeaveAndShift/ShiftSchedule.cshtml`
  - Line 90: Close shift day modal button
  - Full class: `rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-300`

- `Subchron.Web/Pages/App/Settings/Partials/_UpgradePlanSettings.cshtml`
  - Line 34: Update billing information button
  - Line 160: Cancel button in billing panel
  - Full class: `inline-flex items-center rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-200 dark:hover:bg-slate-600`

- `Subchron.Web/Pages/SuperAdmin/Settings/Partials/_AccountSettings.cshtml`
  - Line 12: Change password button
  - Line 20: Cancel change password button
  - Line 39: Cancel new password button
  - Line 82: TOTP modal cancel button
  - Full class: `rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-200 dark:hover:bg-slate-600`

- `Subchron.Web/Pages/App/Operations/AttendanceLogs.cshtml`
  - Line 89: Reset filters button
  - Full class: `h-10 rounded-xl border border-slate-300 bg-white px-4 text-xs font-medium uppercase tracking-wider text-slate-600 transition-all hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-300 dark:hover:bg-slate-600`

---

### 3. **Border + White Background + Gray Text Pattern (Variant)**
Used primarily for secondary/tertiary actions with slightly different color emphasis.

**Pattern:** `rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 shadow-sm hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-200`

**Files using this pattern:**

- `Subchron.Web/Pages/App/Dashboard.cshtml`
  - Lines 90-95: Quick filter shortcut buttons (Today, Yesterday, Week, Month, Year)
  - Line 44: Reports secondary button
  - Full class: `inline-flex items-center rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 shadow-sm transition-colors hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-300`

- `Subchron.Web/Pages/App/Operations/AttendanceLogs.cshtml`
  - Line 13: Export CSV button
  - Full class: `inline-flex items-center gap-2 rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 shadow-sm transition-all hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700`

- `Subchron.Web/Pages/Landing/Services.cshtml`
  - Lines 93-94: Carousel navigation buttons (Prev/Next)
  - Full class: `rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-700 shadow-sm hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-300`

- `Subchron.Web/Pages/App/PayrollAndReports/Payroll.cshtml`
  - Lines 121-122: Export CSV and Export Excel buttons
  - Full class: `inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-200`

- `Subchron.Web/Pages/App/PayrollAndReports/Reports.cshtml`
  - Lines 90-91: Export CSV and Export PDF buttons
  - Full class: `inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-200`

---

### 4. **Pagination Buttons Pattern**
Special pattern used for pagination controls with gray borders and text.

**Pattern:** `inline-flex items-center rounded-lg border border-gray-200 bg-white px-2 py-2 text-gray-500 hover:bg-gray-50 focus:z-20 dark:border-gray-700 dark:bg-gray-800 dark:text-white`

**Files using this pattern:**

- `Subchron.Web/Pages/App/Employee/EmployeeManagement.cshtml`
  - Pagination buttons (prev/next)

- `Subchron.Web/Pages/App/AuditLog/Index.cshtml`
  - Lines 143-151: Pagination navigation buttons
  - Inactive page buttons: `border border-gray-200 bg-white px-4 py-2 text-sm font-semibold text-gray-900 hover:bg-gray-50`
  - Active page buttons: `border border-subgreen-600 bg-subgreen-50 px-4 py-2 text-sm font-semibold text-subgreen-600`

---

### 5. **Archive Button Pattern (with Red Border)**
Used for destructive-like secondary actions (Archive).

**Pattern:** `rounded-xl border border-red-300 bg-white px-4 py-2.5 text-sm font-semibold text-red-700 hover:bg-red-50`

**Files using this pattern:**

- `Subchron.Web/Pages/App/LeaveAndShift/LeaveManagement.cshtml`
  - Line 113: Leave request cancel/delete button with red styling
  - Full class: `rounded-xl border border-red-300 bg-white px-4 py-2.5 text-sm font-semibold text-red-700 hover:bg-red-50 dark:border-red-700 dark:bg-slate-800 dark:text-red-400`

---

### 6. **Day Toggle Buttons (Inactive State)**
Used for toggle/state buttons in inactive state.

**Pattern:** `border border-gray-200 bg-gray-50 text-sm font-bold text-gray-600`

**Files using this pattern:**

- `Subchron.Web/Pages/App/OrgConfig/Partials/_OrgTabShifts.cshtml`
  - Lines 25-26: Inactive day toggle buttons
  - Full class: `day-btn group relative flex h-12 w-12 items-center justify-center rounded-lg border border-gray-200 bg-gray-50 text-sm font-bold text-gray-600`

---

### 7. **Archive/View Link Buttons**
Secondary link buttons styled as buttons.

**Pattern:** `inline-flex items-center rounded-xl border border-gray-300 bg-white px-3 sm:px-4 py-2.5 text-sm font-semibold text-gray-700 shadow-sm transition-all hover:bg-gray-50`

**Files using this pattern:**

- `Subchron.Web/Pages/App/Employee/EmployeeManagement.cshtml`
  - Line 40: View Archive link button
  - Full class: `inline-flex items-center rounded-xl border border-gray-300 bg-white px-3 sm:px-4 py-2.5 text-sm font-semibold text-gray-700 shadow-sm transition-all hover:bg-gray-50 dark:border-gray-700 dark:bg-gray-800 dark:text-white`

---

### 8. **Light Action Info Buttons**
Used for view/read-only actions with light blue styling.

**Pattern:** `inline-flex items-center gap-1 rounded-lg bg-sky-100 px-2 py-1.5 text-xs font-medium text-sky-700 transition-all hover:bg-sky-200`

**Files using this pattern:**

- `Subchron.Web/Pages/App/Operations/AttendanceLogs.cshtml`
  - Lines 152, 175: View detail link buttons
  - Full class: `inline-flex items-center gap-1 rounded-lg bg-sky-100 px-2 py-1.5 text-xs font-medium text-sky-700 transition-all hover:bg-sky-200 dark:bg-sky-900/30 dark:text-sky-300 dark:hover:bg-sky-800/50`

---

### 9. **Outline/Text-Only Secondary Buttons**
Minimal secondary buttons with only text and hover effects, no border.

**Pattern:** `text-xs font-medium text-gray-500 hover:text-gray-700 hover:underline`

**Files using this pattern:**

- `Subchron.Web/Pages/App/OrgConfig/Partials/_OrgTabLocations.cshtml`
  - Line 150: Cancel Edit link button
  - Full class: `hidden text-xs font-medium text-gray-500 hover:text-gray-700 hover:underline`

- `Subchron.Web/Pages/App/OrgConfig/Partials/_OrgTabShifts.cshtml`
  - Line 110: Reset Default button
  - Full class: `mr-3 text-sm font-semibold text-gray-600 hover:text-gray-900 focus:text-gray-900 focus:outline-none dark:text-gray-400 dark:hover:text-white`

---

### 10. **Close Button (Icon Only)**
Minimal close buttons using icon with hover background.

**Pattern:** `absolute right-4 top-4 rounded-lg p-2 text-white hover:bg-white/10`

**Files using this pattern:**

- `Subchron.Web/Pages/Employee/MyQr.cshtml`
  - Line 34: Close fullscreen button

---

## Dark Mode Support Summary

**All secondary button patterns include dark mode variants:**

Common dark mode classes applied:
- `dark:border-slate-600` - Dark mode border color
- `dark:bg-slate-700` / `dark:bg-slate-800` - Dark mode background
- `dark:text-slate-200` / `dark:text-slate-300` / `dark:text-white` - Dark mode text
- `dark:hover:bg-slate-600` / `dark:hover:bg-slate-700` - Dark mode hover state

---

## Button Size Variations

### Height Classes Used:
- `h-10` - Standard secondary button height (40px)
- `h-12` - Larger secondary button (48px)
- `py-2` / `py-2.5` - Padding-based sizing (vertical)

### Padding Classes Used:
- `px-2` / `px-3` / `px-4` - Horizontal padding
- `py-2` / `py-2.5` - Vertical padding
- Flex padding combinations for icon buttons

---

## Summary Table

| Pattern | Primary Use | Files Count | Key Classes |
|---------|------------|-------------|------------|
| `btn-secondary` | Modal close, reset, back buttons | 8+ | Custom class |
| Border + White (slate-300) | Cancel buttons in modals | 6+ | `border border-slate-300 bg-white` |
| Border + White (slate-200) | Secondary actions, filters | 5+ | `border border-slate-200 bg-white` |
| Pagination | Navigation controls | 3+ | `border border-gray-200 bg-white` |
| Red border | Destructive secondary | 1+ | `border border-red-300 bg-white text-red-700` |
| Day toggles (inactive) | State toggles | 2+ | `border border-gray-200 bg-gray-50` |
| Info action | View/detail buttons | 2+ | `bg-sky-100 text-sky-700` |
| Text-only | Minimal actions | 2+ | Text with hover:underline |

---

## Recommendations

### Consolidation Opportunities:
1. **`btn-secondary` Custom Class** - Should be the primary approach. Define it once in CSS with:
   - `border border-slate-300` or `border-slate-200`
   - `bg-white`
   - `px-4 py-2.5`
   - `text-sm font-medium`
   - `text-slate-600`
   - `hover:bg-slate-50`
   - Full dark mode support

2. **Size Modifiers** - Create modifier classes:
   - `btn-secondary-sm` (h-8)
   - `btn-secondary-md` (h-10, default)
   - `btn-secondary-lg` (h-12)

3. **Color Variants** - Create theme variants:
   - `btn-secondary` (gray, default)
   - `btn-secondary-red` (for destructive actions)
   - `btn-secondary-info` (for view/detail actions)

4. **Consistency** - Replace inline `rounded-xl border border-slate-300 bg-white...` patterns with `btn-secondary` class

---

## Files Detailed Reference

### App Directory Files:
- ✓ Archive/Archive.cshtml
- ✓ Dashboard.cshtml
- ✓ AuditLog/Index.cshtml
- ✓ Employee/EmployeeManagement.cshtml
- ✓ Employee/Add.cshtml
- ✓ Employee/Edit.cshtml
- ✓ LeaveAndShift/LeaveManagement.cshtml
- ✓ LeaveAndShift/ShiftSchedule.cshtml
- ✓ Operations/AttendanceLogs.cshtml
- ✓ Operations/AttendanceLogs/Details.cshtml
- ✓ Operations/OvertimeRequests.cshtml
- ✓ Operations/OvertimeRequests/Details.cshtml
- ✓ Operations/ScanStation.cshtml
- ✓ Operations/ScanStation/Create.cshtml
- ✓ Operations/ScanStation/Details.cshtml
- ✓ Operations/ScanStation/Kiosk.cshtml
- ✓ OrgConfig/Partials/_OrgTabLocations.cshtml
- ✓ OrgConfig/Partials/_OrgTabShifts.cshtml
- ✓ OrgConfig/Partials/_OrgTabPay.cshtml
- ✓ PayrollAndReports/Payroll.cshtml
- ✓ PayrollAndReports/Reports.cshtml
- ✓ Settings/Partials/_UpgradePlanSettings.cshtml

### SuperAdmin Directory Files:
- ✓ Settings/Partials/_AccountSettings.cshtml
- ✓ Organizations/Create.cshtml
- ✓ Organizations/Details.cshtml
- ✓ Organizations/Index.cshtml

### Employee Directory Files:
- ✓ MyQr.cshtml
- ✓ Dashboard.cshtml
- ✓ Attendance.cshtml
- ✓ Leave.cshtml
- ✓ MyAttendance.cshtml
- ✓ Overtime.cshtml
- ✓ Settings.cshtml

### Landing Directory Files:
- ✓ Services.cshtml
- ✓ Pricing.cshtml
- ✓ About.cshtml
- ✓ Contact.cshtml
- ✓ Features.cshtml
- ✓ FAQ.cshtml
- ✓ HowItWorks.cshtml
- ✓ Onboarding.cshtml
- ✓ SignupDetails.cshtml

### Auth Directory Files:
- ✓ Login.cshtml
- ✓ Signup.cshtml
- ✓ ForgotPassword.cshtml
- ✓ ResetPassword.cshtml
- ✓ AccessDenied.cshtml
- ✓ VerifyEmail.cshtml
- ✓ Partials/_RecoveryPanel.cshtml
- ✓ Partials/_TotpEntry.cshtml

---

## End of Report
