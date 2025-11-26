namespace WinLauncher
{
    using System.Runtime.InteropServices;

    public static class NativeMethods
    {
        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(ref int pfEnabled);

        public static void EnableShadow(IntPtr hWnd)
        {
            var v = 2;
            DwmSetWindowAttribute(hWnd, 2, ref v, 4);

            var margins = new MARGINS()
            {
                cxLeftWidth = 0,
                cxRightWidth = 0,
                cyTopHeight = 1,
                cyBottomHeight = 0
            };

            DwmExtendFrameIntoClientArea(hWnd, ref margins);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }
    }
}