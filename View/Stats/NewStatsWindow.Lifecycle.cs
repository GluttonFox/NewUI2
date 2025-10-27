using System;
using System.Windows.Forms;

namespace NewUI
{
    public partial class NewStatsWindow
    {
        private void CleanupUIResources()
        {
            try
            {
                _tabFarming?.Dispose();
                _tabRevenue?.Dispose();
                _tabTrading?.Dispose();
                _accentUnderline?.Dispose();
                _navBar?.Dispose();

                _contentCurrent?.Controls.Clear();
                _contentCurrent?.Dispose();
                _contentNext?.Controls.Clear();
                _contentNext?.Dispose();

                _boundControls?.Clear();
            }
            catch (Exception)
            {
                // 忽略清理过程中的异常，确保不会影响窗口关闭流程。
            }
        }
    }
}
