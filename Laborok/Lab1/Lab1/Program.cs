using Silk.NET.Windowing;
using Silk.NET.Maths;

namespace Lab1
{
    internal class Program
    {
        private static IWindow grapichsWindow;
        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "1. szemi";
            windowOptions.Size = new Vector2D<int>(500, 500);
            grapichsWindow = Window.Create(windowOptions);

            grapichsWindow.Run();
        }
    }
}
