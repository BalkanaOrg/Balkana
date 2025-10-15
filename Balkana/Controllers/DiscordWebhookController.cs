using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using NSec.Cryptography;
using Balkana.Services.Discord;

namespace Balkana.Controllers
{
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class DiscordWebhookController : ControllerBase
    {
        private readonly ILogger<DiscordWebhookController> _logger;
        private readonly IConfiguration _configuration;

        public DiscordWebhookController(ILogger<DiscordWebhookController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("api/discord/test")]
        public IActionResult TestCommand()
        {
            _logger.LogInformation("Test command endpoint called");
            return Ok(new { message = "Test successful" });
        }

        [HttpPost("interactions")]
        public async Task<IActionResult> HandleInteraction()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation($"Discord interaction endpoint called at {startTime:yyyy-MM-dd HH:mm:ss.fff} UTC");
            
            try
            {
                // Read the raw request body
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                
                _logger.LogInformation($"Received Discord interaction (length: {body.Length})");
                
                // Verify Discord signature
                var signatureResult = VerifyDiscordSignature(body);
                _logger.LogInformation($"Discord signature verification result: {signatureResult}");
                
                // Enable signature verification
                if (!signatureResult)
                {
                    _logger.LogWarning("Discord signature verification failed");
                    return Unauthorized();
                }
                
                // Parse the JSON to check interaction type
                using var jsonDoc = JsonDocument.Parse(body);
                var root = jsonDoc.RootElement;
                
                if (root.TryGetProperty("type", out var typeElement))
                {
                    var type = typeElement.GetInt32();
                    
                    if (type == 1) // PING
                    {
                        var responseTime = DateTime.UtcNow;
                        var processingTime = responseTime - startTime;
                        _logger.LogInformation($"Responding to Discord PING with PONG (processing time: {processingTime.TotalMilliseconds}ms)");
                        
                        // Return the exact response Discord expects
                        return Ok(new { type = 1 });
                    }
                    else if (type == 2) // APPLICATION_COMMAND
                    {
                        _logger.LogInformation("Received Discord slash command");
                        
                        if (root.TryGetProperty("data", out var dataElement) && 
                            dataElement.TryGetProperty("name", out var nameElement))
                        {
                            var commandName = nameElement.GetString();
                            _logger.LogInformation($"Processing command: {commandName}");
                            
                            // Handle specific commands
                            if (commandName == "test")
                            {
                                return Ok(new { 
                                    type = 4, // CHANNEL_MESSAGE_WITH_SOURCE
                                    data = new { 
                                        content = "Hello world! 🎉" 
                                    } 
                                });
                            }
                                else if (commandName == "team" || commandName == "player" || commandName == "transfers" || commandName == "bracket")
                                {
                                    // Handle team/player/transfers commands using DiscordBotService
                                    var discordBotService = HttpContext.RequestServices.GetRequiredService<IDiscordBotService>();
                                    var arguments = new List<string>();
                                    
                                    if (dataElement.TryGetProperty("options", out var optionsElement))
                                    {
                                        foreach (var option in optionsElement.EnumerateArray())
                                        {
                                            if (option.TryGetProperty("value", out var valueElement))
                                            {
                                                arguments.Add(valueElement.GetString() ?? "");
                                            }
                                        }
                                    }
                                    
                                    var response = await discordBotService.ProcessCommandAsync(commandName, arguments.ToArray());
                                    
                                    // Check if this is a bracket image response
                                    if (response.StartsWith("BRACKET_IMAGE:"))
                                    {
                                        var parts = response.Split(':');
                                        if (parts.Length >= 3)
                                        {
                                            var tournamentId = parts[1];
                                            var tournamentName = string.Join(":", parts.Skip(2)); // In case tournament name contains colons
                                            
                                            // Use a direct image URL that Discord can embed
                                            var imageUrl = $"{Request.Scheme}://{Request.Host}/api/tournaments/{tournamentId}/bracket/image";
                                            
                                            return Ok(new
                                            {
                                                type = 4, // CHANNEL_MESSAGE_WITH_SOURCE
                                                data = new
                                                {
                                                    embeds = new[]
                                                    {
                                                        new
                                                        {
                                                            title = $"🏆 {tournamentName}",
                                                            description = "Tournament Bracket",
                                                            image = new { url = imageUrl },
                                                            color = 3447003, // Blue color
                                                            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                                                            footer = new
                                                            {
                                                                text = "Balkana Tournament System"
                                                            }
                                                        }
                                                    }
                                                }
                                            });
                                        }
                                    }
                                    
                                    return Ok(new { 
                                        type = 4, // CHANNEL_MESSAGE_WITH_SOURCE
                                        data = new { 
                                            content = response
                                        } 
                                    });
                                }
                            
                            _logger.LogWarning($"Unknown command: {commandName}");
                            return Ok(new { 
                                type = 4, // CHANNEL_MESSAGE_WITH_SOURCE
                                data = new { 
                                    content = $"Unknown command: {commandName}" 
                                } 
                            });
                        }
                        
                        return Ok(new { 
                            type = 4, // CHANNEL_MESSAGE_WITH_SOURCE
                            data = new { 
                                content = "Command received! This is a placeholder response." 
                            } 
                        });
                    }
                }
                
                // Default response for unknown interaction types
                var defaultResponseTime = DateTime.UtcNow;
                var defaultProcessingTime = defaultResponseTime - startTime;
                _logger.LogInformation($"Unknown interaction type, responding with PONG (processing time: {defaultProcessingTime.TotalMilliseconds}ms)");
                return Ok(new { type = 1 });
            }
            catch (Exception ex)
            {
                var errorTime = DateTime.UtcNow;
                var errorProcessingTime = errorTime - startTime;
                _logger.LogError(ex, $"Error handling Discord interaction (processing time: {errorProcessingTime.TotalMilliseconds}ms)");
                return Ok(new { type = 1 }); // Still respond with PONG to avoid Discord retries
            }
        }

        [HttpGet("api/discord/verify")]
        public IActionResult Verify()
        {
            _logger.LogInformation("Discord verification endpoint called");
            return Ok(new { status = "ok", message = "Discord bot is running" });
        }

        private bool VerifyDiscordSignature(string body)
        {
            try
            {
                var signature = Request.Headers["X-Signature-Ed25519"].FirstOrDefault();
                var timestamp = Request.Headers["X-Signature-Timestamp"].FirstOrDefault();
                
                if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp))
                {
                    _logger.LogWarning("Missing Discord signature headers");
                    return false;
                }

                // Discord's public key (from your message)
                var publicKeyHex = "fbf662b9250ad0b240544421cbf516836aa854926b21fec0e09e79f2c0668da8";
                
                // Convert hex strings to bytes
                var signatureBytes = ConvertHexStringToByteArray(signature);
                var publicKeyBytes = ConvertHexStringToByteArray(publicKeyHex);
                
                // Create the message to verify (timestamp + body)
                var message = timestamp + body;
                var messageBytes = Encoding.UTF8.GetBytes(message);
                
                // Import the public key using the correct format for Discord
                var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, publicKeyBytes, KeyBlobFormat.RawPublicKey);
                
                // Verify the signature
                var isValid = SignatureAlgorithm.Ed25519.Verify(publicKey, messageBytes, signatureBytes);
                
                _logger.LogInformation($"Discord signature verification result: {isValid}");
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Discord signature");
                return false;
            }
        }

        private static byte[] ConvertHexStringToByteArray(string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException("Hex string must have even length");
            }

            var bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}
