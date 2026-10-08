using System;
using System.Threading;
using System.Windows.Forms;

namespace WinDeck
{
    static class Program
    {
        const string MutexName = @"Local\WinDeck.SingleInstance";
        const string ShowEventName = @"Local\WinDeck.ShowPanel";

        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    // 이미 실행 중이면 그 패널을 보이게 하고 끝낸다.
                    try
                    {
                        using (EventWaitHandle ev = EventWaitHandle.OpenExisting(ShowEventName)) ev.Set();
                    }
                    catch (Exception) { }
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => ErrorLog.Report(e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (s, e) => ErrorLog.Report(e.ExceptionObject as Exception);

                AppConfig cfg = ConfigStore.Load();
                AutoStart.RefreshPathIfEnabled();

                var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
                var form = new DeckForm(cfg, true);
                var listener = new Thread(() =>
                {
                    try
                    {
                        while (showEvent.WaitOne())
                        {
                            try { form.BeginInvoke(new Action(form.ShowPanel)); }
                            catch (InvalidOperationException) { }
                        }
                    }
                    catch (Exception) { }
                });
                listener.IsBackground = true;
                listener.Start();

                Application.Run(form);
                GC.KeepAlive(mutex);
            }
        }
    }
}
