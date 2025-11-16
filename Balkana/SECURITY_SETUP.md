# Security Setup Guide

## 🚨 CRITICAL: Your Credentials Have Been Exposed

Your Discord bot credentials, API keys, and database connection string have been exposed in your public GitHub repository. **You MUST take immediate action:**

1. **Regenerate ALL exposed credentials:**
   - Discord Bot Token (https://discord.com/developers/applications)
   - Discord Client Secret
   - Riot API Key
   - FaceIt API Key
   - Consider changing your Azure SQL password (already in git history)

2. **The old credentials should be considered compromised and disabled.**

---

## Secure Configuration Setup

This project now uses environment variables to manage sensitive configuration data securely.

### For Local Development

Use User Secrets (recommended for .NET):

```powershell
# Navigate to the Balkana directory
cd Balkana

# Initialize User Secrets
dotnet user-secrets init

# Add your secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING"
dotnet user-secrets set "Riot:ApiKey" "YOUR_RIOT_API_KEY"
dotnet user-secrets set "Faceit:ApiKey" "YOUR_FACEIT_API_KEY"
dotnet user-secrets set "Discord:BotToken" "YOUR_DISCORD_BOT_TOKEN"
dotnet user-secrets set "Discord:ClientId" "YOUR_DISCORD_CLIENT_ID"
dotnet user-secrets set "Discord:ClientSecret" "YOUR_DISCORD_CLIENT_SECRET"
dotnet user-secrets set "Discord:GuildId" "YOUR_DISCORD_GUILD_ID"
dotnet user-secrets set "BaseUrl" "YOUR_BASE_URL"
```

Or use environment variables manually:

```powershell
# Windows PowerShell
$env:ConnectionStrings__DefaultConnection = "YOUR_CONNECTION_STRING"
$env:Riot__ApiKey = "YOUR_RIOT_API_KEY"
$env:Faceit__ApiKey = "YOUR_FACEIT_API_KEY"
$env:Discord__BotToken = "YOUR_DISCORD_BOT_TOKEN"
$env:Discord__ClientId = "YOUR_DISCORD_CLIENT_ID"
$env:Discord__ClientSecret = "YOUR_DISCORD_CLIENT_SECRET"
$env:Discord__GuildId = "YOUR_DISCORD_GUILD_ID"
$env:BaseUrl = "YOUR_BASE_URL"
```

### For Production (Azure)

1. **Use Azure Key Vault** (Recommended):

```csharp
// Add to Program.cs if needed
builder.Configuration.AddAzureKeyVault(new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

2. **Or use Azure App Settings** (Simpler):

In your Azure Portal → App Service → Configuration → Application Settings:

- `ConnectionStrings__DefaultConnection`
- `Riot__ApiKey`
- `Faceit__ApiKey`
- `Discord__BotToken`
- `Discord__ClientId`
- `Discord__ClientSecret`
- `Discord__GuildId`
- `BaseUrl`

---

## Configuration File Structure

- `appsettings.example.json` - Template file (safe to commit)
- `appsettings.json` - Your actual configuration (EXCLUDED from Git)
- `.gitignore` - Now excludes sensitive config files

---

## Next Steps

1. ✅ Download your current `appsettings.json` (it's been cleared)
2. ✅ Add your real credentials using User Secrets or environment variables
3. ✅ **Regenerate all exposed credentials** immediately
4. ✅ Push the updated `.gitignore` and `appsettings.example.json` to GitHub
5. ⚠️  Consider using Git filter-branch or BFG Repo-Cleaner to remove secrets from Git history

---

## Testing Your Setup

1. Run the application locally with your new secrets configured
2. Verify the Discord bot connects successfully
3. Test API integrations
4. Deploy to Azure with secure configuration

---

## Important Notes

- **NEVER commit sensitive data to Git**
- The `appsettings.json` is now in `.gitignore` to prevent future accidents
- Use User Secrets for local development
- Use Azure Key Vault or App Settings for production
- Rotate all exposed credentials immediately

