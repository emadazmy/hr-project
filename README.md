# HR ERP Module (WPF + SQL Server) — v2 Schema

A Human Resources module built around a biometric/attendance-device style
schema: `department`, `employee`, `JobPositions`, `Shifts`, `WorkShifts`,
`Attendance`, `attend_summary`, `Holidays`, `Leaves`, plus the existing
`Payroll` module (kept, re-wired to `employee.code`).

## What's included

```
HR_ERP/
├── Database/
│   ├── HR_ERP_Database.sql                       <- run this first in SQL Server (new/rebuilt DB)
│   ├── sp_GenerateMonthlyAttendanceSummary.sql    <- run this second (required for Payroll auto-generate)
│   └── Migration_v3_Documents_LeaveBalance.sql    <- run instead of HR_ERP_Database.sql on an EXISTING db
├── HR_ERP.sln
└── HR_ERP/
    ├── HR_ERP.csproj             <- WPF app, .NET 8, Microsoft.Data.SqlClient, ClosedXML
    ├── App.xaml / App.xaml.cs    <- shared design system (colors, DataGrid style, buttons)
    ├── MainWindow.xaml / .cs     <- left nav + content switcher, opens on Dashboard, auto-sized to screen
    ├── Models/Models.cs          <- Department, JobPosition, Shift, WorkShift, Employee,
    │                                AttendanceRecord, AttendSummary, Holiday, LeaveRequest,
    │                                LeaveBalance, EmployeeDocument, PayrollAdjustment,
    │                                CashAdvance, PayrollRecord
    ├── Data/
    │   ├── DatabaseHelper.cs                 <- connection string lives here
    │   ├── DepartmentRepository.cs           <- Department + JobPosition
    │   ├── ShiftRepository.cs                <- Shift + WorkShift
    │   ├── ShiftDetector.cs                  <- auto-detects Shift from check-in time
    │   ├── EmployeeRepository.cs
    │   ├── EmployeeDocumentRepository.cs     <- Personal Documents Hub (categorized document references)
    │   ├── EmploymentHistoryRepository.cs    <- unified employment history log
    │   ├── DocumentStorage.cs                <- copies files into EmployeeDocuments\ next to the app
    │   ├── AttendanceRepository.cs           <- Attendance + attend_summary
    │   ├── AttendanceCalculator.cs           <- Layer 1: status/late/early/OT from check-in/out + Shift + Weekend/Holidays/Leaves
    │   ├── AttendanceExcelImporter.cs
    │   ├── LeaveRepository.cs                <- LeaveRequest + Holiday + leave balance summary
    │   ├── PayrollAdjustmentRepository.cs    <- Bonuses / Penalties / Incentives
    │   ├── CashAdvanceRepository.cs          <- Cash advances + balance lookup
    │   ├── PayrollRepository.cs
    │   └── PayrollGenerationService.cs       <- automatic monthly payroll: attend_summary + adjustments + advances
    └── Views/                    <- one UserControl per module (XAML + code-behind)
        ├── DashboardView (KPIs, department chart, upcoming holidays, pending leave)
        ├── EmployeesView (+ Department filter, Personal Documents Hub, Employment History)
        ├── DepartmentsView (Departments + Job Positions, filtered by selected department)
        ├── OrgStructureView (department cards: manager + employees)
        ├── ShiftsView (Shifts + WorkShifts side by side)
        ├── AttendanceView (+ Excel import, two preview modes)
        ├── LeaveView (Leave/Mission/Permit requests + Holidays, two tabs, + Leave Balance Summary)
        ├── AdjustmentsView (Bonuses / Penalties / Incentives)
        ├── CashAdvancesView (advances with paid/remaining balance)
        ├── PayrollView (+ printable Payslip)
        ├── PayslipWindow (printable FlowDocument payslip)
        └── DocumentViewerWindow (in-app document preview + rename/description)
```

## Setup steps

**Brand new database:**

1. **Create the database.** Open `Database/HR_ERP_Database.sql` in SSMS (or
   the SQL Server Object Explorer inside Visual Studio) and execute the
   whole script.
2. **Create the monthly summary procedure (required).** Open
   `Database/sp_GenerateMonthlyAttendanceSummary.sql` and execute it too —
   this is what the app calls automatically to turn Attendance rows into a
   monthly summary. Skipping this step will make "Auto-Generate for All
   Employees" in Payroll fail.
3. **Open `HR_ERP.sln`** in Visual Studio 2022 (.NET desktop development
   workload required).
4. **Point the app at your SQL Server** — edit the constants at the top of
   `HR_ERP/Data/DatabaseHelper.cs` (`ServerName`, `DatabaseName`,
   `UseIntegratedSecurity`, etc.).
5. **Restore NuGet packages** (Build → Restore NuGet Packages) —
   `Microsoft.Data.SqlClient` and `ClosedXML` are both required.
