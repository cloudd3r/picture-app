using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PictureApp
{
    /// <summary>
    /// Включает blur-behind / acrylic эффект на окне через недокументированный
    /// SetWindowCompositionAttribute. Работает на Windows 10 (включая LTSC),
    /// на Windows 11 даёт похожий результат через DWM композицию.
    /// </summary>
    internal static class AcrylicHelper
    {
        // ---- WinAPI ----
        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(
            IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4, // Win10 1803+
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor; // ABGR
            public int AnimationId;
        }

        public static void EnableBlurBehind(Window window)
        {
            if (window == null) return;

            var helper = new WindowInteropHelper(window);
            // Если hwnd ещё не создан — создаём (на момент Loaded он уже есть).
            IntPtr hwnd = helper.EnsureHandle();
            if (hwnd == IntPtr.Zero) return;

            // Сначала пробуем acrylic (Win10 1803+), при отказе откатываемся к blur.
            if (!Apply(hwnd, AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND, 0x40FFFFFF))
            {
                Apply(hwnd, AccentState.ACCENT_ENABLE_BLURBEHIND, 0);
            }
        }

        private static bool Apply(IntPtr hwnd, AccentState state, int gradientColor)
        {
            var accent = new AccentPolicy
            {
                AccentState = state,
                AccentFlags = 2, // включаем все 4 границы для blur
                GradientColor = gradientColor
            };

            int size = Marshal.SizeOf(accent);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(accent, ptr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = size,
                    Data = ptr
                };
                int result = SetWindowCompositionAttribute(hwnd, ref data);
                return result == 0 || result == 1; // 0 = OK; некоторые билды возвращают 1 на успех
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
