using libusbK;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ControlK
{
    class Program
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            var config = new NLog.Config.LoggingConfiguration();

            var consoleTarget = new NLog.Targets.ConsoleTarget("Console")
            {
                Layout = "${longdate} | ${logger} | ${level} |   ${message}"
            };

            config.AddRule(LogLevel.Trace, LogLevel.Fatal, consoleTarget);
            LogManager.Configuration = config;

            AppDomain.CurrentDomain.UnhandledException += (s, e) => _logger.Error(e);

            var listenParams = new KHOT_PARAMS();
            listenParams.PatternMatch.DeviceInterfaceGUID = "*";
            listenParams.Flags = KHOT_FLAG.PLUG_ALL_ON_INIT;
            listenParams.OnHotPlug = OnHotPlug;

            // var hot = new HotK(ref listenParams);

            Echo.Run();

            // hot.Dispose();
        }

        private static void OnHotPlug(KHOT_HANDLE handle, KLST_DEVINFO_HANDLE deviceInfo, KLST_SYNC_FLAG plugType)
        {
            var vid = deviceInfo.Common.Vid;
            var pid = deviceInfo.Common.Pid;
        }
    }
}
