# ECommerce2

A modular e-commerce backend built with ASP.NET Core Web API and .NET 10.

## Overview

ECommerce2 is a backend system for an online store. It provides authentication and authorization, product management, shopping cart functionality, order checkout, inventory management, and order tracking.

The project is organized into separate modules, with each module following a 3-tier structure:

API
↓
Services
↓
Data
↓
SQL Server

## Architecture

### API Layer

Handles HTTP requests, routing, authentication, authorization, and responses.

### Services Layer

Contains business logic, validation, DTOs, and mapping.

### Data Layer

Contains entities, repositories, EF Core configurations, and database access.

## Project Structure

ECommerce2
|
|-- docs
|
|-- src
|   |-- Authentication
|   |   |-- Auth.API
|   |   |-- Auth.Services
|   |   |-- Auth.Data
|   |
|   |-- Cart
|   |   |-- Cart.API
|   |   |-- Cart.Services
|   |   |-- Cart.Data
|   |
|   |-- Order
|   |   |-- Order.API
|   |   |-- Order.Services
|   |   |-- Order.Data
|   |
|   |-- Product
|   |   |-- Product.API
|   |   |-- Product.Services
|   |   |-- Product.Data
|   |
|   |-- Tracking
|   |   |-- Tracking.API
|   |   |-- Tracking.Services
|   |   |-- Tracking.Data
|   |
|   |-- ECommerce.Database
|
|-- tests
    |-- ECommerce.UnitTests
    |-- ECommerce.IntegrationTests

## Main Modules

### Authentication

Provides:

- User registration
- Login
- JWT access tokens
- Refresh tokens
- Refresh token rotation
- Logout
- User profile
- Role-based authorization
- Password hashing
- Request validation

Roles currently include:

- Admin
- Customer

### Products

Provides:

- Get all products
- Get product by ID
- Create product
- Update product
- Delete product
- Soft deletion
- Duplicate-name protection
- Stock management

Administrative product operations require the Admin role.

### Cart

Provides:

- Get current user's cart
- Automatic cart creation
- Add products
- Product quantities
- Increase quantity for existing products
- Remove products
- Clear cart

### Orders

Provides:

- Checkout
- Order creation
- Order items
- Order totals
- Stock validation
- Inventory reduction
- Cart item removal after checkout
- Get current user's orders
- Get order by ID
- Database transactions

Checkout creates the initial tracking status automatically.

### Tracking

Provides order tracking through tracking events.

Supported statuses:

Processing
Shipped
Delayed
OutForDelivery
Delivered

The normal progression is:

Processing
↓
Shipped
↓
OutForDelivery
↓
Delivered

Delayed can occur during the shipping and delivery process according to the implemented transition rules.

Delivered is a terminal status.

## Database

The project uses:

- Entity Framework Core
- SQL Server
- Code First migrations

A shared AppDbContext is used for the application's modules.

Database migrations are stored in:

src/ECommerce.Database/Migrations

## Technologies

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Authentication
- BCrypt password hashing
- FluentValidation
- AutoMapper
- Repository Pattern
- Dependency Injection
- Options Pattern
- Scalar / OpenAPI
- xUnit
- Moq
- FluentAssertions

## Authentication

The API uses JWT Bearer authentication.

Access tokens are generated after successful authentication and contain information used for identifying the authenticated user and their role.

Refresh tokens are stored as hashes rather than storing the raw token.

Role-based authorization is used for protected administrative endpoints.

Example:

[Authorize(Roles = "Admin")]

## Error Handling

The project uses global exception handling with ProblemDetails responses.

Common HTTP responses used by the API include:

200 OK
201 Created
204 No Content
400 Bad Request
401 Unauthorized
404 Not Found
409 Conflict

## Validation

FluentValidation is used to validate incoming requests.

Validation is performed before business logic is executed.

## Mapping

AutoMapper is used where appropriate to map between entities and DTOs.

DTOs are used to control the data exposed by API endpoints rather than exposing database entities directly.

## Testing

The solution contains separate unit and integration test projects.

### Unit Tests

Unit tests isolate service logic by mocking dependencies.

Tools:

- xUnit
- Moq
- FluentAssertions

Current unit-test suite:

82 tests

### Integration Tests

Integration tests exercise the actual API pipeline, including controllers, services, repositories, EF Core, authentication, and a separate test database.

The integration tests cover:

- Authentication
- Products
- Cart
- Orders
- Tracking

Current integration-test suite:

22 tests

## Checkout Flow

A successful checkout follows this process:

Customer
↓
Cart
↓
Validate products
↓
Validate quantities
↓
Validate stock
↓
Calculate total
↓
Create Order
↓
Create Order Products
↓
Reduce Stock
↓
Remove purchased Cart Products
↓
Save Changes
↓
Create Processing Tracking Event
↓
Commit Transaction

## Running the Project

1. Restore NuGet packages.

2. Make sure SQL Server LocalDB is available.

3. Update the connection string if necessary.

4. Apply EF Core migrations.

5. Build the solution.

6. Run the required API project(s).

7. Use Scalar/OpenAPI or a tool such as Postman to test the endpoints.

## Running Tests

In Visual Studio:

Test
→ Test Explorer
→ Run All Tests

The unit and integration tests are separated into:

ECommerce.UnitTests
ECommerce.IntegrationTests

### Test Summary

Unit Tests: 60
Integration Tests: 22
Total: 82

## Git

The project is maintained using Git and hosted on GitHub.

The main branch contains the current project state.