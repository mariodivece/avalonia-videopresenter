﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace AvaloniaApplication1;

internal sealed class DwmRenderTimer : IDisposable
{
    public event EventHandler<RenderTimerEventArgs>? Tick;
    private readonly Thread _renderTick;
    private long m_IsDisposed;

    public DwmRenderTimer()
    {
        _renderTick = new Thread(() =>
        {
            var sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                _ = DwmFlush();
                if (Interlocked.Read(ref m_IsDisposed) > 0)
                    break;

                Tick?.Invoke(this, new() { Time = sw.Elapsed });
            }

            Tick = null;
        })
        {
            IsBackground = true
        };

        _renderTick.Start();
    }

    [DllImport("Dwmapi.dll")]
    private static extern int DwmFlush();

    [DllImport("Dwmapi.dll")]
    private static extern int DwmIsCompositionEnabled(out bool enabled);

    public void Dispose()
    {
        Interlocked.Increment(ref m_IsDisposed);
    }
}

public class RenderTimerEventArgs : EventArgs
{
    public TimeSpan Time { get; init; }
}