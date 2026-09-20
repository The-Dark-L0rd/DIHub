namespace DIHub.Core.Models
{
    public class WindowStateModel
    {
        public int X { get; set; } = -1;
        public int Y { get; set; } = -1;
        public int Width { get; set; } = 1400;
        public int Height { get; set; } = 900;
        public bool IsMaximized { get; set; } = false;

        /// <summary>
        /// True when the values look like they came from a real session.
        /// </summary>
        public bool HasValidPosition => X >= 0 && Y >= 0;
    }
}