using System.Runtime.InteropServices;

namespace WinLauncher.Infrastructure
{
    /// <summary>
    /// 原生方法类
    /// 包含与 Windows API 交互的 P/Invoke 声明
    /// 用于实现窗口阴影等高级效果
    /// </summary>
    public static class NativeMethods
    {
        /// <summary>
        /// 将窗口框架扩展到客户区，用于实现自定义窗口阴影
        /// </summary>
        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        /// <summary>
        /// 设置窗口属性
        /// </summary>
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        /// <summary>
        /// 检查 DWM 合成是否启用
        /// </summary>
        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(ref int pfEnabled);

        /// <summary>
        /// 启用窗口阴影效果
        /// </summary>
        public static void EnableShadow(IntPtr hWnd)
        {
            var v = 2;
            DwmSetWindowAttribute(hWnd, 2, ref v, 4);

            var margins = new MARGINS()
            {
                cxLeftWidth = 0,
                cxRightWidth = 0,
                cyTopHeight = 1, // 顶部扩展1像素用于阴影
                cyBottomHeight = 0
            };

            DwmExtendFrameIntoClientArea(hWnd, ref margins);
        }

        /// <summary>
        /// 边距结构体，用于 DWM 扩展框架
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;   // 左边距
            public int cxRightWidth;  // 右边距
            public int cyTopHeight;   // 上边距
            public int cyBottomHeight; // 下边距
        }
    }
}
