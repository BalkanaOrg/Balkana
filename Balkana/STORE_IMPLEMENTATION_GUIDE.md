# Balkana Store Implementation Guide

## 🎯 Overview

Complete e-commerce solution for Balkana with:
- **Product Management** with variants (size, color, etc.)
- **Collections** (e.g., "Balkana 2025 Winter Collection")
- **Guest & Registered User Checkout**
- **Bulgarian Delivery Integration** (Ekont & Speedy)
- **Inventory Management**
- **Order Processing**

## 📦 Database Schema

### Products & Variants
- **Product** - Base product (e.g., "Balkana Logo T-Shirt")
- **ProductVariant** - Specific size/color combinations with individual SKUs and pricing
- **ProductCategory** - Hierarchical categories (Clothing → T-Shirts)
- **ProductImage** - Product gallery images
- **Collection** - Seasonal collections
- **ProductCollection** - Many-to-many link

### Orders & Cart
- **Order** - Customer orders (supports both registered users and guests)
- **OrderItem** - Individual items in an order
- **ShoppingCart** - Persistent cart for registered users
- **ShoppingCartItem** - Items in cart
- **InventoryLog** - Stock movement tracking

### Key Features

#### Product Variants
Products can have multiple variants:
```csharp
Product: "Balkana T-Shirt" (BasePrice: 29.99 BGN)
├─ Variant: "Small / Black" (SKU: BALKANA-TSH-S-BLK, Price: 29.99 BGN, Stock: 50)
├─ Variant: "Small / White" (SKU: BALKANA-TSH-S-WHT, Price: 29.99 BGN, Stock: 30)
├─ Variant: "Medium / Black" (SKU: BALKANA-TSH-M-BLK, Price: 29.99 BGN, Stock: 75)
└─ Variant: "Large / Black" (SKU: BALKANA-TSH-L-BLK, Price: 34.99 BGN, Stock: 40)
```

#### Collections
```csharp
Collection: "Balkana 2025 Winter Collection"
├─ Banner Image
├─ Season: "Winter 2025"
├─ Start/End Dates
└─ Products: [T-Shirt, Hoodie, Mug, Stickers]
```

#### Guest vs Registered Checkout
- **Registered Users**: 
  - Persistent shopping cart
  - Order history
  - Saved addresses
  - Faster checkout
  
- **Guest Users**:
  - Session-based cart
  - Must provide full details at checkout
  - Optional account creation after order

## 🚚 Delivery Integration (Bulgaria)

### Supported Providers
1. **Ekont** - Bulgarian courier service
   - Office pickup
   - Home delivery
   
2. **Speedy** - Bulgarian courier service
   - Office pickup
   - Home delivery
   
3. **Courier to Address** - Direct delivery
4. **Office Pickup** - Collect from Balkana office
5. **International Shipping** - For non-Bulgarian orders

### Order Flow
```
1. Customer adds items to cart
2. Proceeds to checkout
3. Selects delivery method:
   - Ekont Office → Customer selects office from list
   - Ekont Courier → Enters home address
   - Speedy Office → Customer selects office from list
   - Speedy Courier → Enters home address
4. Selects payment method
5. Confirms order
6. Admin processes & ships
7. Tracking number added
8. Customer receives notification
```

## 💳 Payment Methods

### Supported Methods
1. **Cash on Delivery** (COD) - Most common in Bulgaria
2. **Bank Transfer** - Direct bank payment
3. **ePay** - Bulgarian payment gateway (integration required)
4. **Stripe** - International cards
5. **PayPal** - International payments
6. **Card** - Direct card payment

### Payment Flow
```
Order Status: Pending
    ↓
Payment Status: Pending
    ↓
[Customer Pays]
    ↓
Payment Status: Paid
    ↓
Order Status: Confirmed → Processing → Shipped → Delivered
```

## 📊 Inventory Management

### Stock Tracking
- Real-time stock levels per variant
- Low stock warnings (threshold: 10 by default)
- Inventory logs for all changes
- Reserved stock for pending orders

### Inventory Events
- **InitialStock** - First stock entry
- **Restock** - New stock added
- **Sale** - Product sold
- **Return** - Customer return
- **Damaged** - Damaged goods
- **Adjustment** - Manual correction
- **Reserved** - Pending order
- **Released** - Order cancelled

## 🔧 Setup Instructions

### 1. Run Migration
```bash
cd Balkana
dotnet ef migrations add AddStoreModels
dotnet ef database update
```

### 2. Seed Initial Data
Create categories, set up delivery zones, configure payment methods.

### 3. Add Products
- Create product categories
- Add products with base info
- Create variants for each size/color combination
- Upload product images
- Set stock levels

### 4. Configure Delivery
- Set up Ekont office list
- Set up Speedy office list
- Configure delivery prices per zone
- Set weight-based shipping rates

