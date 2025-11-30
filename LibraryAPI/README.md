# Library Management System - Backend API

ASP.NET Core Web API for the Library Management System.

## Quick Start

1. Update `appsettings.json`:
   - Database connection string
   - JWT settings (SecretKey)
   - Email SMTP settings

2. Run the API:
   ```bash
   dotnet run
   ```

3. The API will:
   - Create the database automatically on first run
   - Seed initial data (admin user, sample readers, books)
   - Start on `https://localhost:8080` (HTTPS) or `http://localhost:8080` (HTTP)

## Default Credentials

**Admin Users (for testing):**
- Username: `admin` | Password: `admin123`
- Username: `admin2` | Password: `admin123`

**Reader Users:**
- Username: `reader1` | Password: `reader123` | Card Code: `RC001001`
- Username: `reader2` | Password: `reader123` | Card Code: `RC001002`

## API Documentation

Once running, visit `https://localhost:8080/swagger` for interactive API documentation.

## Email Configuration

For Gmail:
1. Enable 2-Step Verification
2. Generate an App Password
3. Use the App Password in `appsettings.json`

Example configuration:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "your-16-char-app-password",
    "SenderName": "Library Management System"
  }
}
```

## Database

The system uses SQL Server LocalDB by default. To use SQL Server:
1. Update the connection string in `appsettings.json`
2. Run migrations (if using migrations instead of `EnsureCreated`):
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

## Features

- JWT Authentication
- Role-based Authorization (Admin/Reader)
- Book Management (CRUD)
- User Management
- Borrow/Return with Email Verification
- Overdue Notifications
- Search and Filtering
- Popular/New/Most Accessed Books

