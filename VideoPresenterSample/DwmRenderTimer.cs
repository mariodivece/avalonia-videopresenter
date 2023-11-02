using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;

namespace VideoPresenterSample;

internal sealed partial class DwmRenderTimer : IDisposable
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

    [LibraryImport("Dwmapi.dll")]
    private static partial int DwmFlush();

    [LibraryImport("Dwmapi.dll")]
    private static partial int DwmIsCompositionEnabled([MarshalAs(UnmanagedType.Bool)] out bool enabled);

    public void Dispose()
    {
        Interlocked.Increment(ref m_IsDisposed);
    }
}

public class RenderTimerEventArgs : EventArgs
{
    public TimeSpan Time { get; init; }
}