6. **Run** (F5). You'll land on a sign-in screen first — use **admin /
   admin123** (see "Users, Permissions & Language" below), then change that
   password from Settings > Users.

**Already have a database from an earlier version of this project?**
`HR_ERP_Database.sql` **drops and recreates every table** — re-running it
wipes your existing data. If you're just missing something added later
(e.g. you see `Invalid object name 'EmployeeDocuments'`, `'PayrollAdjustments'`,
`'CashAdvances'`, or a similar error), run
`Database/Migration_v3_Documents_LeaveBalance.sql` instead — it only adds
what's missing (the `EmployeeDocuments`, `PayrollAdjustments`, and
`CashAdvances` tables, and the `annual_leave_days`/`leave_carried_over`
columns on `employee`) and never touches or drops anything else. Safe to
re-run. If you see `Invalid object name 'Users'` (or `'Roles'`,
`'Permissions'`, `'RolePermissions'`, `'AppSettings'`) — i.e. you had a
database from before this login/permissions feature existed — also run
`Database/Migration_v4_Users_Permissions_Language.sql`; same rules apply
(additive only, safe to re-run).

## Automatic Attendance → Payroll pipeline

This is the core of the "no manual intervention" workflow:

1. **Attendance rows come in** (typed in the Attendance screen, or bulk
   Excel import). Each row already carries `MinutesLate`,
   `MinutesEarlyLeave`, `Overtime`, and `status1`.
2. **Set pay rates once per employee** (Employees screen → new fields:
   Basic Salary, Allowance, Overtime Rate, Late Deduction Rate). This is
   the only manual data entry left — everything else per pay period is
   computed.
3. **Click "Auto-Generate for All Employees" in Payroll**, pick a month.
   This:
   - Calls `dbo.sp_GenerateMonthlyAttendanceSummary` to recompute
     `attend_summary` for every active employee straight from `Attendance`
     + `Leaves` + `Holidays` (present/absent/late/overtime days).
   - For each employee, computes:
     - `BasicSalary` = `employee.basic_salary`
     - `Allowances` = `employee.allowance`
     - `OvertimeAmount` = `attend_summary.overtime_hours × employee.overtime_rate`
     - `Deductions` = `(absent_days × daily_rate) + ((late_hours + early_leave_hours) × employee.late_deduction_rate)`,
       where `daily_rate = basic_salary / days_count` for that month
   - Upserts one `Payroll` row per employee for that month — re-running it
     for the same month updates the numbers instead of duplicating rows,
     and never touches a record already marked **Paid**.
4. Review the generated rows in the grid, then **Mark as Paid** when
   you've actually run the pay run.

The "Save Manual Entry" button/form still exists for one-off manual
payroll rows (e.g. a bonus run, or an employee with no attendance rows
yet) — auto-generation and manual entry write to the same table and don't
conflict.

## Schema notes — what changed from the raw table snippet

The table/column names you provided are kept exactly (`employee.code`,
`Attendance.checkin_date`, `Leaves.Emp_code`, etc.) so this still matches
however your attendance device or migration exports data. On top of that,
to make it "professional":

- **Every table now has a real primary key** (several of the originals had
  none, or an identity column with no `PRIMARY KEY` constraint).
- **Date-like columns that were `varchar`** (`hire_date`, `checkin_date`,
  `HolidayDate`, `StartDate`/`EndDate`, etc.) are now proper `DATE`/`DATETIME`
  columns — sortable, filterable, and validated by SQL Server itself.
- **Foreign keys added**: `employee.depart` → `department.name`,
  `Attendance.employee_code` / `attend_summary.employee_code` /
  `Leaves.Emp_code` / `Payroll.employee_code` → `employee.code`,
  `JobPositions.Depart` → `department.name`, `Attendance.shift_` →
  `Shifts.ShiftID`.
- **`attend_summary` has a `UNIQUE (employee_code, month)` constraint** so
  you can't accidentally get two summary rows for the same person/month.
- **`Leaves` gained a `Status` column** (`Pending`/`Approved`/`Rejected`) —
  the original had no approval workflow field.
- **`Attendance.TWHours`** is still a computed, persisted column
  (`WorkingHours + Overtime`), matching your original design.
- **`JobPositions.JobDescription`** uses `NVARCHAR(MAX)` instead of the
  deprecated `TEXT` type.
- **Payroll** — not in your table list, so it's been kept as a separate
  module but re-wired from `EmployeeID` (int) to `employee_code` (varchar)
  so it follows the same convention as the rest of the schema.

## Look & feel — new

