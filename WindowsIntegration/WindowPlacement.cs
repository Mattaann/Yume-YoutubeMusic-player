using Microsoft.UI.Windowing;
using Windows.Graphics;
namespace Nocturne.WindowsIntegration;
public static class WindowPlacement
{
    public static void Restore(AppWindow window, int width, int height, int? x, int? y, int minimumWidth, int minimumHeight)
    {
        var size = new SizeInt32(Math.Clamp(width, minimumWidth, 2560), Math.Clamp(height, minimumHeight, 1600));
        var area = x.HasValue && y.HasValue
            ? DisplayArea.GetFromPoint(new PointInt32(x.Value, y.Value), DisplayAreaFallback.Nearest)
            : DisplayArea.GetFromWindowId(window.Id, DisplayAreaFallback.Primary);
        var work = area.WorkArea;
        size.Width = Math.Min(size.Width, work.Width);
        size.Height = Math.Min(size.Height, work.Height);
        var left = Math.Clamp(x ?? work.X + (work.Width - size.Width) / 2, work.X, work.X + work.Width - size.Width);
        var top = Math.Clamp(y ?? work.Y + (work.Height - size.Height) / 2, work.Y, work.Y + work.Height - size.Height);
        window.MoveAndResize(new RectInt32(left, top, size.Width, size.Height));
        if (window.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = minimumWidth;
            presenter.PreferredMinimumHeight = minimumHeight;
        }
    }
}
