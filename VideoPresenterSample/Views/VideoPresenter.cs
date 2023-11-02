using Avalonia;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SkiaSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VideoPresenterSample.Views;

public class VideoPresenter : VideoPresenterBase
{
    private WriteableBitmap? BufferBitmap;
    private readonly BusyLocker Locker = new();

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        BufferBitmap?.Dispose();
        BufferBitmap = null;
    }

    /// <summary>
    /// Renders the control.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    public override void Render(DrawingContext context)
    {
        try
        {
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            using (var bmp = AcquireBitmapBuffer())
                WriteBitmapBuffer(bmp.Address, bmp.Size.Width, bmp.Size.Height, bmp.RowBytes);

            if (BufferBitmap is null)
                return;

            UpdateContextRects();
            context.DrawImage(BufferBitmap, ContextSourceRect, ContextTargetRect);
        }
        finally
        {
            QueueRender();
        }
    }

    private void QueueRender() => Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);

    public unsafe ILockedFramebuffer AcquireBitmapBuffer()
    {
        if (BufferBitmap is not null &&
            BufferBitmap.PixelSize == PicturePixelSize)
            return BufferBitmap.Lock();

        BufferBitmap?.Dispose();
        BufferBitmap = new WriteableBitmap(
            PicturePixelSize, PictureDpi, PicturePixelFormat, PictureAlphaFormat);

        var lockedBuffer = BufferBitmap.Lock();
        var s = new Span<byte>(lockedBuffer.Address.ToPointer(),
            lockedBuffer.RowBytes * lockedBuffer.Size.Height);

        s.Clear();
        return lockedBuffer;
    }

    private sealed class BusyLocker
    {
        private long m_IsBusy = 0;

        public IDisposable TryEnter(out bool entered)
        {
            entered = Interlocked.Increment(ref m_IsBusy) <= 1;
            return new Releaser(this, entered);
        }

        private void Release() => Interlocked.Exchange(ref m_IsBusy, 0);

        private readonly struct Releaser : IDisposable
        {
            private readonly bool Acquired;
            private readonly BusyLocker Locker;

            public Releaser(BusyLocker locker, bool acquired)
            {
                Acquired = acquired;
                Locker = locker;
            }

            public void Dispose()
            {
                if (Acquired)
                    Locker.Release();
            }
        }
    }
}
