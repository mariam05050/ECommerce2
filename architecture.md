# ECommerce2 Architecture
## Overview

ECommerce2 is a modular e-commerce backend built with ASP.NET Core Web API and .NET 10.

The solution is organized into separate business modules:

- Authentication
- Product
- Cart
- Order
- Tracking

Each module follows a 3 tier architecture:

API
↓
Services
↓
Data
↓
SQL Server

## API Layer
The API layer handles HTTP communication between clients and the application.

Responsibilities include:
- Routing
- Controllers
- HTTP requests and responses
- Authentication
- Authorization
- API endpoint definitions
- OpenAPI / Scalar documentation

Examples:

Auth.API
Product.API
Cart.API
Order.API
Tracking.API

## Services Layer

The Services layer contains the application's business logic.

Responsibilities include:
- Business rules
- Validation
- DTOs
- Mapping
- Service interfaces and implementations

Examples:

Auth.Services
Product.Services
Cart.Services
Order.Services
Tracking.Services

## Data Layer

The Data layer handles database access.

Responsibilities include:
- Entities
- Repository interfaces
- Repository implementations
- EF Core configurations
- Database context interfaces

Examples:

Auth.Data
Product.Data
Cart.Data
Order.Data
Tracking.Data

## Database
The application uses:

- Entity Framework Core
- SQL Server
- Code First migrations

A shared AppDbContext is used by the modules through their database-context interfaces.

The database project contains the EF Core migrations:

ECommerce.Database
└── Migrations

## Dependency Injection

ASP.NET Core built-in Dependency Injection is used to register services and repositories.
This allows controllers and other services to depend on abstractions instead of directly creating their dependencies.

## Repository Pattern

Repositories provide a layer between the Services layer and Entity Framework Core.

Example:

IProductRepository
        ↓
ProductRepository
        ↓
AppDbContext
        ↓
SQL Server

This keeps database access separate from business logic.

## Authentication and Authorization

Authentication uses JWT Bearer tokens.

The authentication module provides:

- Registration
- Login
- Access tokens
- Refresh tokens
- Refresh token rotation
- Logout
- User profiles

Authorization is role-based.

The current roles are:

- Admin
- Customer

Administrative endpoints use role-based authorization.

Example:

[Authorize(Roles = "Admin")]

## Validation

FluentValidation is used to validate incoming requests before business logic is executed.

Examples include:

- Registration validation
- Login validation
- Product validation
- Cart validation

## DTOs and Mapping

DTOs are used to control the data exposed by API endpoints.

AutoMapper is used for mapping between entities and DTOs where appropriate.

This prevents database entities from being exposed directly through the API.

## Product Module

The Product module provides:

- Get all products
- Get product by ID
- Create product
- Update product
- Delete product
- Soft deletion
- Duplicate-name protection
- Stock management

Create, update, and delete operations require the Admin role.

## Cart Module

The Cart module provides:

- Get the current user's cart
- Automatic cart creation
- Add products
- Product quantities
- Increase quantity for existing products
- Remove products
- Clear cart

The cart belongs to a user and contains cart-product records.

## Order Module

The Order module provides:

- Checkout
- Order creation
- Order items
- Total calculation
- Stock validation
- Stock reduction
- Cart item removal
- Get current user's orders
- Get order by ID

Checkout is performed inside a database transaction.

## Tracking Module

The Tracking module stores the history of order status changes.

Supported statuses are:

Processing
Shipped
Delayed
OutForDelivery
Delivered

A new order begins with Processing.

Valid status transitions are controlled by the TrackingService.

Delivered is a terminal status.

## Checkout Flow

The checkout process is:

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

## Error Handling

Each API contains global exception handling.

Common exceptions are converted into HTTP responses.

Examples include:

ArgumentException → 400 Bad Request
UnauthorizedAccessException → 401 Unauthorized
KeyNotFoundException → 404 Not Found
InvalidOperationException → 409 Conflict

Unexpected exceptions return a server error response.

## API Documentation

Scalar and OpenAPI are used to document and explore the APIs.

Each API exposes its OpenAPI documentation through its own host.

## Testing

The solution contains two test projects:

ECommerce.UnitTests
ECommerce.IntegrationTests

Unit tests isolate business logic and use mocking where appropriate.

Integration tests execute the real API pipeline using a separate test database.

Current test results:

Unit Tests: 60
Integration Tests: 22
Total: 82

All current tests pass.

## Project Structure

ECommerce2
|
|-- docs
|   |-- architecture.md
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