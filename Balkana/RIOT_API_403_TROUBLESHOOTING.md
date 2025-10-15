# Riot API 403 Error Troubleshooting

## 🔴 Common Causes of 403 Forbidden

### 1. **Tournament API Not Enabled (MOST COMMON)**

**Problem**: Your production API key might not have Tournament API access specifically enabled.

**How to Check**:
1. Go to [Riot Developer Portal](https://developer.riotgames.com/)
2. Navigate to your application
3. Check "APIs" section - you should see **"Tournament-V5"** listed
4. Verify your application status is **"Approved"** (not just "Acknowledged")

**Solution**: 
- If Tournament API is not listed, you need to request it specifically
- Tournament API requires additional approval beyond standard production keys
- Contact Riot Support to enable Tournament API for your application

### 2. **Using Development Key Instead of Production Key**

**Problem**: Tournament API **only works with production keys**.

**How to Check**:
- Development keys start with: `RGAPI-` followed by UUID
- They expire after 24 hours
- Check if your key format matches your environment

**Solution**:
- Generate a production API key from your approved application
- Development keys do NOT have Tournament API access

### 3. **Wrong API Version or Endpoint**

**Problem**: Using outdated Tournament API version.

**Current Version**: `/lol/tournament/v5/` (NOT v4 or v3)

**Verify in Code**:
```csharp
// Should be v5, not v4
_httpClient.BaseAddress = new Uri($"https://{tournamentRegion}.api.riotgames.com/lol/tournament/v5/");
```

### 4. **Regional Routing Mismatch**

**Problem**: Tournament region doesn't match the regional routing endpoint.

**Valid Combinations**:
| Tournament Region | API Endpoint | Routing Region |
|------------------|--------------|----------------|
| EUW1, EUNE1, TR1, RU | europe.api.riotgames.com | europe |
| NA1, BR1, LA1, LA2, OC1 | americas.api.riotgames.com | americas |
| KR, JP1 | asia.api.riotgames.com | asia |

**Verify in appsettings.json**:
```json
{
  "Riot": {
    "TournamentRegion": "europe"  // Must match your platform regions
  }
}
```

### 5. **Missing or Malformed Request Body**

**Problem**: Provider/tournament registration requires specific fields.

**Provider Registration** requires:
```json
{
  "region": "EUW1",  // Must be valid platform (EUW1, EUNE1, etc.)
  "url": ""          // Can be empty string, but field is required
}
```

**Tournament Creation** requires:
```json
{
  "providerId": 123,  // Must be a valid provider ID
  "name": "My Tournament"  // Optional but recommended
}
```

### 6. **API Key Not in Header**

**Problem**: API key not being sent correctly.

**Correct Header**:
```
X-Riot-Token: RGAPI-your-key-here
```

**Our Implementation** (verify this is set):
```csharp
_httpClient.DefaultRequestHeaders.Add("X-Riot-Token", _apiKey);
```

## 🔍 Debugging Steps

### Step 1: Check Application Status

Run your app and check the console output when creating a tournament. You should see:

```
[RIOT API] POST https://europe.api.riotgames.com/lol/tournament/v5/providers
[RIOT API] Region: EUW1, Callback: 
[RIOT API] API Key: RGAPI-xxxx...
```

**What to verify**:
- ✅ Endpoint uses `v5` (not v4)
- ✅ Regional routing matches your region (`europe` for EUW/EUNE)
- ✅ API key starts with `RGAPI-`

### Step 2: Test API Key Directly

Use PowerShell or curl to test your API key:

**PowerShell**:
```powershell
$headers = @{
    "X-Riot-Token" = "RGAPI-your-key-here"
}

$body = @{
    "region" = "EUW1"
    "url" = ""
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://europe.api.riotgames.com/lol/tournament/v5/providers" `
    -Method Post `
    -Headers $headers `
    -Body $body `
    -ContentType "application/json"
```

**Expected Results**:
- ✅ **200 OK + Provider ID** → API key is valid, issue is in code
- ❌ **403 Forbidden** → API key doesn't have Tournament API access
- ❌ **404 Not Found** → Wrong endpoint or region
- ❌ **401 Unauthorized** → Invalid API key

### Step 3: Verify API Key Permissions

1. Log into [Riot Developer Portal](https://developer.riotgames.com/)
2. Go to "My Applications"
3. Click your application
4. Check "Enabled APIs" section
5. **You must see**: `TOURNAMENT-V5` or `Tournament API v5`

If you DON'T see Tournament API listed:
- Your key doesn't have tournament permissions
- You need to request access specifically
- This is the most common cause of 403 errors

### Step 4: Check API Key Status

API keys can be:
- **Active** → Should work
- **Expired** → 403 or 401 error
- **Revoked** → 403 error
- **Rate Limited** → 429 error (not 403)

### Step 5: Verify Platform Regions

The `region` field in provider registration must be a **platform region**, not a routing region:

**Valid Platform Regions**:
- `EUW1` - Europe West
- `EUNE1` - Europe Nordic & East
- `NA1` - North America
- `BR1` - Brazil
- `LA1` - Latin America North
- `LA2` - Latin America South
- `OC1` - Oceania
- `TR1` - Turkey
- `RU` - Russia
- `JP1` - Japan
- `KR` - Korea

**Invalid** (these are routing regions, not platform regions):
- ❌ `europe`
- ❌ `americas`
- ❌ `asia`

## 🛠️ Quick Fixes to Try

### Fix 1: Verify API Key in appsettings.json

Open `appsettings.json` and verify:
```json
{
  "Riot": {
    "ApiKey": "RGAPI-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "TournamentRegion": "europe"
  }
}
```

- Key should be exactly as shown in Developer Portal
- No extra spaces or quotes
- Must start with `RGAPI-`

### Fix 2: Check Environment

If you're using multiple environments (Development, Production):

**appsettings.Development.json**:
```json
{
  "Riot": {
    "ApiKey": "RGAPI-your-dev-key"
  }
}
```

**appsettings.Production.json**:
```json
{
  "Riot": {
    "ApiKey": "RGAPI-your-prod-key"
  }
}
```

Verify you're using the correct key for your environment.

### Fix 3: Test with a Simple Request

Before trying tournament operations, test with a simpler endpoint:

```csharp
// Test endpoint (doesn't require tournament permissions)
var response = await _httpClient.GetAsync($"https://europe.api.riotgames.com/lol/platform/v3/champion-rotations");
```

If this works, your API key is valid but doesn't have tournament access.  
If this fails, your API key itself is invalid.

## 📧 Contacting Riot Support

If none of the above fixes work, you may need to contact Riot:

1. Go to [Riot Games Support](https://support-leagueoflegends.riotgames.com/hc/en-us/requests/new)
2. Select "Third Party Applications"
3. Provide:
   - Your Application Name
   - Application ID
   - The exact error message
   - The endpoint you're trying to access
   - Confirmation that your application is approved

## 🎯 Most Likely Solution

Based on hundreds of similar reports, the **#1 cause** of 403 errors with Tournament API is:

> **Your production API key doesn't have Tournament API permissions specifically enabled.**

Even if your application is "Approved", Tournament API access must be **explicitly granted** by Riot. It's not automatic with standard production keys.

## ✅ Verification Checklist

Before proceeding, verify:

- [ ] Application status is "Approved" (not "Acknowledged")
- [ ] Tournament-V5 API is listed in your application's APIs
- [ ] API key starts with `RGAPI-` and is a production key
- [ ] API key is not expired (check in Developer Portal)
- [ ] `TournamentRegion` in appsettings.json matches your region
- [ ] Using correct endpoint: `/lol/tournament/v5/`
- [ ] Regional routing matches platform regions (europe for EUW/EUNE)
- [ ] Request body includes all required fields

## 📊 Common Response Codes

| Code | Meaning | Likely Cause |
|------|---------|--------------|
| 200 | Success | Everything works! |
| 400 | Bad Request | Invalid JSON or missing required fields |
| 401 | Unauthorized | Invalid API key |
| 403 | Forbidden | Valid key but no Tournament API access |
| 404 | Not Found | Wrong endpoint or region |
| 415 | Unsupported Media Type | Missing `Content-Type: application/json` |
| 429 | Rate Limited | Too many requests |
| 500 | Server Error | Riot API issue (rare) |

---

**Need More Help?**
1. Check console output for detailed error messages
2. Test your API key with PowerShell/curl
3. Verify Tournament API is enabled in Developer Portal
4. Contact Riot Support if issue persists

