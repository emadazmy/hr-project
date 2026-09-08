using System.Data;
using HR_ERP.Models;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    public static class ShiftRepository
    {
        public static List<Shift> GetAll()
        {
            var list = new List<Shift>();
            var table = DatabaseHelper.ExecuteQuery(@"
                SELECT ShiftID, ShiftName, StartTime, EndTime, LateToleranceMinutes, EarlyLeaveToleranceMinutes,
                       OvertimeGraceMinutes, workingHours, DetectionStartTime, DetectionEndTime
                FROM Shifts ORDER BY ShiftName");

            foreach (DataRow row in table.Rows)
            {
                list.Add(new Shift
                {
                    ShiftID = (int)row["ShiftID"],
                    ShiftName = row["ShiftName"].ToString() ?? "",
                    StartTime = (TimeSpan)row["StartTime"],
                    EndTime = (TimeSpan)row["EndTime"],
                    LateToleranceMinutes = (int)row["LateToleranceMinutes"],
                    EarlyLeaveToleranceMinutes = (int)row["EarlyLeaveToleranceMinutes"],
                    OvertimeGraceMinutes = (int)row["OvertimeGraceMinutes"],
                    WorkingHours = row["workingHours"] as int?,
                    DetectionStartTime = row["DetectionStartTime"] as TimeSpan?,
                    DetectionEndTime = row["DetectionEndTime"] as TimeSpan?
                });
            }
            return list;
        }

        public static void Add(Shift s)
        {
            DatabaseHelper.ExecuteNonQuery(@"
                INSERT INTO Shifts (ShiftName, StartTime, EndTime, LateToleranceMinutes, EarlyLeaveToleranceMinutes, OvertimeGraceMinutes, workingHours, DetectionStartTime, DetectionEndTime)
                VALUES (@name, @start, @end, @late, @early, @otGrace, @hours, @detStart, @detEnd)",
                new SqlParameter("@name", s.ShiftName),
                new SqlParameter("@start", s.StartTime),
                new SqlParameter("@end", s.EndTime),
                new SqlParameter("@late", s.LateToleranceMinutes),
                new SqlParameter("@early", s.EarlyLeaveToleranceMinutes),
                new SqlParameter("@otGrace", s.OvertimeGraceMinutes),
                new SqlParameter("@hours", (object?)s.WorkingHours ?? DBNull.Value),
                new SqlParameter("@detStart", (object?)s.DetectionStartTime ?? DBNull.Value),
                new SqlParameter("@detEnd", (object?)s.DetectionEndTime ?? DBNull.Value));
        }

        public static void Update(Shift s)
        {
            DatabaseHelper.ExecuteNonQuery(@"
                UPDATE Shifts SET ShiftName = @name, StartTime = @start, EndTime = @end,
                    LateToleranceMinutes = @late, EarlyLeaveToleranceMinutes = @early,
                    OvertimeGraceMinutes = @otGrace, workingHours = @hours,
                    DetectionStartTime = @detStart, DetectionEndTime = @detEnd
                WHERE ShiftID = @id",
                new SqlParameter("@name", s.ShiftName),
                new SqlParameter("@start", s.StartTime),
                new SqlParameter("@end", s.EndTime),
                new SqlParameter("@late", s.LateToleranceMinutes),
                new SqlParameter("@early", s.EarlyLeaveToleranceMinutes),
                new SqlParameter("@otGrace", s.OvertimeGraceMinutes),
                new SqlParameter("@hours", (object?)s.WorkingHours ?? DBNull.Value),
                new SqlParameter("@detStart", (object?)s.DetectionStartTime ?? DBNull.Value),
                new SqlParameter("@detEnd", (object?)s.DetectionEndTime ?? DBNull.Value),
                new SqlParameter("@id", s.ShiftID));
        }

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM Shifts WHERE ShiftID = @id", new SqlParameter("@id", id));
        }
    }

    public static class WorkShiftRepository
    {
        public static List<WorkShift> GetAll()
        {
            var list = new List<WorkShift>();
            var table = DatabaseHelper.ExecuteQuery("SELECT id, name, start_time, end_time, break_minutes, total_hours FROM WorkShifts ORDER BY name");
            foreach (DataRow row in table.Rows)
            {
                list.Add(new WorkShift
                {
                    Id = (int)row["id"],
                    Name = row["name"].ToString() ?? "",
                    StartTime = (TimeSpan)row["start_time"],
                    EndTime = (TimeSpan)row["end_time"],
                    BreakMinutes = row["break_minutes"] as int? ?? 0,
                    TotalHours = row["total_hours"] as decimal?
                });
            }
            return list;
        }

        public static void Add(WorkShift w)
        {
            DatabaseHelper.ExecuteNonQuery(
                "INSERT INTO WorkShifts (name, start_time, end_time, break_minutes, total_hours) VALUES (@name, @start, @end, @break, @total)",
                new SqlParameter("@name", w.Name),
                new SqlParameter("@start", w.StartTime),
                new SqlParameter("@end", w.EndTime),
                new SqlParameter("@break", w.BreakMinutes),
                new SqlParameter("@total", (object?)w.TotalHours ?? DBNull.Value));
        }

        public static void Update(WorkShift w)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE WorkShifts SET name = @name, start_time = @start, end_time = @end, break_minutes = @break, total_hours = @total WHERE id = @id",
                new SqlParameter("@name", w.Name),
                new SqlParameter("@start", w.StartTime),
                new SqlParameter("@end", w.EndTime),
                new SqlParameter("@break", w.BreakMinutes),
                new SqlParameter("@total", (object?)w.TotalHours ?? DBNull.Value),
                new SqlParameter("@id", w.Id));
        }

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM WorkShifts WHERE id = @id", new SqlParameter("@id", id));
        }
    }
}
