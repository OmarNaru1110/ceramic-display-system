# Ceramic Display System APIs

## Authentication & User Management

### User Registration
```
POST /api/auth/register
{
    username: string,
    email: string,
    password: string,
    role: "admin"
}
Response: {
    user_id: int,
    token: string,
    role: string
}
```

### User Login
```
POST /api/auth/login
{
    email: string,
    password: string
}
Response: {
    user_id: int,
    token: string,
    role: string
}
```

### User Logout
```
POST /api/auth/logout
Headers: Authorization: Bearer {token}
```

## Product Management (Admin Only)

### Create Product
```
POST /api/products
Headers: Authorization: Bearer {admin_token}
{
    name: string,
    quality_grade: "A" | "B" | "C",
    category: "floor" | "walls" | "bathroom" | "kitchen",
    type: "ceramic" | "porcelain",
    quantity: int,
    size: {
        length: float,
        width: float
    },
    pei: "PEI1" | "PEI2" | "PEI3" | "PEI4" | "PEI5",
    srt: float,
    thickness: float,
    price_per_sqm: float,
    surface_finish_id: int,
    images: [string] // URLs or base64
}
Response: {
    product_id: int,
    message: string
}
```

### Update Product
```
PUT /api/products/{product_id}
Headers: Authorization: Bearer {admin_token}
{
    name?: string,
    quality_grade?: "A" | "B" | "C",
    category?: "floor" | "walls" | "bathroom" | "kitchen",
    type?: "ceramic" | "porcelain",
    quantity?: int,
    size?: {
        length: float,
        width: float
    },
    pei?: "PEI1" | "PEI2" | "PEI3" | "PEI4" | "PEI5",
    srt?: float,
    thickness?: float,
    price_per_sqm?: float,
    surface_finish_id?: int,
    images?: [string]
}
Response: {
    product_id: int,
    message: string
}
```

### Delete Product
```
DELETE /api/products/{product_id}
Headers: Authorization: Bearer {admin_token}
Response: {
    message: string
}
```

## Product Viewing (Customer & Admin)

### Get Single Product Details
```
GET /api/products/{product_id}
Response: {
    product_id: int,
    name: string,
    quality_grade: string,
    category: string,
    type: string,
    quantity: int,
    size: {
        length: float,
        width: float
    },
    pei: string,
    srt: float,
    thickness: float,
    price_per_sqm: float,
    surface_finish: {
        id: int,
        name: string
    },
    images: [string],
    discounts: [{
        id: int,
        percentage: float,
        days_required: int
    }],
    created_at: datetime,
    updated_at: datetime
}
```

### Get Products with Filters and Search
```
GET /api/products?query={search_term}&category={category}&type={type}&quality_grade={grade}&price_min={min}&price_max={max}&pei={pei}&sort_by={field}&sort_order={asc|desc}&page={page}&limit={limit}

Query Parameters:
- query: string (search in product name)
- category: "floor" | "walls" | "bathroom" | "kitchen"
- type: "ceramic" | "porcelain"
- quality_grade: "A" | "B" | "C"
- price_min: float
- price_max: float
- pei: "PEI1" | "PEI2" | "PEI3" | "PEI4" | "PEI5"
- sort_by: "name" | "price_per_sqm" | "quality_grade" | "created_at"
- sort_order: "asc" | "desc"
- page: int (default: 1)
- limit: int (default: 20)

Response: {
    products: [{
        product_id: int,
        name: string,
        quality_grade: string,
        category: string,
        type: string,
        quantity: int,
        price_per_sqm: float,
        surface_finish: string,
        images: [string],
        has_discount: boolean
    }],
    pagination: {
        current_page: int,
        total_pages: int,
        total_products: int,
        has_next: boolean,
        has_previous: boolean
    }
}
```

## Surface Finish Management (Admin Only)

### Create Surface Finish
```
POST /api/surface-finishes
Headers: Authorization: Bearer {admin_token}
{
    name: string
}
Response: {
    surface_finish_id: int,
    message: string
}
```

### Get All Surface Finishes
```
GET /api/surface-finishes
Response: {
    surface_finishes: [{
        id: int,
        name: string
    }]
}
```

### Update Surface Finish
```
PUT /api/surface-finishes/{surface_finish_id}
Headers: Authorization: Bearer {admin_token}
{
    name: string
}
Response: {
    message: string
}
```

### Delete Surface Finish
```
DELETE /api/surface-finishes/{surface_finish_id}
Headers: Authorization: Bearer {admin_token}
Response: {
    message: string
}
```

## Discount Management (Admin Only)

### Create Discount
```
POST /api/discounts
Headers: Authorization: Bearer {admin_token}
{
    product_id: int,
    percentage: float,
    days_required: int
}
Response: {
    discount_id: int,
    message: string
}
```

### Get Product Discounts
```
GET /api/products/{product_id}/discounts
Response: {
    discounts: [{
        id: int,
        percentage: float,
        days_required: int,
        created_at: datetime
    }]
}
```

### Update Discount
```
PUT /api/discounts/{discount_id}
Headers: Authorization: Bearer {admin_token}
{
    percentage?: float,
    days_required?: int
}
Response: {
    message: string
}
```

### Delete Discount
```
DELETE /api/discounts/{discount_id}
Headers: Authorization: Bearer {admin_token}
Response: {
    message: string
}
```

## Dashboard APIs (Admin Only)

### Get Admin Dashboard Stats
```
GET /api/admin/dashboard
Headers: Authorization: Bearer {admin_token}
Response: {
    total_products: int,
    products_by_category: {
        floor: int,
        walls: int,
        bathroom: int,
        kitchen: int
    },
    products_by_type: {
        ceramic: int,
        porcelain: int
    },
    low_stock_products: [{
        product_id: int,
        name: string,
        quantity: int
    }],
    recent_products: [{
        product_id: int,
        name: string,
        created_at: datetime
    }]
}
```

## Error Responses

All APIs may return the following error responses:

```
400 Bad Request
{
    error: "Bad Request",
    message: "Invalid input data",
    details: [string]
}

401 Unauthorized
{
    error: "Unauthorized",
    message: "Authentication required"
}

403 Forbidden
{
    error: "Forbidden",
    message: "Insufficient permissions"
}

404 Not Found
{
    error: "Not Found",
    message: "Resource not found"
}

500 Internal Server Error
{
    error: "Internal Server Error",
    message: "Something went wrong"
}
```