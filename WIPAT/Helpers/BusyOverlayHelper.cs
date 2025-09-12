// BusyOverlayHelper.cs
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
            _progressBar = progressBar ?? throw new ArgumentNullException(nameof(progressBar));
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
                _setStatus?.Invoke(message, StatusType.Transparent); // or StatusType.Busy if you have one
                _progressBar.Visible = true;
                _progressBar.Style = ProgressBarStyle.Marquee;
                _parentForm.UseWaitCursor = true; // optional
            });
        }

        public void HideBusy()
        {
            SafeUI(() =>
            {
               // _setStatus?.Invoke("", StatusType.Reset); // restore to a neutral style
                _progressBar.Visible = false;
                _parentForm.UseWaitCursor = false; // optional
            });
        }


    }
}