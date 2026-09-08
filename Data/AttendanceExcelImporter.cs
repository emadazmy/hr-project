using System.Globalization;
using ClosedXML.Excel;
using HR_ERP.Models;

namespace HR_ERP.Data
{
    public class AttendanceImportRowError
    {
        public int RowNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AttendanceImportResult
    {
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public List<AttendanceImportRowError> Errors { get; set; } = new();
        public int TotalRows => Inserted + Updated + Errors.Count;
    }

    /// <summary>
    /// Imports attendance from an .xlsx sheet with columns (header row required):
    /// EmployeeCode | CheckinDate | Checkin | CheckoutDate | Checkout | Shift
    /// Checkin/Checkout accept either Excel time values or "HH:mm" text, and may be left blank.
    /// Shift is matched by name against the Shifts table; if left blank, it's auto-detected from
    /// the Checkin time via ShiftDetector (each Shift's DetectionStartTime/DetectionEndTime
    /// window). There is no Status column — status1/MinutesLate/MinutesEarlyLeave/WorkingHours/
    /// Overtime are all computed automatically by AttendanceCalculator from Checkin/Checkout vs.
    /// the Shift, and from whether the date is the employee's Weekend, a company Holiday, or
    /// covered by an approved Leave — exactly like the manual Attendance form. Existing records
    /// for the same employee/checkin-date are updated rather than duplicated.
    /// </summary>
    public static class AttendanceExcelImporter
    {
        public static AttendanceImportResult Import(string filePath)
        {
            var result = new AttendanceImportResult();

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            // Cache employees and shifts to avoid a DB round-trip per row.
            var employeesByCode = EmployeeRepository.GetAll()
                .GroupBy(e => e.Code, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var shiftsByName = ShiftRepository.GetAll()
                .GroupBy(s => s.ShiftName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // Cache Holidays and approved Leaves once so every row's status factors them in
            // without a DB round-trip per row.
            var holidayDates = HolidayRepository.GetAll().Select(h => h.HolidayDate.Date).ToHashSet();
            var approvedLeaves = LeaveRequestRepository.GetAll()
                .Where(l => string.Equals(l.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                .ToList();

            for (int rowNum = 2; rowNum <= lastRow; rowNum++) // row 1 = header
            {
                var row = worksheet.Row(rowNum);
                if (row.IsEmpty()) continue;

                string code = row.Cell(1).GetString().Trim();
                if (string.IsNullOrWhiteSpace(code)) continue; // skip blank rows

                try
                {
                    if (!employeesByCode.TryGetValue(code, out var employee))
                    {
                        result.Errors.Add(new AttendanceImportRowError { RowNumber = rowNum, Message = $"Employee code '{code}' not found." });
                        continue;
                    }

                    if (!TryReadDate(row.Cell(2), out var checkinDate))
                    {
                        result.Errors.Add(new AttendanceImportRowError { RowNumber = rowNum, Message = "Check-in date is missing or invalid." });
                        continue;
                    }

                    var checkin = TryReadTime(row.Cell(3));

                    DateTime? checkoutDate = null;
                    if (TryReadDate(row.Cell(4), out var co)) checkoutDate = co;
                    var checkout = TryReadTime(row.Cell(5));

                    Shift? shift = null;
                    string shiftName = row.Cell(6).GetString().Trim();
                    if (!string.IsNullOrWhiteSpace(shiftName))
                    {
                        if (!shiftsByName.TryGetValue(shiftName, out shift))
                        {
                            result.Errors.Add(new AttendanceImportRowError { RowNumber = rowNum, Message = $"Shift '{shiftName}' not found." });
                            continue;
                        }
                    }
                    else
                    {
                        // No shift given -> detect it from the check-in time automatically.
                        shift = ShiftDetector.Detect(checkin, shiftsByName.Values);
                    }

                    // Same calculation as the manual Attendance form — no Status column to read.
                    // Factors in Weekend, Holidays, and approved Leaves for this employee/date, not just the punch times.
                    bool isHoliday = holidayDates.Contains(checkinDate.Date);
                    bool isOnLeave = approvedLeaves.Any(l =>
                        string.Equals(l.EmpCode, employee.Code, StringComparison.OrdinalIgnoreCase)
                        && checkinDate.Date >= l.StartDate.Date && checkinDate.Date <= l.EndDate.Date);
                    bool isWeekend = AttendanceCalculator.IsWeekend(employee, checkinDate);

                    var calc = AttendanceCalculator.Calculate(checkin, checkout, shift, isHoliday, isOnLeave, isWeekend);

                    bool existed = AttendanceRepository.GetByEmployeeAndDate(employee.Code, checkinDate) != null;

                    AttendanceRepository.Upsert(new AttendanceRecord
                    {
                        EmployeeCode = employee.Code,
                        Depart = employee.Depart,
                        Name = employee.Name,
                        CheckinDate = checkinDate,
                        Checkin = checkin,
                        CheckoutDate = checkoutDate,
                        Checkout = checkout,
                        ShiftId = shift?.ShiftID,
                        Status1 = calc.Status,
                        MinutesLate = calc.MinutesLate,
                        MinutesEarlyLeave = calc.MinutesEarlyLeave,
                        WorkingHours = calc.WorkingHours,
                        Overtime = calc.Overtime,
                        DayName = checkinDate.ToString("dddd", CultureInfo.InvariantCulture)
                    });

                    if (existed) result.Updated++;
                    else result.Inserted++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new AttendanceImportRowError { RowNumber = rowNum, Message = ex.Message });
                }
            }

            return result;
        }

        private static bool TryReadDate(IXLCell cell, out DateTime date)
        {
            date = default;
            if (cell.IsEmpty()) return false;

            if (cell.DataType == XLDataType.DateTime)
            {
                date = cell.GetDateTime().Date;
                return true;
            }

            var text = cell.GetString().Trim();
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                   || DateTime.TryParse(text, out date);
        }

        private static TimeSpan? TryReadTime(IXLCell cell)
        {
            if (cell.IsEmpty()) return null;

            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime().TimeOfDay;

            var text = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(text)) return null;

            if (TimeSpan.TryParseExact(text, @"hh\:mm", CultureInfo.InvariantCulture, out var t))
                return t;

            return TimeSpan.TryParse(text, out t) ? t : null;
        }

        /// <summary>Writes a blank template workbook with the expected headers and one example row.</summary>
        public static void WriteTemplate(string filePath)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Attendance");

            ws.Cell(1, 1).Value = "EmployeeCode";
            ws.Cell(1, 2).Value = "CheckinDate";
            ws.Cell(1, 3).Value = "Checkin";
            ws.Cell(1, 4).Value = "CheckoutDate";
            ws.Cell(1, 5).Value = "Checkout";
            ws.Cell(1, 6).Value = "Shift";
            ws.Row(1).Style.Font.Bold = true;

            ws.Cell(2, 1).Value = "EMP001";
            ws.Cell(2, 2).Value = DateTime.Today;
            ws.Cell(2, 2).Style.DateFormat.Format = "yyyy-mm-dd";
            ws.Cell(2, 3).Value = "09:00";
            ws.Cell(2, 4).Value = DateTime.Today;
            ws.Cell(2, 4).Style.DateFormat.Format = "yyyy-mm-dd";
            ws.Cell(2, 5).Value = "17:00";
            ws.Cell(2, 6).Value = "Morning";

            ws.Cell(3, 1).Value = "EMP002";
            ws.Cell(3, 2).Value = DateTime.Today;
            ws.Cell(3, 2).Style.DateFormat.Format = "yyyy-mm-dd";
            ws.Cell(3, 3).Value = "09:05";
            ws.Cell(3, 4).Value = DateTime.Today;
            ws.Cell(3, 4).Style.DateFormat.Format = "yyyy-mm-dd";
            ws.Cell(3, 5).Value = "17:00";
            // Shift left blank on purpose: it will be auto-detected from the Checkin time.

            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }
    }
}
