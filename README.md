# User Management API Documentation

## Overview

This User Management API provides CRUD (Create, Read, Update, Delete) operations for managing user records in TechHive Solutions' internal tools. The API is designed for HR and IT departments to efficiently manage employee information.

## Base URL

```
http://localhost:5229/api/users
```

## User Model

A user object contains the following properties:

```json
{
  "id": 1,
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@techhive.com",
  "phoneNumber": "+1-555-0101",
  "department": "IT",
  "jobTitle": "Software Developer",
  "createdAt": "2025-10-02T14:22:47.7019239Z",
  "updatedAt": null,
  "isActive": true
}
```

### Field Descriptions

- **id** (integer): Unique identifier for the user
- **firstName** (string, required): User's first name (max 100 characters)
- **lastName** (string, required): User's last name (max 100 characters)
- **email** (string, required): User's email address (must be valid email format, max 255 characters)
- **phoneNumber** (string, optional): User's phone number (max 20 characters)
- **department** (string, optional): User's department (max 100 characters)
- **jobTitle** (string, optional): User's job title (max 100 characters)
- **createdAt** (datetime): Timestamp when the user was created
- **updatedAt** (datetime, nullable): Timestamp when the user was last updated
- **isActive** (boolean): Whether the user is active (soft delete flag)

## API Endpoints

### 1. Get All Users

**GET** `/api/users`

Retrieves a list of all active users, ordered by last name, then first name.

**Response:**
- **200 OK**: Returns array of user objects
- **500 Internal Server Error**: Server error

**Example Response:**
```json
[
  {
    "id": 1,
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@techhive.com",
    "phoneNumber": "+1-555-0101",
    "department": "IT",
    "jobTitle": "Software Developer",
    "createdAt": "2025-10-02T14:22:47.7019239Z",
    "updatedAt": null,
    "isActive": true
  }
]
```

### 2. Get User by ID

**GET** `/api/users/{id}`

Retrieves a specific user by their ID.

**Parameters:**
- **id** (path parameter): User ID (integer)

**Response:**
- **200 OK**: Returns the user object
- **400 Bad Request**: Invalid user ID
- **404 Not Found**: User not found
- **500 Internal Server Error**: Server error

**Example Response:**
```json
{
  "id": 1,
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@techhive.com",
  "phoneNumber": "+1-555-0101",
  "department": "IT",
  "jobTitle": "Software Developer",
  "createdAt": "2025-10-02T14:22:47.7019239Z",
  "updatedAt": null,
  "isActive": true
}
```

### 3. Create New User

**POST** `/api/users`

Creates a new user.

**Request Body:**
```json
{
  "firstName": "Alice",
  "lastName": "Williams",
  "email": "alice.williams@techhive.com",
  "phoneNumber": "+1-555-0104",
  "department": "Marketing",
  "jobTitle": "Marketing Specialist"
}
```

**Required Fields:**
- firstName
- lastName
- email

**Response:**
- **201 Created**: Returns the created user object with generated ID
- **400 Bad Request**: Validation errors
- **409 Conflict**: Email already exists
- **500 Internal Server Error**: Server error

### 4. Update User

**PUT** `/api/users/{id}`

Updates an existing user. This is a partial update - only provided fields will be updated.

**Parameters:**
- **id** (path parameter): User ID (integer)

**Request Body (all fields optional):**
```json
{
  "firstName": "Updated First Name",
  "lastName": "Updated Last Name",
  "email": "updated.email@techhive.com",
  "phoneNumber": "+1-555-9999",
  "department": "Updated Department",
  "jobTitle": "Updated Job Title",
  "isActive": true
}
```

**Response:**
- **200 OK**: Returns the updated user object
- **400 Bad Request**: Invalid user ID or validation errors
- **404 Not Found**: User not found
- **409 Conflict**: Email already exists for another user
- **500 Internal Server Error**: Server error

### 5. Delete User

**DELETE** `/api/users/{id}`

Deletes a user (soft delete - marks user as inactive).

**Parameters:**
- **id** (path parameter): User ID (integer)

**Response:**
- **200 OK**: Success message
- **400 Bad Request**: Invalid user ID
- **404 Not Found**: User not found
- **500 Internal Server Error**: Server error

**Example Response:**
```json
{
  "message": "User with ID 3 has been successfully deleted"
}
```

## Error Responses

All error responses follow a consistent format:

```json
{
  "message": "Error description",
  "error": "Detailed error information (in development mode)"
}
```

### Common HTTP Status Codes

- **200 OK**: Request successful
- **201 Created**: Resource created successfully
- **400 Bad Request**: Invalid request data
- **404 Not Found**: Resource not found
- **409 Conflict**: Resource conflict (e.g., duplicate email)
- **500 Internal Server Error**: Server error

## Testing

You can test the API using the provided `Users.http` file with REST Client extension in VS Code, or use tools like Postman, curl, or wget.

### Sample Test Requests

1. **Get all users:**
   ```bash
   wget -qO- "http://localhost:5229/api/users"
   ```

2. **Create a user:**
   ```bash
   wget -qO- --post-data='{"firstName":"Test","lastName":"User","email":"test@techhive.com"}' \
        --header="Content-Type: application/json" \
        "http://localhost:5229/api/users"
   ```

3. **Update a user:**
   ```bash
   wget -qO- --method=PUT \
        --body-data='{"department":"Updated Department"}' \
        --header="Content-Type: application/json" \
        "http://localhost:5229/api/users/1"
   ```

## Implementation Notes

- The API uses in-memory storage for demonstration purposes. In production, this should be replaced with a proper database.
- Email addresses are validated and must be unique across all users.
- Deletion is implemented as a "soft delete" - users are marked as inactive rather than permanently removed.
- All timestamps are in UTC format.
- Input validation is performed on all endpoints with appropriate error messages.
- The API follows RESTful conventions and returns appropriate HTTP status codes.

## Future Enhancements

Consider implementing the following features for production use:

1. **Database Integration**: Replace in-memory storage with Entity Framework and SQL Server/PostgreSQL
2. **Authentication & Authorization**: Add JWT authentication and role-based authorization
3. **Pagination**: Add pagination support for large user lists
4. **Search & Filtering**: Add search and filtering capabilities
5. **Audit Logging**: Track all user modifications
6. **Bulk Operations**: Support for bulk user operations
7. **User Profile Images**: Support for uploading and managing user profile pictures
8. **Email Notifications**: Send notifications for user creation/updates
9. **Data Export**: Export user data in various formats (CSV, Excel)
10. **API Versioning**: Implement API versioning for backward compatibility