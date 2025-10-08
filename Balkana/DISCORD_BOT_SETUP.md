# Discord Bot Integration Setup Guide

## 🚀 Complete Discord Bot Integration

Your Discord bot integration is now ready! Here's everything you need to know to get it working.

## 📋 Prerequisites

1. **Discord Application**: You need a Discord application created in the [Discord Developer Portal](https://discord.com/developers/applications)
2. **Bot Token**: Your bot's authentication token
3. **Client ID**: Your application's client ID
4. **Client Secret**: Your application's client secret

## ⚙️ Configuration Steps

### 1. Update appsettings.json

Replace the placeholder values in your `appsettings.json`:

```json
{
  "Discord": {
    "BotToken": "YOUR_ACTUAL_BOT_TOKEN_HERE",
    "ClientId": "YOUR_ACTUAL_CLIENT_ID_HERE", 
    "ClientSecret": "YOUR_ACTUAL_CLIENT_SECRET_HERE"
  }
}
```

### 2. Access Bot Management

Navigate to: `https://localhost:7241/Admin/Discord/BotManagement`

## 🔧 Discord Developer Portal Setup

### 1. Create Discord Application
1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Click "New Application"
3. Give it a name (e.g., "Balkana Bot")
4. Click "Create"

### 2. Configure Bot
1. Go to the "Bot" section in your application
2. Click "Add Bot" if not already added
3. Copy the **Bot Token** (this is your `BotToken`)
4. Copy the **Application ID** (this is your `ClientId`)
5. Go to "General Information" and copy the **Client Secret**

### 3. Set Bot Permissions
1. Go to "OAuth2" → "URL Generator"
2. Select scopes: `bot`, `applications.commands`
3. Select bot permissions: `Send Messages`, `Use Slash Commands`
4. Copy the generated URL and use it to invite your bot to your server

### 4. Configure Interactions Endpoint
1. Go to "General Information"
2. Scroll down to "Interactions Endpoint URL"
3. Enter: `https://your-domain.com/api/discord/interactions`
   - For local development: `https://localhost:7241/api/discord/interactions`
4. Click "Save Changes"
5. Discord will verify the endpoint with a test request

## 🎯 Available Commands

### `/team <team_tag>`
- **Description**: Get active players for a team
- **Example**: `/team TDI`
- **Response**: Shows team name, tag, and list of active players with positions

### `/player <nickname>`
- **Description**: Get basic information for a player  
- **Example**: `/player ext1nct`
- **Response**: Shows player nickname, full name, nationality, current team, and game profiles

## 🛠️ Management Interface

### Bot Management Dashboard
- **URL**: `/Admin/Discord/BotManagement`
- **Features**:
  - View bot configuration status
  - Register slash commands to Discord
  - Copy configuration values
  - Test command functionality

### Command Testing
- **URL**: `/Admin/Discord/TestCommand`
- **Features**:
  - Test commands before deploying
  - View JSON responses
  - Debug command functionality

### Webhook Information
- **URL**: `/Admin/Discord/WebhookInfo`
- **Features**:
  - View webhook URL for Discord configuration
  - Security information
  - Integration instructions

## 🔄 Deployment Process

### 1. Register Commands
1. Go to Bot Management dashboard
2. Click "Register Slash Commands"
3. Wait for success confirmation
4. Commands will appear in Discord after a few minutes

### 2. Test Commands
1. Go to your Discord server
2. Type `/team` or `/player` to see autocomplete
3. Test with real team tags and player nicknames

### 3. Verify Webhook
1. Check Discord Developer Portal for webhook verification status
2. Ensure your server is accessible from Discord's servers

## 🔒 Security Notes

- **Bot Token**: Keep this secret! Never commit it to version control
- **Webhook Verification**: Currently disabled for development. Enable in production
- **Rate Limiting**: Discord has rate limits. The bot handles this automatically

## 🐛 Troubleshooting

### Commands Not Appearing
- Wait 5-10 minutes after registration
- Check bot permissions in Discord server
- Verify bot is invited with correct permissions

### Webhook Verification Failed
- Ensure your server is publicly accessible
- Check firewall settings
- Verify SSL certificate is valid

### Commands Not Working
- Check bot token is correct
- Verify database has team/player data
- Check application logs for errors

## 📊 Database Integration

The bot queries your existing database tables:
- **Teams**: `Teams` table for team information
- **Players**: `Players` table for player information  
- **Transfers**: `PlayerTeamTransfers` table for active rosters
- **Positions**: `TeamPositions` table for player roles
- **Nationalities**: `Nationalities` table for player countries
- **Game Profiles**: `GameProfiles` table for linked accounts

## 🚀 Next Steps

1. **Configure your bot credentials** in `appsettings.json`
2. **Register slash commands** via the management interface
3. **Invite bot to your Discord server** using the OAuth2 URL
4. **Test commands** with real data
5. **Deploy to production** and update webhook URL

Your Discord bot is now fully integrated and ready to provide tournament data directly to Discord users! 🎉