### 5. Configure Payments
- Set up ePay credentials (if using)
- Configure Stripe keys
- Enable/disable payment methods
- Set COD availability

## 🛒 Customer Journey

### Browse Products
```
/store → All products
/store/category/t-shirts → Category view
/store/collections/winter-2025 → Collection view
/store/product/balkana-logo-tshirt → Product details
```

### Add to Cart
- Select variant (size, color)
- Choose quantity
- Add to cart
- Continue shopping or checkout

### Checkout
- Review cart
- Login or continue as guest
- Enter shipping address
- Select delivery method
- Select payment method
- Review order
- Confirm

### After Order
- Order confirmation email
- Order tracking page
- Status updates
- Delivery tracking (when shipped)

## 👨‍💼 Admin Management

### Product Management
- `/admin/store/products` - List all products
- `/admin/store/products/create` - Add new product
- `/admin/store/products/{id}/edit` - Edit product
- `/admin/store/products/{id}/variants` - Manage variants
- `/admin/store/products/{id}/stock` - Update inventory

### Order Management
- `/admin/store/orders` - List all orders
- `/admin/store/orders/{id}` - Order details
- `/admin/store/orders/{id}/process` - Process order
- `/admin/store/orders/{id}/ship` - Mark as shipped
- `/admin/store/orders/{id}/cancel` - Cancel order

### Collection Management
- `/admin/store/collections` - List collections
- `/admin/store/collections/create` - Create collection
- `/admin/store/collections/{id}/products` - Manage products in collection

### Inventory Management
- `/admin/store/inventory` - Stock overview
- `/admin/store/inventory/low-stock` - Low stock alerts
- `/admin/store/inventory/logs` - Inventory change history

## 📈 Reports & Analytics

### Sales Reports
- Total revenue
- Products sold
- Best-selling products
- Revenue by category
- Revenue by collection

### Inventory Reports
- Current stock levels
- Stock value
- Low stock items
- Out of stock items

### Order Reports
- Orders by status
- Orders by payment method
- Orders by delivery provider
- Average order value
- Order completion rate

## 🔐 Security Considerations

1. **Guest Checkout**: Store minimal info, offer account creation
2. **Payment Security**: Never store full card details
3. **Order Verification**: Email confirmation for all orders
4. **Inventory Protection**: Prevent overselling with stock checks
5. **Admin Only**: Restrict management to Administrator role

## 🎨 Team/Player Merchandise

Products can be linked to specific teams or players:
```csharp
Product: "Team Diamond Jersey"
├─ TeamId: 1 (Team Diamond)
├─ Variants: S, M, L, XL
└─ Collection: "Team Jerseys 2025"

Product: "ext1nct Signature Mouse Pad"
├─ PlayerId: 42 (ext1nct)
├─ Variants: Standard, XL
└─ Collection: "Player Gear"
```

## 🚀 Future Enhancements

1. **Discount Codes** - Coupon system
2. **Loyalty Program** - Points for purchases
3. **Product Reviews** - Customer ratings
4. **Wishlist** - Save for later
5. **Size Guide** - Help customers choose sizes
6. **Bundle Deals** - Buy multiple, save more
7. **Pre-orders** - Coming soon products
8. **Gift Cards** - Store credit

## 📞 Customer Support

### Order Issues
- **Wrong Item**: Initiate return/exchange
- **Damaged**: Photo required, full refund
- **Not Delivered**: Check tracking, contact courier
- **Want to Cancel**: Before shipping - easy cancel

### Return Policy
- 14 days return window
- Original condition required
- Refund or store credit
- Customer pays return shipping (unless defective)

## 🇧🇬 Bulgarian-Specific Features

1. **BGN Currency** - Primary currency
2. **Ekont/Speedy Integration** - Most popular Bulgarian couriers
3. **COD Preferred** - Cash on Delivery is very popular in Bulgaria
4. **Office Pickup** - Many Bulgarians prefer office pickup
5. **Bulgarian VAT** - 20% VAT included in prices
6. **Bulgarian Phone Format** - (+359) validation

## 📱 Mobile Considerations

- Responsive design for all pages
- Touch-friendly product galleries
- Easy checkout on mobile
- SMS notifications (optional)
- Mobile-optimized images

## 🧪 Testing Checklist

- [ ] Add product to cart
- [ ] Update cart quantities
- [ ] Remove from cart
- [ ] Checkout as guest
- [ ] Checkout as registered user
- [ ] Apply delivery method
- [ ] Complete payment
- [ ] Receive confirmation email
- [ ] Admin: View order
- [ ] Admin: Process order
- [ ] Admin: Update inventory
- [ ] Test low stock warnings
- [ ] Test out-of-stock prevention

---

**Ready to launch the Balkana Store!** 🎉

