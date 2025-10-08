# Discord Bot Test Scripts

## Test Commands Locally

### Test /team command
```bash
curl -X POST https://localhost:7241/api/discord/test \
  -H "Content-Type: application/json" \
  -d '{"command": "team", "arguments": ["TDI"]}' \
  -k
```

### Test /player command
```bash
curl -X POST https://localhost:7241/api/discord/test \
  -H "Content-Type: application/json" \
  -d '{"command": "player", "arguments": ["ext1nct"]}' \
  -k
```

## Test Discord Webhook (if using ngrok)

### Replace YOUR_NGROK_URL with your actual ngrok URL
```bash
curl -X POST https://YOUR_NGROK_URL/api/discord/interactions \
  -H "Content-Type: application/json" \
  -d '{
    "type": 1,
    "data": {
      "name": "team",
      "options": [
        {
          "name": "tag",
          "value": "TDI"
        }
      ]
    }
  }'
```

## PowerShell Commands (Windows)

### Test /team command
```powershell
Invoke-RestMethod -Uri "https://localhost:7241/api/discord/test" -Method POST -ContentType "application/json" -Body '{"command": "team", "arguments": ["TDI"]}' -SkipCertificateCheck
```

### Test /player command
```powershell
Invoke-RestMethod -Uri "https://localhost:7241/api/discord/test" -Method POST -ContentType "application/json" -Body '{"command": "player", "arguments": ["ext1nct"]}' -SkipCertificateCheck
```

## Troubleshooting

1. **If you get SSL errors**: Add `-k` flag to curl or `-SkipCertificateCheck` to PowerShell
2. **If you get 401 Unauthorized**: Make sure you're logged in as Administrator/Moderator
3. **If you get 404 Not Found**: Check that the application is running on port 7241
4. **If Discord webhook fails**: Make sure you're using ngrok or a public URL, not localhost
