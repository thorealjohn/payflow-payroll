using itpayroll.Constant;
using itpayroll.Data;
using itpayroll.Models;
using itpayroll.Services;
using itpayroll.Utilities;
using itpayroll.ViewModels;
using itpayroll.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Controllers
{
    [Authorize]
    public class LeaveController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public LeaveController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Index(LeaveStatus? status, string? searchString = null, string period = "ThisMonth", string customDateFrom = null, string customDateTo = null)
        {
            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var query = _context.LeaveRequests
                .Include(l => l.Employee)
                .ThenInclude(e => e.User)
                .Include(l => l.ApprovedBy)
                .AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(l => l.StartDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(l => l.EndDate <= to.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(l => l.Status == status);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(l =>
                    l.Employee.User.FirstName.Contains(searchString) ||
                    l.Employee.User.LastName.Contains(searchString) ||
                    l.Employee.EmployeeNumber.Contains(searchString));
            }

            ViewBag.CurrentFilter = searchString;
            ViewBag.Period = period;
            ViewBag.CustomDateFrom = customDateFrom;
            ViewBag.CustomDateTo = customDateTo;

            var leaveRequests = await query
                .OrderByDescending(l => l.CreatedDate)
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            return View(leaveRequests);
        }

        [Authorize(Roles = $"{Roles.Employee}")]
        public async Task<IActionResult> MyLeaves(string period = "ThisMonth", string customDateFrom = null, string customDateTo = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;

            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var query = _context.LeaveRequests
                .Include(l => l.LeaveType)
                .Include(l => l.ApprovedBy)
                .Where(l => l.Employee.UserId == userId)
                .AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(l => l.StartDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(l => l.EndDate <= to.Value);
            }

            var leaves = await query.OrderByDescending(l => l.CreatedDate).ToListAsync();

            ViewBag.Period = period;
            ViewBag.CustomDateFrom = customDateFrom;
            ViewBag.CustomDateTo = customDateTo;

            return View(leaves);
        }

        [Authorize(Roles = $"{Roles.Employee}")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.Employee}")]
        public async Task<IActionResult> Create(CreateLeaveRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return View(model);
            }

            // Validate dates
            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("", "End date must be after start date.");
                return View(model);
            }

            // Check for overlapping leave requests
            var hasOverlap = await _context.LeaveRequests
                .AnyAsync(l => l.EmployeeId == employee.EmployeeId &&
                              l.Status != LeaveStatus.Rejected &&
                              l.Status != LeaveStatus.Cancelled &&
                              ((model.StartDate >= l.StartDate && model.StartDate <= l.EndDate) ||
                               (model.EndDate >= l.StartDate && model.EndDate <= l.EndDate) ||
                               (model.StartDate <= l.StartDate && model.EndDate >= l.EndDate)));

            if (hasOverlap)
            {
                ModelState.AddModelError("", "You have overlapping leave requests for these dates.");
                return View(model);
            }

            var leaveRequest = new LeaveRequest
            {
                EmployeeId = employee.EmployeeId,
                LeaveType = model.LeaveType,
                StartDate = model.StartDate.Date,
                EndDate = model.EndDate.Date,
                DaysRequested = model.DaysRequested,
                Reason = model.Reason,
                Status = LeaveStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave request submitted successfully.";
            return RedirectToAction(nameof(MyLeaves));
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Details(int id)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.Employee)
                .ThenInclude(e => e.User)
                .Include(l => l.ApprovedBy)
                .FirstOrDefaultAsync(l => l.LeaveRequestId == id);

            if (leaveRequest == null)
                return NotFound();

            return View(leaveRequest);
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Approve(int id)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.LeaveRequestId == id);

            if (leaveRequest == null)
                return NotFound();

            leaveRequest.Status = LeaveStatus.Approved;
            leaveRequest.ApprovedById = _userManager.GetUserId(User);
            leaveRequest.ApprovedDate = DateTime.UtcNow;

            // Send notification to employee
            if (leaveRequest.Employee?.UserId != null)
            {
                await _notificationService.CreateNotification(
                    leaveRequest.Employee.UserId,
                    "Leave Request Approved",
                    $"Your {leaveRequest.LeaveType} leave request has been approved.");
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave request approved.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Reject(int id)
        {
            var leaveRequest = await _context.LeaveRequests
                .FindAsync(id);

            if (leaveRequest == null)
                return NotFound();

            return View(leaveRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.LeaveRequestId == id);

            if (leaveRequest == null)
                return NotFound();

            leaveRequest.Status = LeaveStatus.Rejected;
            leaveRequest.RejectionReason = rejectionReason;
            leaveRequest.ApprovedById = _userManager.GetUserId(User);
            leaveRequest.ApprovedDate = DateTime.UtcNow;

            // Send notification to employee
            if (leaveRequest.Employee?.UserId != null)
            {
                await _notificationService.CreateNotification(
                    leaveRequest.Employee.UserId,
                    "Leave Request Rejected",
                    $"Your {leaveRequest.LeaveType} leave request has been rejected. Reason: {rejectionReason}");
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave request rejected.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{Roles.Employee}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId);

            var employeeId = employee?.EmployeeId;
            var leaveRequest = await _context.LeaveRequests
                .FirstOrDefaultAsync(l => l.LeaveRequestId == id &&
                                         l.EmployeeId == employeeId);

            if (leaveRequest == null)
                return NotFound();

            if (leaveRequest.Status != LeaveStatus.Pending)
            {
                TempData["Error"] = "Only pending leave requests can be cancelled.";
                return RedirectToAction(nameof(MyLeaves));
            }

            leaveRequest.Status = LeaveStatus.Cancelled;
            leaveRequest.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave request cancelled.";
            return RedirectToAction(nameof(MyLeaves));
        }
    }
}
