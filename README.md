# Library Management System

A comprehensive library management system built with .NET MAUI (frontend) and ASP.NET Core Web API (backend).

## Architecture

- **Frontend**: .NET MAUI (multi-platform app)
- **Backend**: ASP.NET Core Web API
- **Database**: SQL Server (LocalDB) via Entity Framework Core
- **Authentication**: JWT Bearer tokens

## Features

### Admin Features
- User account management (create, reset password, lock/unlock)
- Book management (CRUD operations)
- Reader card lookup with borrowing history
- Borrow/return books with email verification
- Overdue book notifications
- Dashboard with statistics

### Reader Features
- View profile and reader card
- Browse books (Popular, New, Most Accessed)
- Search books
- View current and historical loans

## Setup Instructions

### Backend (API)

1. Navigate to the `LibraryAPI` folder:
   ```bash
   cd LibraryAPI
   ```

2. Update `appsettings.json` with your database connection string and email settings:
   - Database connection string (default uses LocalDB)
   - Email SMTP settings for sending verification codes

3. Run the API:
   ```bash
   dotnet run
   ```

4. The API will:
   - Create the database automatically
   - Seed initial data (admin user, sample readers, books)
   - Be available at `https://localhost:7000` (check your console output)

### Frontend (MAUI App)

1. Update the API base URL in `Services/ApiService.cs`:
   ```csharp
   _baseUrl = "https://localhost:7000/api"; // Update to match your API URL
   ```

2. For Android/iOS, you may need to configure network security settings

3. Run the MAUI app:
   - Visual Studio: Select target platform and run
   - Command line: `dotnet build` then run on your platform

## Default Credentials

After seeding, you can log in with:

**Admin Accounts:**
- Username: `admin` | Password: `admin123`
- Username: `admin2` | Password: `admin123`

**Reader Accounts:**
- Username: `reader1` | Password: `reader123` | Card Code: `RC001001`
- Username: `reader2` | Password: `reader123` | Card Code: `RC001002`

See `LibraryAPI/TEST_CREDENTIALS.md` for full details.

## Email Configuration

Update `appsettings.json` in the LibraryAPI project with your SMTP settings:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "your-app-password",
    "SenderName": "Library Management System"
  }
}
```

For Gmail, you'll need to use an App Password instead of your regular password.

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login

### Admin Endpoints
- `POST /api/admin/users` - Create user
- `GET /api/admin/users` - Get all users
- `PUT /api/admin/users/{id}/reset-password` - Reset password
- `PUT /api/admin/users/{id}/toggle-active` - Toggle user active status
- `GET /api/admin/readers/{cardCode}` - Get reader by card code
- `GET /api/admin/books` - Get all books (admin)
- `POST /api/admin/books` - Create book
- `PUT /api/admin/books/{id}` - Update book
- `DELETE /api/admin/books/{id}` - Delete book
- `POST /api/admin/borrow/request` - Request borrow (sends email code)
- `POST /api/admin/borrow/confirm` - Confirm borrow with code
- `POST /api/admin/return/request` - Request return (sends email code)
- `POST /api/admin/return/confirm` - Confirm return with code
- `POST /api/admin/overdue/check-and-notify` - Check and send overdue notifications

### Public/Reader Endpoints
- `GET /api/books` - Search/filter books
- `GET /api/books/{id}` - Get book details
- `GET /api/books/popular` - Get popular books
- `GET /api/books/new` - Get new books
- `GET /api/books/most-accessed` - Get most accessed books
- `GET /api/reader/profile` - Get reader profile
- `GET /api/reader/my-loans` - Get reader's loans

## Database Schema

- **Users**: User accounts with roles (Admin/Reader)
- **ReaderCards**: Reader card information linked to users
- **Books**: Book catalog
- **Authors**: Author information
- **BookAuthors**: Many-to-many relationship between books and authors
- **Loans**: Loan transactions
- **LoanItems**: Individual items in a loan
- **EmailVerificationCodes**: Verification codes for borrow/return operations

## Notes

- The system automatically creates a Reader Card when a Reader user is created
- Borrow/return operations require email verification codes
- Overdue notifications can be triggered manually via the admin endpoint
- The database is created automatically on first run with seed data

## Development Notes

- Use HTTPS for the API in production
- Configure proper CORS settings for production
- Use environment variables for sensitive configuration
- Implement proper error handling and logging
- Add unit tests for critical functionality

