using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace PeripheralBatteryMonitor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            EmbeddedAssemblies.Install();
            Run();
        }

            //Split out and never inlined: Run mentions Settings, whose fields are Core
            //types, so the JIT would try to load PeripheralBatteryMonitor.Core before
            //Main's first statement had a chance to install the resolver.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Run()
        {
                //On net48 these are explicit calls rather than the generated
                //ApplicationConfiguration.Initialize() of modern .NET, and there is no
                //Application.SetHighDpiMode either -- the DPI mode comes from app.manifest.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Settings());
        }
    }
}
