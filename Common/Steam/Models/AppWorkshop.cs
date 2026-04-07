namespace SteamEmuUtility.Common.Steam.Models
{
    public class AppWorkshop
    {
        public int? Size { get; }
        public int? TimeUpdated { get; }
        public AppWorkshop(int? size, int? timeupdated)
        {
            Size = size;
            TimeUpdated = timeupdated;
        }
    }
}
