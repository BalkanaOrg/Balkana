# Riot Tournament API Setup Guide

This guide explains how to properly configure and use the Riot Tournament API integration.

## ✅ Fixes Applied

Based on the [official Riot Tournament API documentation](https://developer.riotgames.com/docs/lol#tournament-api), the following issues were identified and fixed:

### 1. **Regional Routing** (CRITICAL FIX)
**Problem**: The service was hardcoded to use `americas.api.riotgames.com`  
**Solution**: Now configurable via `appsettings.json`

The Tournament API uses **regional routing values**:
- **`europe`** - EUW, EUNE, TR, RU
- **`americas`** - NA, BR, LAN, LAS, OCE
- **`asia`** - KR, JP
- **`sea`** - Southeast Asia

### 2. **Match ID Endpoint** (CRITICAL FIX)
**Problem**: Using wrong endpoint `codes/{code}/ids`  
**Solution**: Corrected to `matches/by-code/{code}/ids`

### 3. **Match ID Format** (IMPROVEMENT)
**Problem**: Hardcoded platform region as "EUW1"  
**Solution**: Now dynamically uses the tournament's region

## 🔧 Configuration

### appsettings.json
```json
{
  "Riot": {
    "ApiKey": "RGAPI-YOUR-KEY-HERE",
    "TournamentRegion": "europe"
  }
}
```

**TournamentRegion Options:**
- `"europe"` - For EUW, EUNE, TR, RU tournaments
- `"americas"` - For NA, BR, LAN, LAS, OCE tournaments
- `"asia"` - For KR, JP tournaments
- `"sea"` - For Southeast Asia tournaments

## 📋 API Requirements

According to [Riot's Developer API Policy](https://developer.riotgames.com/docs/lol#developer-api-policy):

### Tournament Policy Requirements
1. ✅ **API Key**: Must have Tournament API access (production key)
2. ✅ **Regional Routing**: Must use correct regional endpoint
3. ✅ **Tournament Policies**:
   - Must follow all monetization policies
   - Allot at least 70% of entry fees to prize pool
   - Win conditions must be fair and transparent
   - Must have at least 20 participants
   - No gambling

### API Endpoints Used
All endpoints use the base URL: `https://{region}.api.riotgames.com/lol/tournament/v5/`

1. **`POST /providers`** - Register a tournament provider
   - Body: `{ "region": "EUW1", "url": "https://your-callback-url.com" }`
   - Returns: `providerId` (integer)

2. **`POST /tournaments`** - Create a tournament
   - Body: `{ "providerId": 123, "name": "My Tournament" }`
   - Returns: `tournamentId` (integer)

3. **`POST /codes?tournamentId={id}&count={n}`** - Generate tournament codes
   - Body: Tournament code parameters (map type, pick type, etc.)
   - Returns: Array of tournament code strings

4. **`GET /matches/by-code/{code}/ids`** - Get match IDs from a code
   - Returns: Array of match IDs (as numbers)

5. **`GET /codes/{code}`** - Get tournament code details
   - Returns: Full tournament code object

## 🎮 Workflow

### 1. Create a Tournament
1. Navigate to `/admin/riot-tournaments`
2. Click "Create New Tournament"
3. Fill in:
   - **Name**: Tournament display name
   - **Region**: Platform region (EUW1, NA1, etc.)
   - **Internal Tournament** (optional): Link to existing tournament

The system will:
- Automatically register a provider with Riot if needed
- Create the tournament via Riot API
- Store the tournament in your database

### 2. Generate Tournament Codes
1. Go to tournament details
2. Click "Generate Tournament Codes"
3. Configure:
   - **Count**: Number of codes to generate (1-100)
   - **Series** (optional): Link to a specific series
   - **Teams** (optional): Specify which teams should play
   - **Map Type**: Summoner's Rift or Howling Abyss
   - **Pick Type**: Tournament Draft, Blind Pick, etc.
   - **Spectator Type**: Who can spectate (All, Lobby Only, None)
   - **Team Size**: 1v1 to 5v5

### 3. Share Tournament Codes
- Players create a **Custom Game** in the League client
- They enter the tournament code when creating the lobby
- The game will be tracked by Riot's Tournament API

### 4. Import Match Data
1. After the match is played, go to tournament details
2. Click "Check" next to the tournament code
3. The system will:
   - Query Riot API for match IDs
   - Store the match ID in the database
   - Display the match ID for import

4. Use the Match History Importer to import the match:
   - Navigate to `/match-history/import`
   - Select "RIOT" as source
   - Enter the match ID (format: `EUW1_1234567890`)
   - Select the series
   - Import the match

## 🐛 Troubleshooting

### "Failed to register provider"
- **Cause**: Invalid API key or insufficient permissions
- **Solution**: Verify your API key has Tournament API access

### "Failed to create tournament"
- **Cause**: Invalid provider ID or API key
- **Solution**: Check that provider registration succeeded first

### "Failed to generate tournament codes"
- **Cause**: Invalid tournament ID or parameters
- **Solution**: Verify tournament was created successfully

### "No matches found for this code yet"
- **Cause**: Match hasn't been played or hasn't finished
- **Solution**: Wait for players to complete the match

### "Failed to get match IDs: 404"
- **Cause**: Wrong regional routing
- **Solution**: Verify `TournamentRegion` in `appsettings.json` matches your tournament region

### Match ID format errors
- **Correct Format**: `EUW1_1234567890` (PLATFORM_GAMEID)
- **Platform**: Must match tournament region (EUW1, EUNE1, NA1, etc.)
- **Game ID**: Returned by Riot Tournament API

## 📚 Additional Resources

- [Riot Tournament API Documentation](https://developer.riotgames.com/docs/lol#tournament-api)
- [Riot API Routing Values](https://developer.riotgames.com/docs/lol#routing-values)
- [Riot Developer Portal](https://developer.riotgames.com/)
- [Tournament API Best Practices](https://developer.riotgames.com/docs/lol#tournament-api_best-practices)

## ⚠️ Important Notes

1. **Production API Key Required**: Tournament API is only available with production keys
2. **One Provider Per Region**: You typically register one provider per regional endpoint
3. **Code Expiration**: Tournament codes don't expire but can only be used once
4. **Match Tracking**: Only matches created with tournament codes are tracked
5. **VALORANT**: Separate API access required for VALORANT tournaments

## 🔐 Security

- Never commit your API key to version control
- Keep `appsettings.json` in `.gitignore`
- Use environment variables for production deployments
- Rotate API keys periodically

## 🎯 Testing

1. **Test with Development Key First** (if available)
2. **Verify Regional Routing**:
   - Check logs for API calls
   - Ensure using correct regional endpoint
3. **Test Tournament Code Generation**:
   - Generate a code
   - Create custom game with code
   - Verify match tracking works
4. **Test Match Import**:
   - Play a match with tournament code
   - Check for match ID
   - Import match data

