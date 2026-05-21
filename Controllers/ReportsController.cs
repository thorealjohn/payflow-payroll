using itpayroll.Data;
using itpayroll.Models;
using itpayroll.Services;
using itpayroll.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace itpayroll.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfService _pdfService;

        public ReportsController(ApplicationDbContext context, IPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> PayrollReport(
            string period = "ThisMonth",
            string customDateFrom = "",
            string customDateTo = "",
            int? employeeId = null,
            string? department = null,
            string? payrollStatus = null,
            int page = 1,
            int pageSize = 10)
        {
            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var query = _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e.User)
                .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
                .AsQueryable();

            query = ApplyPayrollFilters(query, from, to, employeeId, department, payrollStatus);

            var totalGrossPay = await query.SumAsync(p => p.GrossPay);
            var totalDeductions = await query.SumAsync(p => p.TotalDeductions);
            var totalNetPay = await query.SumAsync(p => p.NetPay);
            var employeesPaid = await query.Select(p => p.EmployeeId).Distinct().CountAsync();

            var monthlyTrendRaw = await query
                .GroupBy(p => new { p.PeriodEnd.Year, p.PeriodEnd.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Total = g.Sum(x => x.NetPay)
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Take(6)
                .ToListAsync();
            var monthlyTrend = monthlyTrendRaw
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .Select(x => new
                {
                    Month = $"{x.Year}-{x.Month:D2}",
                    x.Total
                })
                .ToList();

            var totalRows = await query.CountAsync();
            page = Math.Max(1, page);
            var totalPages = (int)Math.Ceiling(totalRows / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var payrolls = await query
                .OrderByDescending(p => p.PeriodEnd)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var payrollIds = payrolls.Select(p => p.PayrollId).ToList();
            var earningsByPayroll = await _context.Earnings
                .Where(e => payrollIds.Contains(e.PayrollId))
                .OrderBy(e => e.Type)
                .GroupBy(e => e.PayrollId)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());
            var deductionsByPayroll = await _context.Deductions
                .Where(d => payrollIds.Contains(d.PayrollId))
                .OrderBy(d => d.Type)
                .GroupBy(d => d.PayrollId)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            ViewBag.Period = period;
            ViewBag.CustomDateFrom = customDateFrom;
            ViewBag.CustomDateTo = customDateTo;
            ViewBag.SelectedEmployee = employeeId;
            ViewBag.SelectedDepartment = department;
            ViewBag.SelectedPayrollStatus = payrollStatus;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRows = totalRows;
            ViewBag.GeneratedBy = User?.Identity?.Name ?? "System";
            ViewBag.GeneratedOn = DateTime.Now;
            ViewBag.DateRangeText = BuildDateRangeText(from, to);
            ViewBag.TotalGrossPay = totalGrossPay;
            ViewBag.TotalDeductions = totalDeductions;
            ViewBag.TotalNetPay = totalNetPay;
            ViewBag.EmployeesPaid = employeesPaid;
            ViewBag.MonthlyTrend = monthlyTrend;
            ViewBag.EarningsByPayroll = earningsByPayroll;
            ViewBag.DeductionsByPayroll = deductionsByPayroll;
            ViewBag.PayrollStatuses = Enum.GetNames(typeof(PayrollStatus)).ToList();
            await PopulateEmployeeDropdown();
            await PopulateDepartmentDropdown();

            return View(payrolls);
        }

        public async Task<IActionResult> ExportPayrollExcel(
            string period = "ThisMonth",
            string customDateFrom = "",
            string customDateTo = "",
            int? employeeId = null,
            string? department = null,
            string? payrollStatus = null)
        {
            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var query = _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e.User)
                .AsQueryable();

            query = ApplyPayrollFilters(query, from, to, employeeId, department, payrollStatus);

            var payrolls = await query.OrderByDescending(p => p.PeriodStart).ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Payroll Report");

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

            int row = 2;
            foreach (var p in payrolls)
            {
                ws.Cells[row, 1].Value = p.Employee?.EmployeeNumber;
                ws.Cells[row, 2].Value = $"{p.Employee?.User?.FirstName} {p.Employee?.User?.LastName}";
                ws.Cells[row, 3].Value = p.PeriodStart.ToString("yyyy-MM-dd");
                ws.Cells[row, 4].Value = p.PeriodEnd.ToString("yyyy-MM-dd");
                ws.Cells[row, 5].Value = (double)p.GrossPay;
                ws.Cells[row, 5].Style.Numberformat.Format = "₱#,##0.00";
                ws.Cells[row, 6].Value = (double)p.TotalDeductions;
                ws.Cells[row, 6].Style.Numberformat.Format = "₱#,##0.00";
                ws.Cells[row, 7].Value = (double)p.NetPay;
                ws.Cells[row, 7].Style.Numberformat.Format = "₱#,##0.00";
                ws.Cells[row, 8].Value = p.Status.ToString();
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var fileName = $"PayrollReport_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public async Task<IActionResult> ExportPayrollPdf(
            string period = "ThisMonth",
            string customDateFrom = "",
            string customDateTo = "",
            int? employeeId = null,
            string? department = null,
            string? payrollStatus = null)
        {
            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var query = _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e.User)
                .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
                .AsQueryable();

            query = ApplyPayrollFilters(query, from, to, employeeId, department, payrollStatus);

            var payrolls = await query.OrderByDescending(p => p.PeriodStart).ToListAsync();
            var totalGross = payrolls.Sum(p => p.GrossPay);
            var totalDeductions = payrolls.Sum(p => p.TotalDeductions);
            var totalNet = payrolls.Sum(p => p.NetPay);
            var dateLabel = BuildDateRangeText(from, to);

            var pdf = _pdfService.Generate(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(c =>
                    {
                        c.Item().Text("Payroll Report").Bold().FontSize(18).FontColor(Colors.Blue.Darken3);
                        c.Item().Text($"Generated by: {User?.Identity?.Name ?? "System"} | {dateLabel}").FontSize(10).FontColor(Colors.Grey.Darken1);
                        c.Item().PaddingBottom(5).LineHorizontal(1);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Employee").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Period").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Gross Pay").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Deductions").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Net Pay").Bold().FontSize(9);
                        });

                        foreach (var p in payrolls)
                        {
                            var name = $"{p.Employee?.EmployeeNumber} - {p.Employee?.User?.FirstName} {p.Employee?.User?.LastName}";
                            var periodStr = $"{p.PeriodStart:MMM dd, yyyy} - {p.PeriodEnd:MMM dd, yyyy}";
                            table.Cell().Padding(2).Text(name).FontSize(8);
                            table.Cell().Padding(2).Text(periodStr).FontSize(8);
                            table.Cell().Padding(2).AlignRight().Text($"₱{p.GrossPay:N2}").FontSize(8);
                            table.Cell().Padding(2).AlignRight().Text($"₱{p.TotalDeductions:N2}").FontSize(8);
                            table.Cell().Padding(2).AlignRight().Text($"₱{p.NetPay:N2}").FontSize(8);
                        }

                        table.Cell().Padding(2).Text("TOTALS").Bold().FontSize(9);
                        table.Cell().Padding(2).Text("").FontSize(8);
                        table.Cell().Padding(2).AlignRight().Text($"₱{totalGross:N2}").Bold().FontSize(9);
                        table.Cell().Padding(2).AlignRight().Text($"₱{totalDeductions:N2}").Bold().FontSize(9);
                        table.Cell().Padding(2).AlignRight().Text($"₱{totalNet:N2}").Bold().FontSize(9);
                    });

                    page.Footer().Row(r =>
                    {
                        r.RelativeItem().AlignLeft().Text(x =>
                        {
                            x.Span($"Generated on {DateTime.Now:MMM dd, yyyy hh:mm tt}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        r.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ").FontSize(8);
                            x.CurrentPageNumber().FontSize(8);
                            x.Span(" of ").FontSize(8);
                            x.TotalPages().FontSize(8);
                        });
                    });
                });
            });

            return File(pdf, "application/pdf", $"PayrollReport_{DateTime.Now:yyyyMMdd}.pdf");
        }

        public async Task<IActionResult> AttendanceReport(
            DateTime? dateFrom,
            DateTime? dateTo,
            int? employeeId,
            int? shiftId = null,
            string? department = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Attendances
                .Include(a => a.Employee).ThenInclude(e => e.User)
                .Include(a => a.Employee).ThenInclude(e => e.Department)
                .Include(a => a.Shift)
                .AsQueryable();

            query = ApplyAttendanceFilters(query, dateFrom, dateTo, employeeId, shiftId, department);

            var presentEmployeeIds = await query.Select(a => a.EmployeeId).Distinct().ToListAsync();
            var presentEmployees = presentEmployeeIds.Count;

            var activeQuery = _context.Employees.Where(e => e.Status == EmploymentStatus.Active);
            if (!string.IsNullOrWhiteSpace(department) && int.TryParse(department, out var deptId))
                activeQuery = activeQuery.Where(e => e.DepartmentId == deptId);
            if (employeeId.HasValue)
                activeQuery = activeQuery.Where(e => e.EmployeeId == employeeId.Value);

            var activeEmployees = await activeQuery.CountAsync();
            var absentEmployees = Math.Max(0, activeEmployees - presentEmployees);

            var lateEmployees = await query.Where(a => a.LateMinutes > 0).Select(a => a.EmployeeId).Distinct().CountAsync();
            var averageHours = await query.Select(a => (double?)a.TotalHours).AverageAsync() ?? 0;
            var openLogs = await query.CountAsync(a => a.TimeOut == default || a.TimeOut <= a.TimeIn);
            var attendanceRate = activeEmployees == 0 ? 0 : (presentEmployees * 100.0 / activeEmployees);

            var mostLateEmployee = await query
                .GroupBy(a => new { a.EmployeeId, Name = a.Employee.User == null ? "" : (a.Employee.User.FirstName + " " + a.Employee.User.LastName) })
                .Select(g => new { g.Key.Name, Late = g.Sum(x => x.LateMinutes) })
                .OrderByDescending(x => x.Late)
                .FirstOrDefaultAsync();
            var mostOvertimeEmployee = await query
                .GroupBy(a => new { a.EmployeeId, Name = a.Employee.User == null ? "" : (a.Employee.User.FirstName + " " + a.Employee.User.LastName) })
                .Select(g => new { g.Key.Name, OT = g.Sum(x => x.OvertimeHours) })
                .OrderByDescending(x => x.OT)
                .FirstOrDefaultAsync();
            var highestAttendanceRateEmployee = await query
                .GroupBy(a => new { a.EmployeeId, Name = a.Employee.User == null ? "" : (a.Employee.User.FirstName + " " + a.Employee.User.LastName) })
                .Select(g => new { g.Key.Name, Days = g.Count() })
                .OrderByDescending(x => x.Days)
                .FirstOrDefaultAsync();

            var totalRows = await query.CountAsync();
            page = Math.Max(1, page);
            var totalPages = (int)Math.Ceiling(totalRows / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var attendances = await query
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.EmployeeId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.SelectedEmployee = employeeId;
            ViewBag.SelectedShift = shiftId;
            ViewBag.SelectedDepartment = department;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRows = totalRows;
            ViewBag.GeneratedBy = User?.Identity?.Name ?? "System";
            ViewBag.GeneratedOn = DateTime.Now;
            ViewBag.DateRangeText = BuildDateRangeText(dateFrom, dateTo);
            ViewBag.PresentEmployees = presentEmployees;
            ViewBag.LateEmployees = lateEmployees;
            ViewBag.AbsentEmployees = absentEmployees;
            ViewBag.AverageHoursWorked = averageHours;
            ViewBag.OpenLogs = openLogs;
            ViewBag.AttendanceRate = attendanceRate;
            ViewBag.MostLateEmployee = mostLateEmployee?.Name ?? "N/A";
            ViewBag.MostLateMinutes = mostLateEmployee?.Late ?? 0;
            ViewBag.MostOvertimeEmployee = mostOvertimeEmployee?.Name ?? "N/A";
            ViewBag.MostOvertimeHours = mostOvertimeEmployee?.OT ?? 0;
            ViewBag.HighestAttendanceEmployee = highestAttendanceRateEmployee?.Name ?? "N/A";
            await PopulateEmployeeDropdown();
            await PopulateShiftDropdown();
            await PopulateDepartmentDropdown();

            return View(attendances);
        }

        public async Task<IActionResult> ExportAttendanceExcel(
            DateTime? dateFrom,
            DateTime? dateTo,
            int? employeeId,
            int? shiftId = null,
            string? department = null)
        {
            var query = _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.User)
                .AsQueryable();

            query = ApplyAttendanceFilters(query, dateFrom, dateTo, employeeId, shiftId, department);

            var attendances = await query.OrderByDescending(a => a.Date).ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Attendance Report");

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

        public async Task<IActionResult> ExportAttendancePdf(
            DateTime? dateFrom,
            DateTime? dateTo,
            int? employeeId,
            int? shiftId = null,
            string? department = null)
        {
            var query = _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.User)
                .AsQueryable();

            query = ApplyAttendanceFilters(query, dateFrom, dateTo, employeeId, shiftId, department);

            var attendances = await query.OrderByDescending(a => a.Date).ToListAsync();
            var dateLabel = BuildDateRangeText(dateFrom, dateTo);

            var pdf = _pdfService.Generate(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(c =>
                    {
                        c.Item().Text("Attendance Report").Bold().FontSize(18).FontColor(Colors.Blue.Darken3);
                        c.Item().Text($"Generated by: {User?.Identity?.Name ?? "System"} | {dateLabel}").FontSize(10).FontColor(Colors.Grey.Darken1);
                        c.Item().PaddingBottom(5).LineHorizontal(1);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Employee").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Date").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Time In").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Time Out").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Total Hrs").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Late (min)").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Day Type").Bold().FontSize(9);
                        });

                        foreach (var a in attendances)
                        {
                            var name = $"{a.Employee?.EmployeeNumber} - {a.Employee?.User?.FirstName} {a.Employee?.User?.LastName}";
                            table.Cell().Padding(2).Text(name).FontSize(8);
                            table.Cell().Padding(2).Text(a.Date.ToString("MMM dd, yyyy")).FontSize(8);
                            table.Cell().Padding(2).Text(a.TimeIn.ToString(@"hh\:mm")).FontSize(8);
                            table.Cell().Padding(2).Text(a.TimeOut.ToString(@"hh\:mm")).FontSize(8);
                            table.Cell().Padding(2).AlignRight().Text($"{a.TotalHours:N2}").FontSize(8);
                            table.Cell().Padding(2).AlignRight().Text($"{a.LateMinutes}").FontSize(8);
                            table.Cell().Padding(2).Text(a.DayType.ToString()).FontSize(8);
                        }
                    });

                    page.Footer().Row(r =>
                    {
                        r.RelativeItem().AlignLeft().Text(x =>
                        {
                            x.Span($"Generated on {DateTime.Now:MMM dd, yyyy hh:mm tt}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        r.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ").FontSize(8);
                            x.CurrentPageNumber().FontSize(8);
                            x.Span(" of ").FontSize(8);
                            x.TotalPages().FontSize(8);
                        });
                    });
                });
            });

            return File(pdf, "application/pdf", $"AttendanceReport_{DateTime.Now:yyyyMMdd}.pdf");
        }

        public async Task<IActionResult> EmployeeReport(
            string? search = null,
            string? department = null,
            string? employmentType = null,
            string? status = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Employees
                .Include(e => e.User)
                .Include(e => e.Shift)
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.User != null)
                .AsQueryable();

            query = ApplyEmployeeFilters(query, search, department, employmentType, status);
            query = query.Where(e =>
                !(string.IsNullOrWhiteSpace(e.User!.FirstName) && string.IsNullOrWhiteSpace(e.User.LastName)) &&
                !string.IsNullOrWhiteSpace(e.EmployeeNumber));

            var totalEmployees = await query.CountAsync();
            var activeEmployees = await query.CountAsync(e => e.Status == EmploymentStatus.Active);
            var departments = await query
                .Where(e => e.DepartmentId != null)
                .Select(e => e.DepartmentId)
                .Distinct()
                .CountAsync();
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var newHires = await query.CountAsync(e => e.HireDate >= monthStart && e.HireDate <= DateTime.Today);

            page = Math.Max(1, page);
            var totalPages = (int)Math.Ceiling(totalEmployees / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var employees = await query
                .OrderBy(e => e.EmployeeNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.SelectedDepartment = department;
            ViewBag.SelectedEmploymentType = employmentType;
            ViewBag.SelectedStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRows = totalEmployees;
            ViewBag.GeneratedBy = User?.Identity?.Name ?? "System";
            ViewBag.GeneratedOn = DateTime.Now;
            ViewBag.DateRangeText = "All records";
            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.ActiveEmployees = activeEmployees;
            ViewBag.DepartmentsCount = departments;
            ViewBag.NewHires = newHires;
            await PopulateDepartmentDropdown();
            ViewBag.EmploymentTypes = Enum.GetNames(typeof(EmploymentType)).ToList();
            ViewBag.EmploymentStatuses = Enum.GetNames(typeof(EmploymentStatus)).ToList();

            return View(employees);
        }

        public async Task<IActionResult> ExportEmployeeExcel(
            string? search = null,
            string? department = null,
            string? employmentType = null,
            string? status = null)
        {
            var query = _context.Employees
                .Include(e => e.User)
                .Include(e => e.Shift)
                .Where(e => e.User != null)
                .AsQueryable();

            query = ApplyEmployeeFilters(query, search, department, employmentType, status);
            query = query.Where(e =>
                !(string.IsNullOrWhiteSpace(e.User!.FirstName) && string.IsNullOrWhiteSpace(e.User.LastName)) &&
                !string.IsNullOrWhiteSpace(e.EmployeeNumber));

            var employees = await query.OrderBy(e => e.EmployeeNumber).ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Employee Report");

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
                ws.Cells[row, 4].Style.Numberformat.Format = "₱#,##0.00";
                ws.Cells[row, 5].Value = e.Shift?.ShiftName ?? "N/A";
                ws.Cells[row, 6].Value = e.HireDate.ToString("yyyy-MM-dd");
                ws.Cells[row, 7].Value = e.Status.ToString();
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var fileName = $"EmployeeReport_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public async Task<IActionResult> ExportEmployeePdf(
            string? search = null,
            string? department = null,
            string? employmentType = null,
            string? status = null)
        {
            var query = _context.Employees
                .Include(e => e.User)
                .Include(e => e.Shift)
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.User != null)
                .AsQueryable();

            query = ApplyEmployeeFilters(query, search, department, employmentType, status);
            query = query.Where(e =>
                !(string.IsNullOrWhiteSpace(e.User!.FirstName) && string.IsNullOrWhiteSpace(e.User.LastName)) &&
                !string.IsNullOrWhiteSpace(e.EmployeeNumber));

            var employees = await query.OrderBy(e => e.EmployeeNumber).ToListAsync();

            var pdf = _pdfService.Generate(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(c =>
                    {
                        c.Item().Text("Employee Report").Bold().FontSize(18).FontColor(Colors.Blue.Darken3);
                        c.Item().Text($"Generated by: {User?.Identity?.Name ?? "System"}").FontSize(10).FontColor(Colors.Grey.Darken1);
                        c.Item().PaddingBottom(5).LineHorizontal(1);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Employee #").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Name").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Department").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Type").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Salary").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Hire Date").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Status").Bold().FontSize(9);
                        });

                        foreach (var e in employees)
                        {
                            table.Cell().Padding(2).Text(e.EmployeeNumber).FontSize(8);
                            table.Cell().Padding(2).Text($"{e.User?.FirstName} {e.User?.LastName}").FontSize(8);
                            table.Cell().Padding(2).Text(e.Department?.Name ?? "N/A").FontSize(8);
                            table.Cell().Padding(2).Text(e.EmploymentType?.ToString() ?? "N/A").FontSize(8);
                            table.Cell().Padding(2).AlignRight().Text($"₱{e.BasicSalary:N2}").FontSize(8);
                            table.Cell().Padding(2).Text(e.HireDate.ToString("MMM dd, yyyy")).FontSize(8);
                            table.Cell().Padding(2).Text(e.Status.ToString()).FontSize(8);
                        }
                    });

                    page.Footer().Row(r =>
                    {
                        r.RelativeItem().AlignLeft().Text(x =>
                        {
                            x.Span($"Generated on {DateTime.Now:MMM dd, yyyy hh:mm tt}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        r.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ").FontSize(8);
                            x.CurrentPageNumber().FontSize(8);
                            x.Span(" of ").FontSize(8);
                            x.TotalPages().FontSize(8);
                        });
                    });
                });
            });

            return File(pdf, "application/pdf", $"EmployeeReport_{DateTime.Now:yyyyMMdd}.pdf");
        }

        private static IQueryable<Payroll> ApplyPayrollFilters(
            IQueryable<Payroll> query,
            DateTime? from, DateTime? to,
            int? employeeId, string? department, string? payrollStatus)
        {
            if (from.HasValue)
                query = query.Where(p => p.PeriodStart >= from.Value);
            if (to.HasValue)
                query = query.Where(p => p.PeriodEnd <= to.Value);
            if (employeeId.HasValue)
                query = query.Where(p => p.EmployeeId == employeeId.Value);
            if (!string.IsNullOrWhiteSpace(department) && int.TryParse(department, out var deptId))
                query = query.Where(p => p.Employee.DepartmentId == deptId);
            if (!string.IsNullOrWhiteSpace(payrollStatus) && Enum.TryParse<PayrollStatus>(payrollStatus, out var parsedStatus))
                query = query.Where(p => p.Status == parsedStatus);
            return query;
        }

        private static IQueryable<Attendance> ApplyAttendanceFilters(
            IQueryable<Attendance> query,
            DateTime? dateFrom, DateTime? dateTo,
            int? employeeId, int? shiftId, string? department)
        {
            if (dateFrom.HasValue)
                query = query.Where(a => a.Date >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(a => a.Date <= dateTo.Value);
            if (employeeId.HasValue)
                query = query.Where(a => a.EmployeeId == employeeId.Value);
            if (shiftId.HasValue)
                query = query.Where(a => a.ShiftId == shiftId.Value);
            if (!string.IsNullOrWhiteSpace(department) && int.TryParse(department, out var deptId))
                query = query.Where(a => a.Employee.DepartmentId == deptId);
            return query;
        }

        private static IQueryable<Employee> ApplyEmployeeFilters(
            IQueryable<Employee> query,
            string? search,
            string? department,
            string? employmentType,
            string? status)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e =>
                    e.EmployeeNumber.Contains(search) ||
                    (e.User != null && (
                        e.User.FirstName.Contains(search) ||
                        e.User.LastName.Contains(search) ||
                        (e.User.Email != null && e.User.Email.Contains(search)))));
            }
            if (!string.IsNullOrWhiteSpace(department) && int.TryParse(department, out var deptId))
                query = query.Where(e => e.DepartmentId == deptId);
            if (!string.IsNullOrWhiteSpace(employmentType) && Enum.TryParse<EmploymentType>(employmentType, out var parsedType))
                query = query.Where(e => e.EmploymentType == parsedType);
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EmploymentStatus>(status, out var parsedStatus))
                query = query.Where(e => e.Status == parsedStatus);
            return query;
        }

        private async Task PopulateEmployeeDropdown()
        {
            ViewBag.Employees = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.Status == EmploymentStatus.Active && e.User != null)
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeId.ToString(),
                    Text = $"{e.EmployeeNumber} - {e.User!.FirstName} {e.User.LastName}"
                }).ToListAsync();
        }

        private async Task PopulateDepartmentDropdown()
        {
            ViewBag.Departments = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = d.Name })
                .ToListAsync();
        }

        private async Task PopulateShiftDropdown()
        {
            ViewBag.Shifts = await _context.Shifts
                .Where(s => s.IsActive)
                .OrderBy(s => s.ShiftName)
                .Select(s => new SelectListItem { Value = s.ShiftId.ToString(), Text = s.ShiftName })
                .ToListAsync();
        }

        private static string BuildDateRangeText(DateTime? from, DateTime? to)
        {
            if (from.HasValue && to.HasValue)
                return $"{from.Value:MMM dd, yyyy} - {to.Value:MMM dd, yyyy}";
            if (from.HasValue)
                return $"From {from.Value:MMM dd, yyyy}";
            if (to.HasValue)
                return $"Until {to.Value:MMM dd, yyyy}";
            return "All dates";
        }
    }
}
