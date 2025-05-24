// Pridajte túto triedu do vášho projektu (napr. ako WindowHelper.cs)

using System;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace TestBenchTarget.WinUI3
{
    /// <summary>
    /// Helper trieda pre inteligentné umiestnenie okien
    /// </summary>
    public static class WindowHelper
    {
        /// <summary>
        /// Zabezpečí, že okno sa zobrazí v rámci viditeľnej plochy
        /// </summary>
        /// <param name="appWindow">AppWindow objekt</param>
        /// <param name="windowSize">Požadovaná veľkosť okna</param>
        /// <param name="preferredPosition">Preferovaná pozícia (null pre centrovanie)</param>
        public static void EnsureWindowVisibility(AppWindow appWindow, SizeInt32 windowSize, PointInt32? preferredPosition = null)
        {
            if (appWindow == null) return;

            // Získanie informácií o display
            var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
            if (displayArea == null) return;

            var workArea = displayArea.WorkArea;

            // Zabezpečenie minimálnych rozmerov
            var safeWidth = Math.Min(windowSize.Width, workArea.Width - 100); // 50px margin na každej strane
            var safeHeight = Math.Min(windowSize.Height, workArea.Height - 100); // 50px margin hore a dole

            var finalSize = new SizeInt32(safeWidth, safeHeight);

            PointInt32 finalPosition;

            if (preferredPosition.HasValue)
            {
                // Použitie preferovanej pozície s korekciou
                finalPosition = EnsurePositionInWorkArea(preferredPosition.Value, finalSize, workArea);
            }
            else
            {
                // Centrovanie okna
                finalPosition = new PointInt32(
                    workArea.X + (workArea.Width - finalSize.Width) / 2,
                    workArea.Y + (workArea.Height - finalSize.Height) / 2
                );
            }

            // Aplikovanie veľkosti a pozície
            appWindow.Resize(finalSize);
            appWindow.Move(finalPosition);
        }

        /// <summary>
        /// Zabezpečí, že pozícia je v rámci work area
        /// </summary>
        private static PointInt32 EnsurePositionInWorkArea(PointInt32 position, SizeInt32 windowSize, RectInt32 workArea)
        {
            var x = position.X;
            var y = position.Y;

            // Kontrola pravej strany
            if (x + windowSize.Width > workArea.X + workArea.Width)
            {
                x = workArea.X + workArea.Width - windowSize.Width;
            }

            // Kontrola ľavej strany
            if (x < workArea.X)
            {
                x = workArea.X;
            }

            // Kontrola spodnej strany
            if (y + windowSize.Height > workArea.Y + workArea.Height)
            {
                y = workArea.Y + workArea.Height - windowSize.Height;
            }

            // Kontrola hornej strany
            if (y < workArea.Y)
            {
                y = workArea.Y;
            }

            return new PointInt32(x, y);
        }

        /// <summary>
        /// Získa optimálnu veľkosť okna na základe dostupného priestoru
        /// </summary>
        /// <param name="appWindow">AppWindow objekt</param>
        /// <param name="requestedSize">Požadovaná veľkosť</param>
        /// <param name="maxScreenPercentage">Maximálne percento obrazovky (0.0 - 1.0)</param>
        /// <returns>Optimálna veľkosť okna</returns>
        public static SizeInt32 GetOptimalWindowSize(AppWindow appWindow, SizeInt32 requestedSize, double maxScreenPercentage = 0.9)
        {
            var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
            if (displayArea == null) return requestedSize;

            var workArea = displayArea.WorkArea;

            var maxWidth = (int)(workArea.Width * maxScreenPercentage);
            var maxHeight = (int)(workArea.Height * maxScreenPercentage);

            var optimalWidth = Math.Min(requestedSize.Width, maxWidth);
            var optimalHeight = Math.Min(requestedSize.Height, maxHeight);

            return new SizeInt32(optimalWidth, optimalHeight);
        }

        /// <summary>
        /// Zistí, či je okno úplne viditeľné
        /// </summary>
        /// <param name="appWindow">AppWindow objekt</param>
        /// <returns>True ak je okno úplne viditeľné</returns>
        public static bool IsWindowFullyVisible(AppWindow appWindow)
        {
            if (appWindow == null) return false;

            var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
            if (displayArea == null) return false;

            var workArea = displayArea.WorkArea;
            var windowPosition = appWindow.Position;
            var windowSize = appWindow.Size;

            // Kontrola, či je okno úplne v rámci work area
            return windowPosition.X >= workArea.X &&
                   windowPosition.Y >= workArea.Y &&
                   windowPosition.X + windowSize.Width <= workArea.X + workArea.Width &&
                   windowPosition.Y + windowSize.Height <= workArea.Y + workArea.Height;
        }

        /// <summary>
        /// Oprava pozície okna ak je mimo obrazovku
        /// </summary>
        /// <param name="appWindow">AppWindow objekt</param>
        public static void CorrectWindowPositionIfNeeded(AppWindow appWindow)
        {
            if (appWindow == null) return;

            if (!IsWindowFullyVisible(appWindow))
            {
                EnsureWindowVisibility(appWindow, appWindow.Size, appWindow.Position);
            }
        }
    }
}
