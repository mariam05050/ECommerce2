# ECommerce2 API Documentation

## Authentication

### Register
POST /api/auth/register
Creates a new Customer account.
Request:
{
  "name": "Mariam",
  "email": "mariam@example.com",
  "password": "Password123!"
}
Response: 200 OK
Returns an access token, refresh token, user ID, and role.

### Login
POST /api/auth/login
Authenticates a user.
Request:
{
  "email": "mariam@example.com",
  "password": "Password123!"
}
Response: 200 OK

### Refresh Token
POST /api/auth/refresh
Generates a new access token and refresh token.
Request:
{
  "refreshToken": "refresh-token"
}
Response: 200 OK

### Logout
POST /api/auth/logout
Requires JWT authentication.
Revokes the supplied refresh token.
Response: 204 No Content

### Profile
GET /api/auth/profile
Requires JWT authentication.
Returns the current user's profile information.
Response: 200 OK

## Products

### Get All Products
GET /api/products
Returns all available products.
Response: 200 OK

### Get Product
GET /api/products/{id}
Returns a product by ID.
Response: 200 OK

### Create Product
POST /api/products
Requires Admin authorization.
Creates a new product.
Request:
{
  "name": "USB-C Cable",
  "description": "USB-C charging cable",
  "price": 10,
  "stock": 50
}
Response: 201 Created

### Update Product
PUT /api/products/{id}
Requires Admin authorization.
Updates an existing product.
Response: 200 OK

### Delete Product
DELETE /api/products/{id}
Requires Admin authorization.
Soft-deletes a product.
Response: 204 No Content

## Cart

All Cart endpoints require JWT authentication.

### Get Cart
GET /api/cart
Returns the current user's cart.
Response: 200 OK

### Add Product
POST /api/cart/products
Adds a product to the cart.
Request:
{
  "productId": "product-id",
  "quantity": 2
}
If the product already exists in the cart, its quantity is increased.
Response: 200 OK

### Remove Product
DELETE /api/cart/products/{productId}
Removes a product from the current user's cart.
Response: 204 No Content

### Clear Cart
DELETE /api/cart
Removes all products from the current user's cart.
Response: 204 No Content

## Orders

All Order endpoints require JWT authentication.

### Checkout
POST /api/orders/checkout
Creates an order from the current user's cart.
The checkout process:
1. Validates the cart.
2. Validates products and quantities.
3. Checks available stock.
4. Calculates the order total.
5. Creates the order.
6. Creates order items.
7. Reduces product stock.
8. Removes purchased products from the cart.
9. Creates the initial Processing tracking event.
10. Commits the database transaction.
Response: 201 Created

### Get My Orders
GET /api/orders
Returns the authenticated user's orders.
Response: 200 OK

### Get Order
GET /api/orders/{orderId}
Returns a specific order belonging to the authenticated user.
Response: 200 OK

## Tracking

All Tracking endpoints require JWT authentication.

### Get Tracking
GET /api/tracking/orders/{orderId}
Returns the tracking history for an order.
Response: 200 OK

### Update Status
POST /api/tracking/orders/{orderId}/status/{status}
Requires Admin authorization.
Updates the order's tracking status.
Response: 200 OK

Supported statuses:
Processing
Shipped
Delayed
OutForDelivery
Delivered

Invalid tracking statuses return:
400 Bad Request

Invalid status transitions return:
409 Conflict

Delivered is the terminal status.

## Authorization

JWT-protected requests use:
Authorization: Bearer {access-token}

Administrative endpoints require the Admin role.
Users can access resources belonging to their own account.

## HTTP Status Codes

200 OK - Request succeeded
201 Created - Resource created
204 No Content - Request succeeded with no content
400 Bad Request - Invalid request or validation error
401 Unauthorized - Authentication required or invalid
404 Not Found - Resource not found
409 Conflict - Conflict with the current application state

## API Documentation Tools

The APIs use OpenAPI and Scalar.
They provide interactive API documentation and endpoint testing.