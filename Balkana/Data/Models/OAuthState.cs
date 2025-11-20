using System;
using System.ComponentModel.DataAnnotations;

namespace Balkana.Data.Models
{
    public class OAuthState
    {
        [Key]
        public string State { get; set; }
        
        public string UserId { get; set; }
        
        public string Provider { get; set; } // "FaceIt", "Discord", etc.
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime ExpiresAt { get; set; }

        public string CodeVerifier { get; set; }
    }
}

