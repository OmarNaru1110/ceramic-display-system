# Ceramic Display System APIs

## Authentication & User Management

### User Registration
```
POST /api/auth/register
{
    username: string,
    email: string,
    password: string,
    role: "admin" | "seller"
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
    pieces_per_box: int,
    price_per_sqm: float,
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
    pieces_per_box?: int,
    price_per_sqm?: float,
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

## Product Viewing (ALL)

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
    pieces_per_box: int,
    price_per_sqm: float,
    images: [string],
    discount: {
        id: int,
        percentage: float,
        days_required: int,
    },
    created_date: datetime
}
```

### Get Products with Filters and Search
```
GET /api/products?category={category}&type={type}&quality_grade={grade}&price_min={min}&price_max={max}&sort_by={field}&sort_order={asc|desc}&page={page}&limit={limit}

Query Parameters:
- query: string (search in product name)
- category: "floor" | "walls" | "bathroom" | "kitchen"
- type: "ceramic" | "porcelain"
- quality_grade: "A" | "B" | "C"
- price_min: float
- price_max: float
- sort_by: "name" | "price_per_sqm" | "quality_grade" | "created_date"
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
        pieces_per_box: int,
        price_per_sqm: float,
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
    discount: {
        id: int,
        percentage: float,
        days_required: int,
        created_date: datetime
    }
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

## Order Management (Admin Only)

### Get All Orders
```
GET /api/admin/orders?status={status}&page={page}&limit={limit}
Headers: Authorization: Bearer {admin_token}

Query Parameters:
- status: "pending" | "confirmed" | "rejected" | "completed"
- page: int (default: 1)
- limit: int (default: 20)

Response: {
    orders: [{
        order_id: int,
        seller_id: int,
        seller_name: string,
        customer_name: string,
        customer_contact: string,
        items: [{
            product_id: int,
            product_name: string,
            quantity: int,
            price_per_sqm: float,
            total_price: float
        }],
        total_amount: float,
        status: string,
        order_date: datetime,
        updated_at: datetime
    }],
    pagination: {
        current_page: int,
        total_pages: int,
        total_orders: int
    }
}
```

### Get Single Order Details
```
GET /api/admin/orders/{order_id}
Headers: Authorization: Bearer {admin_token}
Response: {
    order_id: int,
    seller_id: int,
    seller_name: string,
    customer_name: string,
    customer_contact: string,
    customer_address: string,
    items: [{
        product_id: int,
        product_name: string,
        category: string,
        quantity: int,
        price_per_sqm: float,
        total_price: float,
        product_images: [string]
    }],
    total_amount: float,
    status: string,
    order_date: datetime,
    notes: string
}
```

### Confirm Order
```
PUT /api/admin/orders/{order_id}/confirm
Headers: Authorization: Bearer {admin_token}
{
    notes?: string
}
Response: {
    order_id: int,
    status: "confirmed",
    invoice_id: int,
    message: string
}
```

### Reject Order
```
PUT /api/admin/orders/{order_id}/reject
Headers: Authorization: Bearer {admin_token}
{
    rejection_reason: string
}
Response: {
    order_id: int,
    status: "rejected",
    message: string
}
```

## Cart Management (Seller Only)

### Get Cart
```
GET /api/seller/cart
Headers: Authorization: Bearer {seller_token}
Response: {
    cart_id: int,
    items: [{
        cart_item_id: int,
        product_id: int,
        product_name: string,
        category: string,
        price_per_sqm: float,
        quantity: int,
        total_price: float,
        product_images: [string]
    }],
    total_amount: float,
    item_count: int
}
```

### Add Item to Cart
```
POST /api/seller/cart/items
Headers: Authorization: Bearer {seller_token}
{
    product_id: int,
    quantity: int
}
Response: {
    cart_item_id: int,
    message: string
}
```

### Update Cart Item
```
PUT /api/seller/cart/items/{cart_item_id}
Headers: Authorization: Bearer {seller_token}
{
    quantity: int
}
Response: {
    cart_item_id: int,
    message: string
}
```

### Remove Item from Cart
```
DELETE /api/seller/cart/items/{cart_item_id}
Headers: Authorization: Bearer {seller_token}
Response: {
    message: string
}
```

### Clear Cart //remove all cart items from cart
```
DELETE /api/seller/cart
Headers: Authorization: Bearer {seller_token}
Response: {
    message: string
}
```

## Order Management (Seller Only)

### Create Order from Cart
```
POST /api/seller/orders
Headers: Authorization: Bearer {seller_token}
{
    customer_name: string,
    customer_contact: string,
    customer_address: string,
    notes?: string
}
Response: {
    order_id: int,
    total_amount: float,
    status: "pending",
    message: string
}
```

### Get Seller Orders
```
GET /api/seller/orders?status={status}&page={page}&limit={limit}
Headers: Authorization: Bearer {seller_token}