All screens now share one design system (`App.xaml`): a consistent color
palette, rounded flat buttons, styled `DataGrid`s (dark header bar,
alternating row shading, single shared row height/font across every
screen), and status-based row coloring wherever a status makes sense
(Attendance, Leave, Payroll). The left nav got icons, a subtitle, and a
version tag. No behavior changed — this only touches `App.xaml` and each
view's `RowStyle`/header, so if you'd rather have a different color scheme,
everything is driven from the brush resources at the top of `App.xaml`.

## How each screen works

- **Dashboard** – live KPIs and summaries; see the dedicated section below.
- **Employees** – full CRUD against `employee`; Department/Position pickers
  are linked by name/title text (matching how `depart`/`position` are
  stored as free text with FK-by-name constraints, not surrogate IDs). A
  **Department filter** dropdown above the grid narrows the list to one
  department (or "All Departments"). Two collapsed panels below the edit
  form: a categorized **Personal Documents Hub** (National ID, Birth
  Certificate, contracts, etc. — see "Personal Documents Hub" below) and an
  **Employment History** log (career moves, transfers, performance reviews
  — see "Employment History" below).
- **Departments & Positions** – two side-by-side CRUD panels for
  `department` and `JobPositions`. Selecting a department narrows the
  Positions grid to just that department's positions (and pre-fills the
  department on new positions); click "New" on the department side to see
  all positions again.
- **Org Structure** – a card-per-department view of the whole company:
  each department shows its manager (`department.empl_code` resolved to a
  name) and every active employee in it, grouped visually — see
  "Organizational Structure" below.
- **Shifts** – two side-by-side CRUD panels for `Shifts` (tolerance-rule
  based, used by an attendance engine) and `WorkShifts` (simple fixed
  templates).
