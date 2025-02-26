using Microsoft.AspNetCore.Mvc;

namespace Balkana.Data
{
    public class DataConstants
    {
        public const int defaultStringMaxLength = 15;
        public const int defaultStringMinLength = 3;

        //Teams
        public const int TeamFullNameMaxLength = 50;
        public const int TeamFullNameMinLength = 1;

        public const int TeamTagMaxLength = 5;
        public const int TeamTagMinLength = 5;

        //Names
        public const int NameMinLength = 3;
        public const int NameMaxLength = 30;

        //Tournaments
        public const int TournamentFullNameMaxLength = 50;
        public const int TournamentShortNameMaxLength = 15;
    }
}
