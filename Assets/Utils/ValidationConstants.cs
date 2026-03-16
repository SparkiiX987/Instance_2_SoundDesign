namespace Auth.Utils
{
    /// <summary>
    /// Constantes de validation centralisées pour l'authentification et les formulaires
    /// </summary>
    public static class ValidationConstants
    {
        // Password validation
        public const int MinPasswordLength = 8;
        public const int MaxPasswordLength = 128;
        
        // Username validation
        public const int MinUsernameLength = 3;
        public const int MaxUsernameLength = 20;
        
        // Email validation
        public const int MaxEmailLength = 255;
        
        // Friend request
        public const int MaxFriendRequestMessage = 200;
    }
}