- **Attendance** – two preview modes: **"One day, all employees"** (pick a
  date, see everyone) or **"One employee, date range"** (pick a person and
  either a custom range or "This Month" for the whole month). The grid
  shows Employee, Date, **Day name**, Check-in/out, **Shift**, Late/**Early
  Leave** minutes, Overtime, Total Hours, Status, and Approved — and rows
  are color-coded by status (late = amber, absent = red, leave = blue,
  holiday = green, weekend = gray) with a live summary strip
  (present/late/absent/leave counts + total overtime) above the edit form.
  **Import from Excel** — "Download Template" gives a blank `.xlsx` with
  columns `EmployeeCode, CheckinDate, Checkin, CheckoutDate, Checkout,
  Shift`; "Import from Excel..." bulk-loads it, matching employees by
  `code` and Shift by name (or auto-detecting it — see below), upserting on
  `(employee_code, checkin_date)` so re-imports are safe. There is no
  Status column — status/late/early leave/overtime are computed
  automatically per row, same as manual entry (see below).
- **Leave & Holidays** – now one screen with two tabs (previously two
  separate screens). Tab 1: submit/approve/reject Leave/Mission/Permit
  requests against `Leaves`, rows color-coded by status, plus a collapsed
  **"Employee Leave Balance Summary"** panel — see "Leave Balance Summary"
  below. Tab 2: the same `Holidays` CRUD that used to be its own screen.
- **Bonuses & Penalties** – one-off Bonus/Penalty/Incentive amounts per
  employee per month against the new `PayrollAdjustments` table — see
  "Bonuses, Penalties & Incentives" below.
- **Cash Advances** – salary advances with automatic monthly-installment
  deduction and a live paid/remaining balance — see "Cash Advances" below.
- **Payroll** – unchanged workflow (generate a period, mark as paid), now
  looked up by `employee_code` instead of an integer employee ID, rows
  color-coded Pending/Paid, plus a printable payslip per record (see below).

## Auto-detected screen resolution — new

`MainWindow` no longer opens at a fixed pixel size. On startup it reads
`SystemParameters.WorkArea` (the actual screen's usable area, accounting
for the taskbar) and sizes itself to 90% of that, capped at 1600×950 so it
doesn't feel sparse on a large monitor — and if the screen is small enough
that even the capped size wouldn't fit comfortably (under ~1000×650), it
maximizes instead. Every view was already built with `Grid` star-sizing and
`ScrollViewer`s rather than fixed-size panels, so they reflow correctly at
whatever size this resolves to — this only changes the window's starting
size, not each view's internal layout.

## Organizational Structure — updated: main departments + sub-departments

`department` gained a self-referencing `parent_code` column — a department
with `parent_code = NULL` is a **main department**; one with `parent_code`
set to another department's code is a **sub-department** of it (set via the
new "Parent Department" picker on the Departments & Positions screen; the
dropdown excludes the department itself, and saving is blocked if it would
create a two-department cycle).

The **Org Structure** screen (`Views/OrgStructureView`) now reflects this:
one outer card per **main department** (manager + any employees directly in
it), with each of its **sub-departments** rendered as a nested block inside
that same card (own manager, own employee list, visually indented with a
"↳" marker). The employee count on the main card is the total across it and
all its sub-departments. This is two levels deep by design — main
department → sub-department — not arbitrary nesting, matching what was
asked for; say the word if you need deeper hierarchies later.

Employees whose `depart` doesn't match any department (main or sub) land in
a catch-all "(No Department)" card, rather than being silently dropped, so
data-entry mismatches are visible instead of hidden. No new employee-side
tables — this is entirely computed from `department` + `employee`.

## Org Chart Modeling: Reporting Lines + Vacant Roles — new

The Org Structure screen is now a `TabControl` with three tabs:

- **By Department** — the card view above.
- **Reporting Lines** — a real manager → direct-reports tree, built from a
  new `employee.manager_code` self-referencing column (set via the new
  "Reports To" picker on the Employees screen). This is a genuinely
  different relationship from the department hierarchy: it tracks who
  each *individual* reports to, at arbitrary depth (VP → Director →
  Manager → Employee...), not just which department someone sits in.
  Rendered with a native WPF `TreeView`/`HierarchicalDataTemplate`, so it
  handles any depth automatically. Employees with no manager set (or whose
  manager isn't an active employee) become top-level nodes. Saving on the
  Employees screen blocks an obvious one-level cycle (A reports to B, whose
  manager is already A); deeper cycles aren't blocked at save time, but the
  tree-building code tracks visited employees and simply stops recursing if
  it ever finds one, so a cycle can't hang the screen — it just won't
  render past the loop point.
- **Vacant Roles** — every `JobPositions` row where no active employee's
  `(depart, position)` matches it. This is a computed comparison, not a
  headcount/budget field — there's no "how many people should be in this
  role" number in the schema, so "vacant" here means "defined but
  currently unfilled," not "under quota." If you need actual budgeted
  headcount tracking (e.g., "this role should have 3 people, has 2, so 1 is
  vacant"), that needs a `JobPositions.headcount` column — say the word and
  I'll add it.

## Employees grid: guaranteed minimum size — new

`EmployeesGrid` now has `MinHeight="200"` (~5 rows). Previously, opening
one or more of the Employees screen's three expanders (Edit form,
Documents Hub, Employment History) could squeeze the grid down to a sliver
since it sits in a `*`-sized row competing with `Auto`-sized expanders
below it. The grid can no longer be squeezed below roughly 5 visible rows
no matter how many expanders are open. One trade-off worth knowing: the
Employees screen still isn't wrapped in a page-level `ScrollViewer`, so if
you open all three expanders on a small window, content below the grid's
guaranteed minimum could extend past the bottom of the window. Resizing the
window (or collapsing an expander you're not using) works around it for
now; say the word if you'd rather have the whole screen scroll as a unit.

## Bonuses, Penalties & Incentives — new

A new `PayrollAdjustments` table backs a dedicated screen: pick an
employee, a **Type** (Bonus / Incentive / Penalty), a month, an amount, and
an optional description. These feed straight into automatic payroll
generation — no manual re-entry in Payroll:

- **Bonus** and **Incentive** amounts are added to that month's `Allowances`.
- **Penalty** amounts are added to that month's `Deductions`.

`PayrollGenerationService` pulls the month's adjustments for each employee
automatically every time you click "Auto-Generate for All Employees" in
Payroll.

## Cash Advances — new

A new `CashAdvances` table + screen handles salary advances that get paid
back automatically over one or more months:

- Enter the employee, **Total Amount**, and **Installments** (number of
  months) — the **Monthly Deduction** auto-computes (`Amount / Installments`,
  rounded) as soon as you tab out of either field.
- Pick a **Start Month**; the grid's "Balance as of" selector lets you check
  **Paid So Far** / **Remaining** for any month, computed from elapsed
  months since the start (`CashAdvance.PaidAsOf`/`RemainingAsOf` in
  `Models.cs` — no separate deduction-ledger table, the balance is derived
  from `installments_elapsed × monthly_deduction`, capped at the total).
- `PayrollGenerationService` automatically deducts each active advance's
  installment (capped to whatever's left, so the final installment doesn't
  overshoot) from that month's `Deductions`, and flips the advance's status
  to **Completed** once the balance reaches zero — so it stops being
  deducted from then on, and you never have to remember to close it out
  manually.



You no longer have to pick a Shift by hand every time. `Data/ShiftDetector.cs`
figures it out from the check-in time alone, using each Shift's
`DetectionStartTime`/`DetectionEndTime` window (falling back to
`StartTime`/`EndTime` if detection times aren't set) — so a 08:55 check-in
matches the "Morning" shift's detection window and gets assigned
automatically, even though you never touched the Shift dropdown. It handles
windows that cross midnight (night shifts) correctly.

- **Manual entry**: type a Check-in time and tab out — the Shift dropdown
  fills in by itself (you can still override it manually afterward).
- **Excel import**: leave the `Shift` column blank and it's detected the
  same way per row; only fill it in yourself for punches you want to force
  onto a specific shift.

## Automatic Attendance calculation (Layer 1) — updated: now saves itself, no button needed

Previously the screen calculated `status1`/`MinutesLate`/`MinutesEarlyLeave`/
`Overtime`/`WorkingHours` into the form fields, but still needed an explicit
"Recalculate" and "Save" click to persist. That requirement is gone:

1. Pick an **Employee**, type **Check-in**/**Check-out**. The **Shift** is
   auto-detected from Check-in, or pick one yourself.
