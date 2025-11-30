using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/admin/overdue")]
    [Authorize(Roles = "Admin")]
    public class OverdueController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly EmailService _emailService;

        public OverdueController(LibraryDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("check-and-notify")]
        public async Task<ActionResult> CheckAndNotifyOverdue()
        {
            var now = DateTime.UtcNow;

            // Find all overdue loans
            var overdueLoans = await _context.Loans
                .Include(l => l.ReaderCard)
                    .ThenInclude(rc => rc.User)
                .Include(l => l.LoanItems)
                    .ThenInclude(li => li.Book)
                .Where(l => l.Status != "Returned" && 
                           l.DueDate < now && 
                           l.ReturnDate == null)
                .ToListAsync();

            var notificationsSent = 0;
            var readerGroups = overdueLoans
                .GroupBy(l => l.ReaderCardId)
                .ToList();

            foreach (var group in readerGroups)
            {
                var readerCard = group.First().ReaderCard;
                var overdueItems = group
                    .SelectMany(l => l.LoanItems.Where(li => li.Status != "Returned"))
                    .Select(li => (
                        BookTitle: li.Book.Title,
                        BorrowDate: li.Loan.BorrowDate,
                        DueDate: li.Loan.DueDate
                    ))
                    .ToList();

                // Update loan status to Overdue
                foreach (var loan in group)
                {
                    if (loan.Status != "Overdue")
                    {
                        loan.Status = "Overdue";
                    }

                    foreach (var item in loan.LoanItems.Where(li => li.Status != "Returned"))
                    {
                        item.Status = "Overdue";
                    }
                }

                // Send email notification
                var success = await _emailService.SendOverdueNotificationEmailAsync(
                    readerCard,
                    overdueItems);

                if (success)
                {
                    notificationsSent++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Overdue check completed",
                overdueLoansCount = overdueLoans.Count,
                notificationsSent
            });
        }

        [HttpGet("list")]
        public async Task<ActionResult> GetOverdueList()
        {
            var now = DateTime.UtcNow;

            // Lấy tất cả các loan đang được mượn (chưa trả) - bao gồm cả Borrowed và Overdue
            var activeLoans = await _context.Loans
                .Include(l => l.ReaderCard)
                    .ThenInclude(rc => rc.User)
                .Include(l => l.LoanItems)
                    .ThenInclude(li => li.Book)
                .Where(l => l.Status != "Returned" && l.ReturnDate == null)
                .OrderByDescending(l => l.DueDate)
                .ToListAsync();

            var overdueDetailsList = new List<OverdueDetailDto>();

            foreach (var loan in activeLoans)
            {
                if (loan.ReaderCard == null)
                    continue;

                // Lấy tất cả LoanItems chưa trả
                if (loan.LoanItems == null || !loan.LoanItems.Any())
                    continue;

                var activeItems = loan.LoanItems
                    .Where(li => li != null && 
                                 li.Book != null && 
                                 li.Status != "Returned")
                    .ToList();

                if (!activeItems.Any())
                    continue;

                foreach (var item in activeItems)
                {
                    var daysOverdue = loan.DueDate < now ? (int)(now - loan.DueDate).TotalDays : 0;
                    var daysRemaining = loan.DueDate >= now ? (int)(loan.DueDate - now).TotalDays : 0;
                    overdueDetailsList.Add(new OverdueDetailDto
                    {
                        LoanId = loan.Id,
                        ReaderCardId = loan.ReaderCard.Id,
                        ReaderName = loan.ReaderCard.FullName ?? "",
                        ReaderCardCode = loan.ReaderCard.CardCode ?? "",
                        ReaderEmail = loan.ReaderCard.User?.Email ?? "",
                        ReaderPhone = loan.ReaderCard.Phone,
                        ReaderAddress = loan.ReaderCard.Address,
                        BookTitle = item.Book?.Title ?? "",
                        BookManagementCode = item.Book?.ManagementCode ?? "",
                        BorrowDate = loan.BorrowDate,
                        DueDate = loan.DueDate,
                        DaysOverdue = daysOverdue,
                        DaysRemaining = daysRemaining,
                        Quantity = item.Quantity
                    });
                }
            }

            var result = overdueDetailsList
                .OrderByDescending(x => x.DaysOverdue)
                .ToList();

            return Ok(result);
        }

        [HttpPost("send-email/{readerCardId}")]
        public async Task<ActionResult> SendOverdueEmailToReader(int readerCardId)
        {
            var now = DateTime.UtcNow;

            // Find reader card with all overdue loans
            var readerCard = await _context.ReaderCards
                .Include(rc => rc.User)
                .FirstOrDefaultAsync(rc => rc.Id == readerCardId);

            if (readerCard == null)
            {
                return NotFound(new { message = "Reader card not found" });
            }

            // Find all overdue loans for this reader
            var overdueLoans = await _context.Loans
                .Include(l => l.LoanItems)
                    .ThenInclude(li => li.Book)
                .Where(l => l.ReaderCardId == readerCardId &&
                           (l.Status == "Overdue" || (l.Status != "Returned" && l.DueDate < now && l.ReturnDate == null)))
                .ToListAsync();

            if (overdueLoans.Count == 0)
            {
                return BadRequest(new { message = "No overdue books found for this reader" });
            }

            // Collect all overdue items
            var overdueItems = overdueLoans
                .SelectMany(l => l.LoanItems.Where(li => li != null && 
                                                         li.Book != null && 
                                                         (li.Status == "Overdue" || (li.Status != "Returned" && l.DueDate < now))))
                .Select(li => (
                    BookTitle: li.Book!.Title,
                    BorrowDate: li.Loan.BorrowDate,
                    DueDate: li.Loan.DueDate
                ))
                .ToList();

            // Send email notification
            var success = await _emailService.SendOverdueNotificationEmailAsync(
                readerCard,
                overdueItems);

            if (success)
            {
                return Ok(new { message = "Email sent successfully" });
            }
            else
            {
                return StatusCode(500, new { message = "Failed to send email" });
            }
        }
    }
}

