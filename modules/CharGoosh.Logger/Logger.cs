// CharGoosh - Copyright (c) 2026 MuttonTheLast
// This file is part of CharGoosh.
//
// Licensed under the GNU GPLv3 with additional permissions.
// See the LICENSE file for details.
//
// This program is distributed WITHOUT ANY WARRANTY.
// "CharGoosh"™ is a trademark of MuttonTheLast.

/// why is this a module?
/// i want other modules have access to it and its a simple init and dispose.
/// main reason is maybe some mod wants to have customized logger or change main logger

using Serilog;

namespace CharGoosh.Logger;

public class Logger : IDisposable
{
    public static LoggerConfiguration DefaultConfig => new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.File("log.txt",
                rollingInterval: RollingInterval.Hour,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 5 * 1024 * 1024); // 5mb each log


    public Serilog.Core.Logger Handle { get; private set; }
    bool _isGlobal = false;
    bool disposed = false;

    public Logger(LoggerConfiguration? configuration = null)
    {
        configuration = configuration == null ? DefaultConfig : configuration;
        Handle = configuration.CreateLogger();
    }

    public static implicit operator Serilog.Core.Logger(Logger logger)
    {

        return logger.Handle;
    }

    // if true then sets this logger to be global logger
    public void SetGlobal(bool global = true)
    {
        if (global)
        {
            Log.Logger = Handle;
        }
        else
        {

            Log.Logger = DefaultConfig.CreateLogger();
        }
        _isGlobal = global;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }
        if (disposing)
        {
            Handle.Dispose();
            if (_isGlobal)
            {
                SetGlobal(false);
            }
        }
        disposed = true;
    }
}
