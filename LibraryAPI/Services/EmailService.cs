using LibraryAPI.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace LibraryAPI.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _configuration["EmailSettings:SenderName"],
                    _configuration["EmailSettings:SenderEmail"]));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = body };

                using var client = new SmtpClient();
                await client.ConnectAsync(
                    _configuration["EmailSettings:SmtpServer"],
                    Convert.ToInt32(_configuration["EmailSettings:SmtpPort"]),
                    SecureSocketOptions.StartTls);

                await client.AuthenticateAsync(
                    _configuration["EmailSettings:SenderEmail"],
                    _configuration["EmailSettings:SenderPassword"]);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendBorrowVerificationEmailAsync(ReaderCard readerCard, List<string> bookTitles, DateTime borrowDate, DateTime dueDate, string verificationCode)
        {
            var body = $@"
                <html>
                <body>
                    <h2>Library Borrowing Verification</h2>
                    <p>Dear {readerCard.FullName},</p>
                    <p>You have requested to borrow the following book(s):</p>
                    <ul>
                        {string.Join("", bookTitles.Select(title => $"<li>{title}</li>"))}
                    </ul>
                    <p><strong>Borrow Date:</strong> {borrowDate:yyyy-MM-dd}</p>
                    <p><strong>Due Date:</strong> {dueDate:yyyy-MM-dd}</p>
                    <p><strong>Verification Code:</strong> <strong style='font-size: 20px; color: #007bff;'>{verificationCode}</strong></p>
                    <p>Please provide this code to the librarian to complete your borrowing transaction.</p>
                    <p>This code will expire in 10 minutes.</p>
                    <p>Thank you,<br>Library Management System</p>
                </body>
                </html>";

            return await SendEmailAsync(readerCard.User.Email, "Borrowing Verification Code", body);
        }

        public async Task<bool> SendReturnVerificationEmailAsync(ReaderCard readerCard, List<string> bookTitles, string verificationCode)
        {
            var body = $@"
                <html>
                <body>
                    <h2>Library Return Verification</h2>
                    <p>Dear {readerCard.FullName},</p>
                    <p>You are returning the following book(s):</p>
                    <ul>
                        {string.Join("", bookTitles.Select(title => $"<li>{title}</li>"))}
                    </ul>
                    <p><strong>Verification Code:</strong> <strong style='font-size: 20px; color: #007bff;'>{verificationCode}</strong></p>
                    <p>Please provide this code to the librarian to complete your return transaction.</p>
                    <p>This code will expire in 10 minutes.</p>
                    <p>Thank you,<br>Library Management System</p>
                </body>
                </html>";

            return await SendEmailAsync(readerCard.User.Email, "Return Verification Code", body);
        }

        public async Task<bool> SendOverdueNotificationEmailAsync(ReaderCard readerCard, List<(string BookTitle, DateTime BorrowDate, DateTime DueDate)> overdueBooks)
        {
            var body = $@"
                <html>
                <body>
                    <h2>Overdue Book(s) Notification</h2>
                    <p>Dear {readerCard.FullName},</p>
                    <p>The following book(s) are overdue and need to be returned:</p>
                    <ul>
                        {string.Join("", overdueBooks.Select(b => $"<li><strong>{b.BookTitle}</strong><br>Borrowed: {b.BorrowDate:yyyy-MM-dd}<br>Due: {b.DueDate:yyyy-MM-dd}</li>"))}
                    </ul>
                    <p>Please return these books as soon as possible to avoid additional fees.</p>
                    <p>Thank you,<br>Library Management System</p>
                </body>
                </html>";

            return await SendEmailAsync(readerCard.User.Email, "Overdue Book(s) Notification", body);
        }

        public async Task<bool> SendBorrowApprovalEmailAsync(ReaderCard readerCard, List<string> bookTitles, DateTime borrowDate, DateTime dueDate)
        {
            var daysRemaining = (dueDate - DateTime.UtcNow).Days;
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; border-radius: 10px 10px 0 0; text-align: center;'>
                            <h2 style='color: white; margin: 0;'>✅ Yêu cầu mượn sách đã được duyệt</h2>
                        </div>
                        <div style='background-color: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; border-radius: 0 0 10px 10px;'>
                            <p>Xin chào <strong>{readerCard.FullName}</strong>,</p>
                            <p style='color: #4CAF50; font-size: 16px; font-weight: bold;'>Yêu cầu mượn sách của bạn đã được phê duyệt thành công!</p>
                            <p>Bạn có thể đến thư viện để nhận các cuốn sách sau:</p>
                            <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #4CAF50;'>
                                <p style='margin-top: 0; font-weight: bold; color: #2c3e50;'>📚 Danh sách sách đã được duyệt:</p>
                                <ul style='margin: 10px 0; padding-left: 20px;'>
                                    {string.Join("", bookTitles.Select(title => $"<li style='margin: 8px 0;'>{title}</li>"))}
                                </ul>
                            </div>
                            <div style='background-color: #e3f2fd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <p style='margin: 5px 0;'><strong>📅 Ngày mượn:</strong> {borrowDate:dd/MM/yyyy}</p>
                                <p style='margin: 5px 0;'><strong>⏰ Hạn trả:</strong> <span style='color: #f44336; font-weight: bold;'>{dueDate:dd/MM/yyyy}</span></p>
                                <p style='margin: 5px 0;'><strong>📊 Thời gian mượn:</strong> {daysRemaining} ngày</p>
                            </div>
                            <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                                <p style='margin: 0;'><strong>⚠️ Lưu ý quan trọng:</strong></p>
                                <ul style='margin: 10px 0; padding-left: 20px;'>
                                    <li>Vui lòng đến thư viện để nhận sách trong vòng 3 ngày kể từ ngày nhận email này</li>
                                    <li>Nhớ mang theo thẻ độc giả khi đến nhận sách</li>
                                    <li>Vui lòng trả sách đúng hạn để tránh phí phạt</li>
                                </ul>
                            </div>
                            <p style='margin-top: 30px;'>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với thư viện.</p>
                            <p style='color: #6c757d; font-size: 12px; margin-top: 30px; border-top: 1px solid #e0e0e0; padding-top: 20px;'>
                                Trân trọng,<br>
                                <strong>Hệ thống quản lý thư viện</strong>
                            </p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(readerCard.User.Email, "✅ Yêu cầu mượn sách đã được duyệt", body);
        }

        public async Task<bool> SendBorrowRejectionEmailAsync(ReaderCard readerCard, List<string> bookTitles, string? reason)
        {
            var body = $@"
                <html>
                <body>
                    <h2>Borrow Request Rejected</h2>
                    <p>Dear {readerCard.FullName},</p>
                    <p>Unfortunately, your borrow request for the following book(s) has been rejected:</p>
                    <ul>
                        {string.Join("", bookTitles.Select(title => $"<li>{title}</li>"))}
                    </ul>
                    {(string.IsNullOrWhiteSpace(reason) ? "" : $"<p><strong>Reason:</strong> {reason}</p>")}
                    <p>If you have any questions, please contact the library.</p>
                    <p>Thank you,<br>Library Management System</p>
                </body>
                </html>";

            return await SendEmailAsync(readerCard.User.Email, "Borrow Request Rejected", body);
        }

        public async Task<bool> SendBorrowNotificationEmailAsync(ReaderCard readerCard, List<string> bookTitles, DateTime borrowDate, DateTime dueDate, int loanId)
        {
            var reportLink = _configuration["EmailSettings:ReportLink"] ?? "mailto:" + _configuration["EmailSettings:SenderEmail"] + "?subject=Báo cáo lỗi mượn sách&body=Tôi muốn báo cáo về giao dịch mượn sách không chính xác.";
            
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #2c3e50;'>Thông báo mượn sách tại thư viện</h2>
                        <p>Xin chào <strong>{readerCard.FullName}</strong>,</p>
                        <p>Bạn đã mượn sách tại thư viện với thông tin sau:</p>
                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                            <p><strong>Danh sách sách đã mượn:</strong></p>
                            <ul>
                                {string.Join("", bookTitles.Select(title => $"<li>{title}</li>"))}
                            </ul>
                            <p><strong>Ngày mượn:</strong> {borrowDate:dd/MM/yyyy}</p>
                            <p><strong>Hạn trả:</strong> {dueDate:dd/MM/yyyy}</p>
                        </div>
                        <p>Nếu thông tin trên là <strong>chính xác</strong>, bạn có thể bỏ qua email này.</p>
                        <p>Nếu thông tin trên <strong>không chính xác</strong>, vui lòng báo cáo cho thư viện bằng cách:</p>
                        <div style='text-align: center; margin: 20px 0;'>
                            <a href='{reportLink}' style='background-color: #dc3545; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;'>Báo cáo lỗi</a>
                        </div>
                        <p style='color: #6c757d; font-size: 12px; margin-top: 30px;'>Trân trọng,<br>Hệ thống quản lý thư viện</p>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(readerCard.User.Email, "Thông báo mượn sách tại thư viện", body);
        }

        public async Task<bool> SendReturnNotificationEmailAsync(ReaderCard readerCard, List<string> bookTitles, DateTime returnDate, int loanId)
        {
            var reportLink = _configuration["EmailSettings:ReportLink"] ?? "mailto:" + _configuration["EmailSettings:SenderEmail"] + "?subject=Báo cáo lỗi trả sách&body=Tôi muốn báo cáo về giao dịch trả sách không chính xác.";
            
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #2c3e50;'>Thông báo trả sách tại thư viện</h2>
                        <p>Xin chào <strong>{readerCard.FullName}</strong>,</p>
                        <p>Bạn đã trả sách tại thư viện với thông tin sau:</p>
                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                            <p><strong>Danh sách sách đã trả:</strong></p>
                            <ul>
                                {string.Join("", bookTitles.Select(title => $"<li>{title}</li>"))}
                            </ul>
                            <p><strong>Ngày trả:</strong> {returnDate:dd/MM/yyyy}</p>
                        </div>
                        <p>Nếu thông tin trên là <strong>chính xác</strong>, bạn có thể bỏ qua email này.</p>
                        <p>Nếu thông tin trên <strong>không chính xác</strong>, vui lòng báo cáo cho thư viện bằng cách:</p>
                        <div style='text-align: center; margin: 20px 0;'>
                            <a href='{reportLink}' style='background-color: #dc3545; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;'>Báo cáo lỗi</a>
                        </div>
                        <p style='color: #6c757d; font-size: 12px; margin-top: 30px;'>Trân trọng,<br>Hệ thống quản lý thư viện</p>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(readerCard.User.Email, "Thông báo trả sách tại thư viện", body);
        }
    }
}