Query Parameters:
- status: "pending" | "confirmed" | "rejected" | "completed"
- page: int (default: 1)
- limit: int (default: 20)

Response: {
    orders: [{
        order_id: int,
        customer_name: string,
        total_amount: float,
        status: string,
        order_date: datetime,
        item_count: int
    }],
    pagination: {
        current_page: int,
        total_pages: int,
        total_orders: int
    }
}
```

### Update Order (Before Confirmation)
```
PUT /api/seller/orders/{order_id}
Headers: Authorization: Bearer {seller_token}
{
    customer_name?: string,
    customer_contact?: string,
    customer_address?: string,
    notes?: string
}
Response: {
    order_id: int,
    message: string
}
```

### Cancel Order
```
DELETE /api/seller/orders/{order_id}
Headers: Authorization: Bearer {seller_token}
Response: {
    message: string
}
```

## Return Management

### Submit Return Request (Seller)
```
POST /api/seller/returns
Headers: Authorization: Bearer {seller_token}
{
    order_id: int,
    items: [{
        product_id: int,
        quantity: int,
        reason: string
    }],
    return_reason: string,
    description?: string
}
Response: {
    return_id: int,
    status: "pending",
    message: string
}
```

### Get Return Requests (Admin)
```
GET /api/admin/returns?status={status}&page={page}&limit={limit}
Headers: Authorization: Bearer {admin_token}

Response: {
    returns: [{
        return_id: int,
        order_id: int,
        seller_name: string,
        customer_name: string,
        items: [{
            product_name: string,
            quantity: int,
            reason: string
        }],
        return_reason: string,
        status: string,
        request_date: datetime
    }]
}
```

### Accept Return Request (Admin)
```
PUT /api/admin/returns/{return_id}/accept
Headers: Authorization: Bearer {admin_token}
{
    admin_notes?: string
}
Response: {
    return_id: int,
    status: "accepted",
    return_paper_url: string,
    message: string
}
```

### Reject Return Request (Admin)
```
PUT /api/admin/returns/{return_id}/reject
Headers: Authorization: Bearer {admin_token}
{
    rejection_reason: string
}
Response: {
    return_id: int,
    status: "rejected",
    message: string
}
```

## Invoice Management

### Get Invoice
```
GET /api/invoices/{invoice_id}
Headers: Authorization: Bearer {token}
Response: {
    invoice_id: int,
    order_id: int,
    invoice_number: string,
    customer_name: string,
    customer_address: string,
    items: [{
        product_name: string,
        quantity: int,
        price_per_sqm: float,
        total_price: float
    }],
    subtotal: float,
    tax_amount: float,
    total_amount: float,
    invoice_date: datetime,
    pdf_url: string,
    status: "generated" | "printed"
}
```

### Download Invoice PDF
```
GET /api/invoices/{invoice_id}/pdf
Headers: Authorization: Bearer {token}
Response: PDF file download
```

### Print Invoice (Admin)
```
POST /api/admin/invoices/{invoice_id}/print
Headers: Authorization: Bearer {admin_token}
Response: {
    message: string,
    print_status: "sent_to_printer"
}
```
## Dashboard APIs (Admin Only)

### Get Admin Dashboard Stats
```
GET /api/admin/dashboard
Headers: Authorization: Bearer {admin_token}
Response: {
    total_products: int,
    orders: {
        floor: int,
        walls: int,
        bathroom: int,
        kitchen: int
    },
    invoices: {
        ceramic: int,
        porcelain: int
    },
    returned_items: [{
        product_id: int,
        name: string,
        quantity: int
    }]
}
```