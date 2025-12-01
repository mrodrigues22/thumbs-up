# Paddle Configuration Guide

This document explains how to configure Paddle Billing for the Thumbs Up application.

## 1. Create a Paddle Account

1. Go to https://www.paddle.com/
2. Sign up for a Paddle account
3. Complete account verification
4. You'll start in **Sandbox mode** for testing

## 2. Get Your API Key

### Sandbox (for testing)
1. Log into Paddle dashboard: https://sandbox-vendors.paddle.com/
2. Navigate to **Developer Tools** → **Authentication**
3. Click **Create API Key**
4. Give it a name (e.g., "ThumbsUp Sandbox")
5. Copy the API key (starts with `pdl_sdbx_`)
6. Paste it in `appsettings.json` → `Paddle.ApiKey`

### Production (when ready to go live)
1. Log into production dashboard: https://vendors.paddle.com/
2. Navigate to **Developer Tools** → **Authentication**
3. Create a new API key
4. Copy the production API key (starts with `pdl_live_`)
5. Paste it in production `appsettings.json` or environment variables

## 3. Create Products and Prices

### Create Products
1. In Paddle dashboard, go to **Catalog** → **Products**
2. Create three products:

#### Starter Plan
- **Name**: Thumbs Up Starter
- **Description**: Perfect for individual professionals
- **Type**: Subscription

#### Pro Plan
- **Name**: Thumbs Up Pro
- **Description**: For professionals managing client approvals
- **Type**: Subscription

#### Enterprise Plan
- **Name**: Thumbs Up Enterprise
- **Description**: For teams and agencies
- **Type**: Subscription

### Create Prices
For each product, create two prices (Monthly and Yearly):

#### Starter Prices
1. **Monthly Price**:
   - Amount: $12.00 USD
   - Billing cycle: Monthly
   - Copy the Price ID (starts with `pri_`)
   - Paste in `appsettings.json` → `Paddle.PriceIds.Starter_Monthly`

2. **Yearly Price**:
   - Amount: $120.00 USD ($10/month)
   - Billing cycle: Yearly
   - Copy the Price ID
   - Paste in `appsettings.json` → `Paddle.PriceIds.Starter_Yearly`

#### Pro Prices
1. **Monthly Price**:
   - Amount: $29.00 USD
   - Billing cycle: Monthly
   - Copy the Price ID
   - Paste in `appsettings.json` → `Paddle.PriceIds.Pro_Monthly`

2. **Yearly Price**:
   - Amount: $290.00 USD ($24.17/month)
   - Billing cycle: Yearly
   - Copy the Price ID
   - Paste in `appsettings.json` → `Paddle.PriceIds.Pro_Yearly`

#### Enterprise Prices
1. **Monthly Price**:
   - Amount: $99.00 USD
   - Billing cycle: Monthly
   - Copy the Price ID
   - Paste in `appsettings.json` → `Paddle.PriceIds.Enterprise_Monthly`

2. **Yearly Price**:
   - Amount: $990.00 USD ($82.50/month)
   - Billing cycle: Yearly
   - Copy the Price ID
   - Paste in `appsettings.json` → `Paddle.PriceIds.Enterprise_Yearly`

## 4. Set Default Payment Link Domain (REQUIRED)

⚠️ **CRITICAL STEP** - Paddle requires a default payment link domain to be configured:

1. In Paddle dashboard, go to **Checkout** → **Checkout settings**
2. Find **Default Payment Link** section
3. For **development/testing**, set the domain to: `http://localhost:5173`
   - Note: You may need to request approval for `localhost` domain
   - Click "Request website approval" if needed
4. For **production**, you'll need to:
   - Set your production domain (e.g., `https://yourdomain.com`)
   - Submit the domain for approval via "Request website approval"
   - Wait for Paddle to approve your domain
5. Click **Save**

> **Important**: The application will pass specific success/cancel URLs (e.g., `/subscription/success`, `/pricing`) when creating checkout sessions. Paddle just needs the base domain to be configured here.

## 5. Set Up Webhooks

Webhooks allow Paddle to notify your application when subscription events occur (e.g., payment successful, subscription cancelled).

### Create Webhook Endpoint
1. In Paddle dashboard, go to **Developer Tools** → **Notifications**
2. Click **New Notification Destination**
3. Enter your webhook URL:
   - **Development**: `https://your-ngrok-url.ngrok.io/api/paddle/webhook`
   - **Production**: `https://yourdomain.com/api/paddle/webhook`
4. Select these events:
   - ✅ `subscription.created`
   - ✅ `subscription.updated`
   - ✅ `subscription.activated`
   - ✅ `subscription.canceled`
   - ✅ `subscription.paused`
   - ✅ `subscription.resumed`
   - ✅ `transaction.completed`
   - ✅ `transaction.paid`
5. Copy the **Webhook Secret** (shown after creation)
6. Paste it in `appsettings.json` → `Paddle.WebhookSecret`

### For Local Development (using ngrok)
```bash
# Install ngrok: https://ngrok.com/download
# Start your API
cd ThumbsUpApi
dotnet run

# In another terminal, start ngrok
ngrok http 5000

# Copy the HTTPS URL (e.g., https://abc123.ngrok.io)
# Use this as your webhook URL: https://abc123.ngrok.io/api/paddle/webhook
```

## 6. Update Configuration Files

