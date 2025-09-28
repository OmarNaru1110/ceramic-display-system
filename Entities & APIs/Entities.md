- user
  ```
  id,
  username,
  email,
  password_hash,
  role [admin, seller],
  created_date
- product
  ```
  id,
  quality_grade [A, B, C],
  name,
  category [floor, walls, bathroom, kitchen],
  type [ceramic, porcelain],
  quantity,
  size,
  pieces_per_box,
  price_per_sqm,
  discount_id,
  created_date,
  images
- discount
  ```
  id,
  percentage,
  days_required,
  created_date,
  product_id
- size
  ```
  length,
  width
- cart
  ```
  id,
  seller_id,
  created_date
- cart_item
  ```
  id,
  cart_id,
  product_id,
  quantity,
  added_date
- order
  ```
  id,
  seller_id,
  customer_name,
  customer_contact,
  customer_address,
  total_amount,
  status [pending, confirmed, rejected, completed],
  notes,
  order_date,
  updated_date
- order_item
  ```
  id,
  order_id,
  product_id,
  quantity,
  price_per_sqm,
  total_price
- return_request
  ```
  id,
  order_id,
  return_reason,
  description,
  status [pending, accepted, rejected],
  request_date,
  admin_notes
- return_item
  ```
  id,
  return_request_id,
  product_id,
  quantity,
  reason
- invoice
  ```
  id,
  order_id,
  subtotal,
  total_amount,
  invoice_date,
  pdf_url,