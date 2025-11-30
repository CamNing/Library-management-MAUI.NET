using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Models;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/admin/return")]
    [Authorize(Roles = "Admin")]
    public class ReturnController : ControllerBase
    {
        private readonly LibraryDbContext _context;
        private readonly EmailService _emailService;

        public ReturnController(LibraryDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("request")]
        public async Task<ActionResult> RequestReturn([FromBody] ReturnRequest request)
        {
            var readerCard = await _context.ReaderCards
                .Include(rc => rc.User)
                .FirstOrDefaultAsync(rc => rc.CardCode == request.ReaderCardCode);

            if (readerCard == null)
            {
                return NotFound(new { message = "Reader card not found" });
            }

            // Get loan items to return
            var loanItems = await _context.LoanItems
                .Include(li => li.Book)
                .Include(li => li.Loan)
                .Where(li => request.LoanItemIds.Contains(li.Id) && li.Status != "Returned")
                .ToListAsync();

            if (loanItems.Count != request.LoanItemIds.Count)
            {
                return BadRequest(new { message = "One or more loan items not found or already returned" });
            }

            // Generate verification code
            var random = new Random();
            var verificationCode = random.Next(100000, 999999).ToString();
            var expiresAt = DateTime.UtcNow.AddMinutes(10);

            var emailCode = new EmailVerificationCode
            {
                ReaderCardId = readerCard.Id,
                Code = verificationCode,
                Type = "Return",
                ExpiresAt = expiresAt,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow,
                RequestData = JsonSerializer.Serialize(request)
            };

            _context.EmailVerificationCodes.Add(emailCode);
            await _context.SaveChangesAsync();

            // Send email
            var bookTitles = loanItems.Select(li => li.Book.Title).ToList();
            await _emailService.SendReturnVerificationEmailAsync(
                readerCard,
                bookTitles,
                verificationCode);

            return Ok(new
            {
                message = "Verification code sent to reader's email",
                verificationCodeId = emailCode.Id,
                expiresAt
            });
        }

        [HttpPost("confirm")]
        public async Task<ActionResult> ConfirmReturn([FromBody] ConfirmReturnRequest request)
        {
            var emailCode = await _context.EmailVerificationCodes
                .Include(evc => evc.ReaderCard)
                    .ThenInclude(rc => rc.User)
                .FirstOrDefaultAsync(evc => evc.Id == request.VerificationCodeId);

            if (emailCode == null)
            {
                return NotFound(new { message = "Verification code not found" });
            }

            if (emailCode.IsUsed)
            {
                return BadRequest(new { message = "Verification code already used" });
            }

            if (emailCode.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Verification code expired" });
            }

            if (emailCode.Code != request.Code)
            {
                return BadRequest(new { message = "Invalid verification code" });
            }

            if (emailCode.Type != "Return")
            {
                return BadRequest(new { message = "Invalid verification code type" });
            }

            // Deserialize return request
            if (string.IsNullOrEmpty(emailCode.RequestData))
            {
                return BadRequest(new { message = "Invalid request data" });
            }

            var returnRequest = JsonSerializer.Deserialize<ReturnRequest>(emailCode.RequestData);
            if (returnRequest == null)
            {
                return BadRequest(new { message = "Invalid request data format" });
            }

            // Get loan items
            var loanItems = await _context.LoanItems
                .Include(li => li.Book)
                .Include(li => li.Loan)
                .Where(li => returnRequest.LoanItemIds.Contains(li.Id))
                .ToListAsync();

            var returnDate = DateTime.UtcNow;

            // Update loan items and books
            foreach (var loanItem in loanItems)
            {
                loanItem.Status = "Returned";
                loanItem.Book.AvailableQuantity += loanItem.Quantity;

                // Update loan status if all items are returned
                var allItemsReturned = await _context.LoanItems
                    .AllAsync(li => li.LoanId == loanItem.LoanId && (li.Status == "Returned" || li.Id == loanItem.Id));

                if (allItemsReturned)
                {
                    loanItem.Loan.Status = "Returned";
                    loanItem.Loan.ReturnDate = returnDate;
                }
            }

            emailCode.IsUsed = true;
            await _context.SaveChangesAsync();

            // Reload reader card with user info to ensure we have all data for email
            var readerCard = await _context.ReaderCards
                .Include(rc => rc.User)
                .FirstOrDefaultAsync(rc => rc.Id == emailCode.ReaderCardId);

            if (readerCard == null)
            {
                return BadRequest(new { message = "Reader card not found" });
            }

            // Send notification email after successful confirmation
            var bookTitles = loanItems.Select(li => li.Book.Title).ToList();
            var loanId = loanItems.FirstOrDefault()?.LoanId ?? 0;
            await _emailService.SendReturnNotificationEmailAsync(
                readerCard,
                bookTitles,
                returnDate,
                loanId);

            return Ok(new { message = "Books returned successfully. Notification email sent." });
        }

        [HttpPost("execute")]
        public async Task<ActionResult> ExecuteReturn([FromBody] ReturnRequest request)
        {
            var readerCard = await _context.ReaderCards
                .Include(rc => rc.User)
                .FirstOrDefaultAsync(rc => rc.CardCode == request.ReaderCardCode);

            if (readerCard == null)
            {
                return NotFound(new { message = "Reader card not found" });
            }

            // Get loan items to return
            var loanItems = await _context.LoanItems
                .Include(li => li.Book)
                .Include(li => li.Loan)
                .Where(li => request.LoanItemIds.Contains(li.Id) && li.Status != "Returned")
                .ToListAsync();

            if (loanItems.Count != request.LoanItemIds.Count)
            {
                return BadRequest(new { message = "One or more loan items not found or already returned" });
            }

            var returnDate = DateTime.UtcNow;
            var bookTitles = loanItems.Select(li => li.Book.Title).ToList();
            var loanId = loanItems.FirstOrDefault()?.LoanId ?? 0;

            // Update loan items and books
            foreach (var loanItem in loanItems)
            {
                loanItem.Status = "Returned";
                loanItem.Book.AvailableQuantity += loanItem.Quantity;

                // Update loan status if all items are returned
                var allItemsReturned = await _context.LoanItems
                    .AllAsync(li => li.LoanId == loanItem.LoanId && (li.Status == "Returned" || li.Id == loanItem.Id));

                if (allItemsReturned)
                {
                    loanItem.Loan.Status = "Returned";
                    loanItem.Loan.ReturnDate = returnDate;
                }
            }

            await _context.SaveChangesAsync();

            // Send notification email
            await _emailService.SendReturnNotificationEmailAsync(
                readerCard,
                bookTitles,
                returnDate,
                loanId);

            return Ok(new { message = "Books returned successfully. Notification email sent." });
        }
    }
}