### appsettings.json (already updated)
```json
{
  "Paddle": {
    "ApiKey": "YOUR_PADDLE_API_KEY_HERE",
    "WebhookSecret": "YOUR_PADDLE_WEBHOOK_SECRET_HERE",
    "Environment": "sandbox",
    "PriceIds": {
      "Starter_Monthly": "pri_01xxx",
      "Starter_Yearly": "pri_01yyy",
      "Pro_Monthly": "pri_01aaa",
      "Pro_Yearly": "pri_01bbb",
      "Enterprise_Monthly": "pri_01ccc",
      "Enterprise_Yearly": "pri_01ddd"
    }
  }
}
```

### Production Environment Variables (Recommended)
For production, use environment variables instead of hardcoding:

```bash
# .env or hosting platform environment variables
PADDLE__APIKEY=pdl_live_xxx
PADDLE__WEBHOOKSECRET=pdl_ntfset_01xxx
PADDLE__ENVIRONMENT=production
PADDLE__PRICEIDS__STARTER_MONTHLY=pri_01xxx
PADDLE__PRICEIDS__STARTER_YEARLY=pri_01yyy
PADDLE__PRICEIDS__PRO_MONTHLY=pri_01aaa
PADDLE__PRICEIDS__PRO_YEARLY=pri_01bbb
PADDLE__PRICEIDS__ENTERPRISE_MONTHLY=pri_01ccc
PADDLE__PRICEIDS__ENTERPRISE_YEARLY=pri_01ddd
```

## 7. Testing Checkout Flow

### Sandbox Test Cards
Paddle provides test card numbers for sandbox testing:

**Successful Payment:**
- Card: `4242 4242 4242 4242`
- Expiry: Any future date
- CVV: Any 3 digits

**Declined Payment:**
- Card: `4000 0000 0000 0002`

### Test the Flow
1. Start your API: `dotnet run`
2. Start your frontend: `npm run dev`
3. Navigate to `/pricing`
4. Click "Get Started" on any plan
5. Complete checkout with test card
6. Verify webhook events in Paddle dashboard → **Developer Tools** → **Events**

## 8. Switch to Production

When ready to go live:

1. **Verify Business Details**
   - Complete all business information in Paddle
   - Add payment methods
   - Complete tax setup

2. **Create Production Products and Prices**
   - Recreate all products/prices in production environment
   - Get new production Price IDs

3. **Update Configuration**
   - Change `Paddle.Environment` to `"production"`
   - Use production API key (starts with `pdl_live_`)
   - Use production webhook secret
   - Update all Price IDs to production values

4. **Update Webhook URL**
   - Point to your production domain
   - Verify webhook endpoint is accessible

5. **Test Production Checkout**
   - Test with real card (will be charged)
   - Verify subscription creation
   - Test cancellation flow

## 9. Monitoring

### Paddle Dashboard
- Monitor subscriptions: **Subscriptions** → **All subscriptions**
- View transactions: **Transactions** → **All transactions**
- Check webhook events: **Developer Tools** → **Events**
- View revenue: **Reports** → **Revenue**

### Application Logs
- Check API logs for webhook processing
- Monitor subscription creation/updates
- Track payment failures

## Configuration Summary

**Files to Update:**
1. `ThumbsUpApi/appsettings.json` - Paddle configuration
2. Production environment variables (when deploying)

**Paddle Dashboard Setup:**
1. ✅ Create API key
2. ✅ Create 3 products (Starter, Pro, Enterprise)
3. ✅ Create 6 prices (2 per product: monthly + yearly)
4. ✅ **Set default checkout URLs (CRITICAL)**
5. ✅ Set up webhook endpoint
6. ✅ Copy all IDs to configuration

**Values Needed:**
- [ ] Paddle API Key → `Paddle.ApiKey`
- [ ] Webhook Secret → `Paddle.WebhookSecret`
- [ ] Starter Monthly Price ID → `Paddle.PriceIds.Starter_Monthly`
- [ ] Starter Yearly Price ID → `Paddle.PriceIds.Starter_Yearly`
- [ ] Pro Monthly Price ID → `Paddle.PriceIds.Pro_Monthly`
- [ ] Pro Yearly Price ID → `Paddle.PriceIds.Pro_Yearly`
- [ ] Enterprise Monthly Price ID → `Paddle.PriceIds.Enterprise_Monthly`
- [ ] Enterprise Yearly Price ID → `Paddle.PriceIds.Enterprise_Yearly`

## Troubleshooting

### Webhook Not Receiving Events
- Verify webhook URL is publicly accessible
- Check webhook secret matches configuration
- Review Paddle Events log for delivery failures
- For local dev, ensure ngrok is running

### Checkout Not Opening / 503 Service Unavailable Error
- ⚠️ **Most Common**: Check if Default Checkout URL is set in Paddle Dashboard (see step 4)
- Verify Price IDs are correct
- Check Paddle.js script is loaded in `index.html`
- Verify API key has correct permissions
- Check browser console for errors
- Review API logs for detailed Paddle error messages

### Subscription Not Created
- Check webhook endpoint logs
- Verify webhook events are subscribed
- Review Paddle Events in dashboard
- Check database for subscription records

## Support

- **Paddle Documentation**: https://developer.paddle.com/
- **Paddle Support**: https://www.paddle.com/support
- **API Reference**: https://developer.paddle.com/api-reference/overview
