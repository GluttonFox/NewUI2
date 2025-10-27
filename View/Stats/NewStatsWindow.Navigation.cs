using System;
using System.Windows.Forms;

namespace NewUI
{
    public partial class NewStatsWindow
    {
        private void SwitchTo(Page target)
        {
            if (target == _currentPage)
            {
                return;
            }

            if (_slideTimer.Enabled)
            {
                _pendingPage = target;
                return;
            }

            _nextPage = target;
            BuildPage(_nextPage, _contentNext);

            int VisualIndex(Page page) => page == Page.Revenue ? 0 : page == Page.Farming ? 1 : 2;
            bool toRight = VisualIndex(target) < VisualIndex(_currentPage);

            int width = Width;
            _contentNext.Top = _navBar.Bottom;
            _contentCurrent.Top = _navBar.Bottom;

            _contentNext.Left = toRight ? -width : width;
            _contentCurrent.Left = 0;

            _animationLeftStartCurrent = _contentCurrent.Left;
            _animationLeftStartNext = _contentNext.Left;
            _animationTargetLeftCurrent = toRight ? width : -width;
            _animationTargetLeftNext = 0;
            _animationDx = Math.Max(12, width / 18) * (toRight ? 1 : -1);

            _slideTimer.Start();
        }

        private void SlideTimer_Tick(object sender, EventArgs e)
        {
            _contentCurrent.Left += _animationDx;
            _contentNext.Left += _animationDx;

            bool currentDone = (_animationDx < 0) ? _contentCurrent.Left <= _animationTargetLeftCurrent
                                                  : _contentCurrent.Left >= _animationTargetLeftCurrent;
            bool nextDone = (_animationDx < 0) ? _contentNext.Left <= _animationTargetLeftNext
                                               : _contentNext.Left >= _animationTargetLeftNext;

            if (currentDone && nextDone)
            {
                _slideTimer.Stop();
                var temp = _contentCurrent;
                _contentCurrent = _contentNext;
                _contentNext = temp;

                _contentNext.Left = Width;
                _contentNext.Controls.Clear();

                _currentPage = _nextPage;
                UpdateAccentBar();

                if (_pendingPage.HasValue && _pendingPage.Value != _currentPage)
                {
                    var pending = _pendingPage.Value;
                    _pendingPage = null;
                    SwitchTo(pending);
                }
            }
        }
    }
}