2. As soon as you leave the Check-in/Check-out box (or change the Shift,
   Employee, or Check-in Date), `Data/AttendanceCalculator.cs` checks — in
   priority order — whether the employee has an **approved Leave** covering
   that date (`Status = "Leave"` — covers **Official Mission** and
   **Departure Permit** too, since they're just `LeaveType` values in the
   same `Leaves` table), whether it's a **company Holiday**
   (`Status = "Holiday"`), whether it's the employee's **Weekend**
   (`Status = "Weekend"`, from `employee.weekend1`/`weekend2`), and only
   then compares the punch times against the Shift to get
   `Present`/`Late`/`Absent`.
3. **The result is saved immediately** (`AttendanceRepository.Upsert`) — no
   button click required. The "Force Recalculate" and "Save (manual
   override)" buttons still exist, but only as an optional explicit
   trigger; the normal flow never needs them.
4. **Approved records are never auto-touched.** If an Attendance row for
   that employee/date is already marked `Approved`, step 2's automatic
   recalculation is skipped entirely — the existing values load into the
   form for review, with a lock message explaining why nothing changed. You
   can still edit any field by hand and click **"Save (manual override)"**,
   which persists your change regardless of the Approved flag — that's a
   deliberate action, distinct from the automatic pathway. To let automatic
   recalculation resume for that record afterward, uncheck Approved and
   save — the DB state (not the on-screen checkbox alone) is what governs
   the lock, so an unsaved checkbox toggle doesn't unlock it by itself.
5. **One safeguard on top of "no button needed":** just browsing the
   Employee dropdown with a date already picked, with no check-in/check-out
   typed, does **not** create a new row for every employee you pass through.
   Auto-save only fires when there's something real to record — an actual
   punch, an existing record being edited, or the date being a
   Weekend/Holiday/Leave (worth recording even without a punch). Plain
   absences don't need a row at all — `attend_summary` already infers them
   from the *lack* of an Attendance row on a working day.

This applies uniformly to both entry paths — manual entry in the Attendance
screen, and the Excel bulk importer (which caches Holidays and approved
Leaves once per import for performance rather than querying per row).
Neither entry path accepts a Status column anymore.

## Leaves and Holidays now push updates back into Attendance automatically — new

Previously, approving a Leave request (or adding/editing/deleting a
Holiday) after an Attendance row for that date already existed left the row
showing stale data — you'd have to manually re-open and re-save it.
`Data/AttendanceRecalculationService.cs` closes that gap:

- **Approving/rejecting/deleting a Leave request** (in the Leave tab)
  recalculates every existing Attendance row for that employee across the
  request's date range.
- **Saving or deleting a Holiday** (in the Holidays tab) recalculates every
  employee's existing Attendance row on that date, since a holiday affects
  everyone at once.
- **Approved Attendance rows are always skipped** by this recalculation too
  — same lock as above. An HR manager's approval is never silently
  overridden by something changing elsewhere; they can still open that row
  and edit it manually if the situation genuinely calls for it.
- This only updates **rows that already exist** — it doesn't create new
  Attendance rows for days that don't have one yet (consistent with the
  "don't create rows just to represent an absence" principle above).


## Payroll now accounts for Holidays and Leave type — updated

`sp_GenerateMonthlyAttendanceSummary` was updated so the leave type matters:

- **Company Holidays** are never deducted — they're excluded from
  `days_count` entirely (not a working day to begin with), so they can't
  show up as `absent`.
- **Paid leave** — `Annual Leave`, `Sick Leave`, `Maternity/Paternity Leave`,
  **Official Mission**, **Departure Permit**, or any type other than
  `Unpaid Leave` — is excluded from `absent`, no deduction. The rule is a
  match against the literal string `'Unpaid Leave'`, so both new types
  automatically land in the paid bucket without any SP change; no code
  currently special-cases "Official Mission" or "Departure Permit" by name.
- **`Unpaid Leave`** is now deliberately *not* excluded from `absent` — an
  approved unpaid-leave day falls straight through into the `absent` bucket,
  so `PayrollGenerationService` deducts a day's pay for it automatically,
  exactly like an unexplained absence.

## Dashboard — new

