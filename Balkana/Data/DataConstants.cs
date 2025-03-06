using Microsoft.AspNetCore.Mvc;

namespace Balkana.Data
{
    public class DataConstants
    {
        public const int defaultYear = 2025;

        public const int defaultStringMaxLength = 15;
        public const int defaultStringMinLength = 3;

        //Teams
        public const int TeamFullNameMaxLength = 50;
        public const int TeamFullNameMinLength = 1;

        public const int TeamTagMaxLength = 7;
        public const int TeamTagMinLength = 1;

        //Names
        public const int NameMinLength = 2;
        public const int NameMaxLength = 30;

        //Tournaments
        public const int TournamentFullNameMaxLength = 50;
        public const int TournamentShortNameMaxLength = 15;
    }
}
