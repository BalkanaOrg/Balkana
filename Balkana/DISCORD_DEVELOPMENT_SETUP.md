# Discord Bot Development Setup with ngrok

## 🚀 Quick Setup for Local Development

Since Discord can't reach `localhost` directly, we'll use ngrok to create a public tunnel.

### Step 1: Install ngrok
1. Go to [ngrok.com](https://ngrok.com) and sign up
2. Download ngrok for Windows
3. Extract and add to your PATH

### Step 2: Start ngrok tunnel
```bash
ngrok http 7241
```

### Step 3: Update Discord Webhook URL
1. Copy the HTTPS URL from ngrok (e.g., `https://abc123.ngrok.io`)
2. Go to Discord Developer Portal → Your Application → General Information
3. Set Interactions Endpoint URL to: `https://abc123.ngrok.io/api/discord/interactions`
4. Click "Save Changes"

### Step 4: Test the Integration
1. Go to `https://localhost:7241/Admin/Discord/BotManagement`
2. Click "Register Slash Commands"
3. Test commands in Discord

## 🔄 Alternative: Temporary Public Access

If you don't want to use ngrok, you can temporarily:
1. Deploy to a cloud service (Azure, AWS, Heroku)
2. Use a service like [localtunnel](https://localtunnel.github.io/www/)
3. Set up port forwarding on your router

## ⚠️ Important Notes

- **ngrok URLs change** each time you restart ngrok (unless you have a paid account)
- **Keep ngrok running** while testing Discord integration
- **Use HTTPS URLs only** - Discord requires secure connections
- **Update webhook URL** whenever ngrok URL changes