The app now opens on a **Dashboard** (`Views/DashboardView`) instead of
Employees. It shows, computed live from existing repositories — no new
tables: Active Employees, Present/Late/Absent/On Leave Today, Pending Leave
Requests, Department count, and this month's net payroll (from `Payroll`,
if generated yet) as KPI cards; a headcount-by-department bar chart; the
next 5 upcoming Holidays; and up to 6 pending Leave requests. Click
"⟳ Refresh" to recompute after making changes elsewhere in the app.

## Printable Payslip — new

Payroll → select a row → **"🖨 Print Payslip"** opens `PayslipWindow`, a
formatted payslip (employee info, earnings, deductions, net pay, status)
built as a `FlowDocument`, with a **Print** button that opens the standard
Windows print dialog. No new package required — this uses WPF's built-in
`System.Windows.Controls.PrintDialog`.

## More room for the data grids — new

The edit forms in **Employees** and **Attendance** were long enough to push
the grid into a small sliver of the screen. Both are now wrapped in a
collapsible `Expander` (collapsed by default), so the grid gets the full
space until you actually need to add or edit a record — selecting a row, or
clicking "New", automatically expands the form back open.

## Employee Department filter — new

A **Department** dropdown sits next to the search box on the Employees
screen ("All Departments" plus every row from `department`). Combined with
the existing text search, you can narrow the grid to one department, one
search term, or both at once.

## Grid navigation: scroll in every direction, Code/Name always visible — new

Every `DataGrid` in the app now scrolls both **vertically and horizontally**
(explicit `ScrollViewer.HorizontalScrollBarVisibility`/
`VerticalScrollBarVisibility="Auto"` in the shared `App.xaml` style), and
the **Code/Name columns are frozen** (`FrozenColumnCount`) so they never
scroll out of view no matter how far right you scroll:

- Employees, Departments, Positions: `Code` + `Name`/`Title` pinned.
- Attendance, Leave, Payroll: didn't previously show an employee **code** at
  all (only the name) — each grid gained a `Code` column, and both `Code`
  and `Employee` are pinned.
- Shifts, WorkShifts: no separate code field in the schema, so just `Name`
  is pinned.

## Personal Documents Hub — updated: fixed categories, rename, edit, view inside the app

