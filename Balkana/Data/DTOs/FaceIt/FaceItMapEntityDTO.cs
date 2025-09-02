namespace Balkana.Data.DTOs.FaceIt
{
    public class FaceItMapEntityDTO
    {
        public string name { get; set; }          // e.g., "Inferno"
        public string class_name { get; set; }    // e.g., "de_inferno"
        public string game_map_id { get; set; }   // sometimes same as class_name
    }
}
