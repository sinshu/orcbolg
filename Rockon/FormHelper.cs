using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rockon
{
    internal static class FormHelper
    {
        public static void SetFormResizeAction(Form form, Action action)
        {
            form.ResizeEnd += (sender, e) => action();

            var previous = form.WindowState;
            form.Resize += (sender, e) =>
            {
                if (form.WindowState == FormWindowState.Maximized)
                {
                    action();
                }
                else if (form.WindowState == FormWindowState.Normal && previous == FormWindowState.Maximized)
                {
                    action();
                }

                previous = form.WindowState;
            };
        }

        public static void SetAsyncFormClosingAction(Form form, Action closingStart, Action closingEnd, Task completion)
        {
            var closing = false;
            var canClose = false;
            form.FormClosing += async (sender, e) =>
            {
                if (canClose)
                {
                    return;
                }

                if (!closing)
                {
                    closingStart();
                    closing = true;
                    e.Cancel = true;
                }
                else
                {
                    e.Cancel = true;
                    return;
                }

                try
                {
                    await completion;
                }
                catch
                {
                }

                closingEnd();
                canClose = true;
                form.Close();
            };
        }
    }
}