The **Personal Documents Hub** panel under the Employees edit form is a
secure, categorized document store per employee — National ID Card, Birth
Certificate, Personal Photo, Graduation Certificate, Criminal Record
(Fish), Military Status Certificate, Labor Office Card (Kaab Amal), Social
Insurance Printout (Ta'meenat), Medical Fitness Certificate, Contract, or
Other. The category list is enforced by a `CHECK` constraint on
`EmployeeDocuments.document_type` in the database, matched exactly against
`EmployeeDocument.Categories` in `Models.cs` — the dropdown can't offer a
value the database would reject.

- **Pick a Category, then "📎 Import Document..."** — the file is
  **copied** into an `EmployeeDocuments\{EmployeeCode}\` folder next to the
  built application (`Data/DocumentStorage.cs`, `AppContext.BaseDirectory`),
  and a row is added with its category, original file name, and an optional
  description. The original file the user picked is untouched and can be
  moved or deleted afterward without affecting the copy.
- **"👁 View / Rename / Edit"** (or double-click a row) opens
  `Views/DocumentViewerWindow` — a window **inside the app**, not an
  external program:
  - **Images** (.jpg/.png/.gif/.bmp) render directly in an `Image` control.
  - **Text-like files** (.txt/.csv/.log/.md/.xml/.json) render in a
    read-only text box.
  - **PDF** renders via WPF's built-in `WebBrowser` control, pointed at the
    local file — still inside the window, no external viewer launches.
    (This depends on the system's installed browser/PDF handler; if it
    doesn't render on a given machine, that's an OS-level limitation, not
    something the app can bypass without adding a PDF-rendering package.)
  - Anything else shows a "no inline preview available" message rather
    than silently failing.
  - **File Name**, **Category**, and **Description** are all editable —
    "💾 Save Changes" updates the `EmployeeDocuments` row. Renaming only
    changes the *display* name in the database; the underlying stored file
    on disk keeps its original name, so the stored path never breaks.
- **"Delete Selected"** removes both the DB row and the stored file.
- Name collisions on import are handled automatically (`file (1).pdf`,
  `file (2).pdf`, etc.) so importing two different files with the same name
  never overwrites one.

## Employment History — new

A second collapsed panel, **"Employment History"**, sits below the
Documents Hub. It's one flexible table (`EmploymentHistory`) rather than
five overlapping ones — a single `event_type` column covers Career
Progression, Employment Record, Job History Log, Internal Mobility Record,
and Performance History all at once:

- **Automatic entries** — `Department Transfer`, `Position Change`, and
  `Salary Change` are logged for you, with no extra step: whenever you edit
  an existing employee's Department, Position, or Basic Salary and click
  Save, `EmployeesView.LogAutomaticHistory` diffs the before/after record
  and writes a history row capturing the old → new values. Saving a brand
  new employee logs a `Hire` entry automatically; changing Employment
  Status to `Terminated` logs a `Termination` entry.
- **Manual entries** — `Performance Review` (with a Rating: Excellent /
  Good / Average / Needs Improvement) and anything else (`Promotion`,
  `Rehire`, `Other`, or a manual `Department Transfer`/`Position Change` you
  want to log yourself with more detail) are added directly in the panel.
- The grid's **Change** column shows a human-readable summary
  (`old → new`) computed from whichever old/new fields are populated for
  that row (`EmploymentHistoryEntry.Summary` in `Models.cs`).


## Positions filtered by Department — new

On the Departments & Positions screen, selecting a row in the **Departments**
grid now narrows the **Job Positions** grid to just that department's
positions (via `JobPositionRepository.GetByDepartment`), and pre-selects
that department in the "new position" form for convenience. Click "New" on
the department side (or don't select any department) to see every position
across all departments again.

## Main window nav buttons now actually left-align — bug fix

This was a real bug, not a style choice: the shared `Button` `ControlTemplate`
in `App.xaml` hardcoded `HorizontalAlignment="Center"` on its
`ContentPresenter`, which silently overrode the left-alignment that
`MainWindow.xaml`'s `NavButtonStyle` was already setting via
`HorizontalContentAlignment="Left"` — templates win over style setters
when they don't forward the property. Fixed by binding the
`ContentPresenter`'s alignment to `{TemplateBinding HorizontalContentAlignment}`
instead of hardcoding it, so per-style overrides (like the nav buttons)
take effect again. Scoped narrowly: only `NavButtonStyle` sets
`HorizontalContentAlignment`, so every other button in the app keeps its
existing centered look — this didn't change anything except the left nav.

## Leave Balance Summary — new

A collapsed **"Employee Leave Balance Summary (Current Year)"** panel at the
bottom of the Leave Management screen (lazy-loaded on first expand, with a
"⟳ Refresh" button) shows, for every active employee:

| Column | Meaning |
|---|---|
| Entitlement (Year) | `employee.annual_leave_days` — set per employee in the Employees screen (defaults to 21) |
| Carried Over | `employee.leave_carried_over` — an HR-maintained figure you set directly (e.g., at year start); there's no historical multi-year computation, since that would require attendance data going back further than most fresh installs will have |
| Total Available | Entitlement + Carried Over |
| Used This Year | Sum of days across all **approved**, `LeaveType = 'Annual Leave'` requests whose `StartDate` falls in the current year (`LeaveRequestRepository.GetApprovedAnnualLeaveDaysUsed`) |
| Remaining Balance | Total Available − Used This Year |

Only **Annual Leave** counts against this balance — Sick/Unpaid/Maternity
leave don't consume it, matching how Payroll already treats those types
differently. The summary refreshes automatically after you submit, approve,
or reject a leave request (once it's been expanded at least once in the
session).

## Users, Permissions & Language — new

The app now requires signing in, and has a **Settings** screen (visible only to
users whose role grants it) for managing accounts, role-based permissions, and
the app's display language.

- **Login.** On launch, `Views/LoginWindow` asks for a username and password
  before anything else opens. Credentials are checked against the new `Users`
  table; passwords are never stored in plain text — `Data/AuthHelper.cs`
  hashes them with PBKDF2-SHA256 (100,000 iterations, random per-user salt).
  A default account is seeded by the database scripts:
  **username `admin`, password `admin123`** — change it from Settings > Users
  after your first login. `Helpers/Session.cs` holds the signed-in user and
  their permissions for the rest of the app session; **Sign Out** at the
  bottom of the nav returns to the login screen without restarting the app.
- **Roles & Permissions.** Three roles are seeded — **Administrator** (full
  access, including Settings), **HR Manager** (full access to every
  operational module, no Settings), and **Viewer** (read-only everywhere
  except Settings) — but roles are fully editable: Settings > Roles &
  Permissions lets you add a role and, for each module (Dashboard, Employees,
  Attendance, Payroll, ...), tick **Can View** and/or **Can Add/Edit/Delete**
  independently. The left nav only shows the modules the signed-in user's
  role can view — this is enforced in `MainWindow.xaml.cs`, not just hidden
  by CSS-style styling; a module a role can't view never gets a nav button in
  the first place. (Per-field edit-locking *inside* an already-visible module,
  e.g. graying out Save on Payroll for a view-only role, isn't wired up yet —
  today `can_edit` is tracked and available via `Session.CanEdit(moduleKey)`
  for any screen that wants to start checking it, but no existing view calls
  it yet. Say the word if you want that wired into specific screens.)
- **Users.** Settings > Users is full CRUD against the `Users` table:
  username, full name, role, an optional link to an `employee` record (for
  staff who are both a system user and on the payroll), active/inactive, and
  password (set on creation, or via the separate **Reset Password** button
  afterward — editing a user's other fields never silently changes their
  password). A user can't delete their own account or the role they're
  currently signed in under, to avoid locking yourself out.
- **Language — English / Arabic.** Settings > Language offers **English**
  and **العربية (Arabic)**; **Apply** saves the choice to a new `AppSettings`
  key/value table and switches immediately, no restart needed. Per the
  request, **English keeps the left nav on the left; Arabic moves it to the
  right** — this isn't a separate Arabic layout, it's WPF's built-in
  `FlowDirection.RightToLeft` applied to the whole window (`MainWindow`
  listens for `Helpers/Localization.LanguageChanged` and flips it), which
  mirrors the nav/content Grid columns, text alignment, and scrollbars
  together the same way any RTL app does. `Helpers/Localization.cs` is a
  small English/Arabic string table (`Localization.T("key")`) currently wired
  into the **left nav, the login window, and the Settings screen itself** —
  the pieces that exist specifically for this feature. The other ten
  existing views (Employees, Payroll, Attendance, etc.) keep their original
  English-only labels; translating each of those is a separate, much larger
  pass with the same mechanism (add its strings to the two dictionaries in
  `Localization.cs`, swap its labels for `Localization.T(...)` calls) — say
  the word if you want specific screens translated next.

**Database:** `Database/Migration_v4_Users_Permissions_Language.sql` adds the
`Roles`, `Permissions`, `RolePermissions`, `Users`, and `AppSettings` tables
(and seeds the default roles/admin login/English setting) on an **existing**
database — safe to re-run, never touches anything else. A **brand new**
database doesn't need it separately: `HR_ERP_Database.sql` already includes
all five tables and their seed data.


- **A dedicated `attend_summary` screen** — the data is now generated and
  consumed automatically by the Payroll pipeline above, but there's no
  grid to browse it directly yet (only via `AttendSummaryRepository`).
  Let me know if you want a read-only "Monthly Attendance Report" screen.

## Worth knowing about / caveats

- **Weekend comparison is a plain string match** against the day-of-week
  name (`DateTime.DayOfWeek.ToString()`, e.g. `"Friday"`). It only works if
  `employee.weekend1`/`weekend2` are spelled exactly like that (case doesn't
  matter, but the word does) — same assumption the SQL side already made in
  `sp_GenerateMonthlyAttendanceSummary`'s `DATENAME(WEEKDAY, ...)` checks.
- **PDF preview depends on what's installed on the machine.** WPF's
  `WebBrowser` control hosts the system's IE/Edge engine; whether it
  actually renders a PDF inline depends on the Windows install. If it
  doesn't, that's a constraint of avoiding a new PDF-rendering package
  (e.g. PdfiumViewer) — say the word if you'd rather add one for guaranteed
  rendering.
- **"Departure Permit" is treated as a full paid day off**, same as Annual
  Leave — because `Leaves` only has date-level granularity (`StartDate`/
  `EndDate`, no time-of-day), there's no way to represent "left 2 hours
  early" as anything other than a whole day. If Departure Permit is meant
  to be a partial-day permission that only shaves a few hours off pay
  rather than excusing the whole day, that needs a schema change (e.g. a
  `hours` column) — flag it and I'll build that out.
- **Cash Advance balances assume payroll is generated in month order,
  every month, from the start month onward.** `PaidAsOf`/`RemainingAsOf`
  compute the balance from elapsed calendar months, not from an actual
  deduction ledger — so if you skip generating payroll for a month
  entirely, the balance calculation still assumes that month's installment
  was paid. This keeps the schema simple (no extra ledger table) at the
  cost of that assumption; say the word if you'd rather have an explicit
  per-month deduction record instead.
- **The "Approved" lock is governed by what's saved in the database, not
  by the on-screen checkbox.** If you uncheck "Approved" in the form but
  haven't clicked Save yet, automatic recalculation still treats the
  record as locked (because the DB still says it's Approved) — you have to
  actually save the unchecked state first before automatic updates resume
  for that record. This is deliberate: an unsaved checkbox toggle shouldn't
  be enough to bypass a lock.
- **The main/sub-department cycle guard is one level deep.** Saving a
  department checks only whether the chosen parent's own parent points
  back at the department being saved — it won't catch a longer chain (A →
  B → C → A). This matches the two-level hierarchy the Org Structure
  screen actually renders (main department → sub-department, not arbitrary
  depth), so a longer cycle shouldn't be constructible through the UI in
  the first place, but there's no database-level `CHECK` enforcing it.
