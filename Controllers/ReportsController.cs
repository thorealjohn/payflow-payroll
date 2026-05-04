using itpayroll.Data;
using itpayroll.Models;
using itpayroll.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace itpayroll.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> PayrollReport(string period = "ThisMonth", string customDateFrom = null, string customDateTo = null)
        {
            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var query = _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e.User)
                .AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(p => p.PeriodStart >= from.Value);
            }
            if (to.HasValue)
            {
                query = query.Where(p => p.PeriodEnd <= to.Value);
            }

            var payrolls = await query.OrderByDescending(p => p.PeriodStart).ToListAsync();

            ViewBag.Period = period;
            ViewBag.CustomDateFrom = customDateFrom;
            ViewBag.CustomDateTo = customDateTo;
            ViewBag.Employees = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.Status == EmploymentStatus.Active)
                .Select(e => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = e.EmployeeId.ToString(),
                    Text = $"{e.EmployeeNumber} - {e.User.FirstName} {e.User.LastName}"
                }).ToListAsync();

            return View(payrolls);
        }

        public async Task<IActionResult> ExportPayrollExcel(string period = "ThisMonth", string customDateFrom = null, string customDateTo = null)
        {
            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var query = _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e.User)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(p => p.PeriodStart >= from.Value);
            if (to.HasValue)
                query = query.Where(p => p.PeriodEnd <= to.Value);

            var payrolls = await query.OrderByDescending(p => p.PeriodStart).ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Payroll Report");

            // Header
            ws.Cells[1, 1].Value = "Employee Number";
            ws.Cells[1, 2].Value = "Employee Name";
            ws.Cells[1, 3].Value = "Period Start";
            ws.Cells[1, 4].Value = "Period End";
            ws.Cells[1, 5].Value = "Gross Pay";
            ws.Cells[1, 6].Value = "Deductions";
            ws.Cells[1, 7].Value = "Net Pay";
            ws.Cells[1, 8].Value = "Status";

            using var headerRange = ws.Cells[1, 1, 1, 8];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            // Data
            int row = 2;
            foreach (var p in payrolls)
            {
                ws.Cells[row, 1].Value = p.Employee?.EmployeeNumber;
                ws.Cells[row, 2].Value = $"{p.Employee?.User?.FirstName} {p.Employee?.User?.LastName}";
                ws.Cells[row, 3].Value = p.PeriodStart.ToString("yyyy-MM-dd");
                ws.Cells[row, 4].Value = p.PeriodEnd.ToString("yyyy-MM-dd");
                ws.Cells[row, 5].Value = (double)p.GrossPay;
                ws.Cells[row, 6].Value = (double)p.TotalDeductions;
                ws.Cells[row, 7].Value = (double)p.NetPay;
                ws.Cells[row, 8].Value = p.Status.ToString();
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var fileName = $"PayrollReport_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public async Task<IActionResult> AttendanceReport(DateTime? dateFrom, DateTime? dateTo, int? employeeId)
        {
            var query = _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.User)
                .AsQueryable();

            if (dateFrom.HasValue)
                query = query.Where(a => a.Date >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(a => a.Date <= dateTo.Value);
            if (employeeId.HasValue)
                query = query.Where(a => a.EmployeeId == employeeId.Value);

            var attendances = await query.OrderByDescending(a => a.Date).ToListAsync();

            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.Employees = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.Status == EmploymentStatus.Active)
                .Select(e => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = e.EmployeeId.ToString(),
                    Text = $"{e.EmployeeNumber} - {e.User.FirstName} {e.User.LastName}"
                }).ToListAsync();

            return View(attendances);
        }

        public async Task<IActionResult> ExportAttendanceExcel(DateTime? dateFrom, DateTime? dateTo, int? employeeId)
        {
            var query = _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.User)
                .AsQueryable();

            if (dateFrom.HasValue)
                query = query.Where(a => a.Date >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(a => a.Date <= dateTo.Value);
            if (employeeId.HasValue)
                query = query.Where(a => a.EmployeeId == employeeId.Value);

            var attendances = await query.OrderByDescending(a => a.Date).ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Attendance Report");

            // Header
            ws.Cells[1, 1].Value = "Employee Number";
            ws.Cells[1, 2].Value = "Employee Name";
            ws.Cells[1, 3].Value = "Date";
            ws.Cells[1, 4].Value = "Time In";
            ws.Cells[1, 5].Value = "Time Out";
            ws.Cells[1, 6].Value = "Total Hours";
            ws.Cells[1, 7].Value = "Late (min)";
            ws.Cells[1, 8].Value = "Undertime (min)";
            ws.Cells[1, 9].Value = "Night Shift (hrs)";
            ws.Cells[1, 10].Value = "Day Type";

            using var headerRange = ws.Cells[1, 1, 1, 10];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            int row = 2;
            foreach (var a in attendances)
            {
                ws.Cells[row, 1].Value = a.Employee?.EmployeeNumber;
                ws.Cells[row, 2].Value = $"{a.Employee?.User?.FirstName} {a.Employee?.User?.LastName}";
                ws.Cells[row, 3].Value = a.Date.ToString("yyyy-MM-dd");
                ws.Cells[row, 4].Value = a.TimeIn.ToString(@"hh\:mm");
                ws.Cells[row, 5].Value = a.TimeOut.ToString(@"hh\:mm");
                ws.Cells[row, 6].Value = Math.Round(a.TotalHours, 2);
                ws.Cells[row, 7].Value = a.LateMinutes;
                ws.Cells[row, 8].Value = a.UndertimeMinutes;
                ws.Cells[row, 9].Value = Math.Round(a.NightShiftHours, 2);
                ws.Cells[row, 10].Value = a.DayType.ToString();
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var fileName = $"AttendanceReport_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public async Task<IActionResult> EmployeeReport()
        {
            var employees = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Shift)
                .Where(e => e.Status == EmploymentStatus.Active)
                .OrderBy(e => e.EmployeeNumber)
                .ToListAsync();

            return View(employees);
        }

        public async Task<IActionResult> ExportEmployeeExcel()
        {
            var employees = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Shift)
                .Where(e => e.Status == EmploymentStatus.Active)
                .OrderBy(e => e.EmployeeNumber)
                .ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Employee Report");

            // Header
            ws.Cells[1, 1].Value = "Employee Number";
            ws.Cells[1, 2].Value = "Name";
            ws.Cells[1, 3].Value = "Email";
            ws.Cells[1, 4].Value = "Basic Salary";
            ws.Cells[1, 5].Value = "Shift";
            ws.Cells[1, 6].Value = "Hire Date";
            ws.Cells[1, 7].Value = "Status";

            using var headerRange = ws.Cells[1, 1, 1, 7];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            int row = 2;
            foreach (var e in employees)
            {
                ws.Cells[row, 1].Value = e.EmployeeNumber;
                ws.Cells[row, 2].Value = $"{e.User?.FirstName} {e.User?.LastName}";
                ws.Cells[row, 3].Value = e.User?.Email;
                ws.Cells[row, 4].Value = (double)e.BasicSalary;
                ws.Cells[row, 5].Value = e.Shift?.ShiftName ?? "N/A";
                ws.Cells[row, 6].Value = e.HireDate.ToString("yyyy-MM-dd");
                ws.Cells[row, 7].Value = e.Status.ToString();
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var fileName = $"EmployeeReport_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
