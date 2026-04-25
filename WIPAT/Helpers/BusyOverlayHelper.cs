using System;
using System.Windows.Forms;
using WIPAT.Entities.Enum;

namespace WIPAT.Helpers
{
    public class BusyOverlayHelper
    {
        private readonly ProgressBar _progressBar;
        private readonly Action<string, StatusType> _setStatus;
        private readonly Form _parentForm;

        public BusyOverlayHelper(Form parentForm, ProgressBar progressBar, Action<string, StatusType> setStatus)
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));

            // FIX: Allow progressBar to be null. Do not throw exception.
            _progressBar = progressBar;

            _setStatus = setStatus;
        }

        private void SafeUI(Action action)
        {
            if (_parentForm.IsDisposed) return;
            if (_parentForm.InvokeRequired)
                _parentForm.BeginInvoke(action);
            else
                action();
        }

        public void ShowBusy(string message)
        {
            SafeUI(() =>
            {
                _setStatus?.Invoke(message, StatusType.Transparent);

                // FIX: Check for null before accessing
                if (_progressBar != null)
                {
                    _progressBar.Visible = true;
                    _progressBar.Style = ProgressBarStyle.Marquee;
                }

                _parentForm.UseWaitCursor = true;
            });
        }

        public void HideBusy()
        {
            SafeUI(() =>
            {
                // FIX: Check for null before accessing
                if (_progressBar != null)
                {
                    _progressBar.Visible = false;
                }

                _parentForm.UseWaitCursor = false;
            });
        }
    }
}