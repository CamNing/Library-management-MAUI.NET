# Test Credentials

## Admin Accounts (for testing)

1. **Username:** `admin`  
   **Password:** `admin123`  
   **Email:** admin@library.com

2. **Username:** `admin2`  
   **Password:** `admin123`  
   **Email:** admin2@library.com

## Reader Accounts

1. **Username:** `reader1`  
   **Password:** `reader123`  
   **Email:** reader1@library.com  
   **Reader Card Code:** `RC001001`  
   **Full Name:** John Doe

2. **Username:** `reader2`  
   **Password:** `reader123`  
   **Email:** reader2@library.com  
   **Reader Card Code:** `RC001002`  
   **Full Name:** Jane Smith

## Sample Books

- **LIB-001:** Harry Potter and the Philosopher's Stone (Fantasy)
- **LIB-002:** A Game of Thrones (Fantasy)
- **LIB-003:** The Shining (Horror)
- **LIB-004:** Murder on the Orient Express (Mystery)
- **LIB-005:** Foundation (Science Fiction)

## How to Add More Admin Users

### Option 1: Via API (recommended)
Use the Swagger UI at `http://localhost:8080/swagger`:
- Endpoint: `POST /api/admin/users`
- Body:
```json
{
  "username": "newadmin",
  "password": "admin123",
  "email": "newadmin@library.com",
  "role": "Admin"
}
```

### Option 2: Delete database and reseed
1. Delete the database file or drop database
2. Restart API - it will automatically seed with all users including `admin` and `admin2`



