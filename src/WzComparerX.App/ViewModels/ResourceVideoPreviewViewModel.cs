using Avalonia.Media.Imaging;
using Avalonia.Threading;
using WzComparerX.App.Services;
using WzComparerX.Rendering;

namespace WzComparerX.App.ViewModels;

public sealed class ResourceVideoPreviewViewModel : ResourceBitmapPreviewViewModel
{
    private static readonly TimeSpan MinimumFrameDelay = TimeSpan.FromMilliseconds(16);

    private readonly ResourceVideoSequenceDocument document;
    private readonly WriteableBitmap writableBitmap;
    private readonly DispatcherTimer timer;
    private int currentFrameIndex;
    private bool isPlaying;

    public ResourceVideoPreviewViewModel(
        ResourceVideoSequenceDocument document,
        WriteableBitmap bitmap,
        double initialScale = DefaultScale)
        : base(
            document.SourcePath,
            document.Selector,
            document.ValuePath,
            document.Width,
            document.Height,
            format: 0,
            bitmap,
            initialScale)
    {
        this.document = document;
        writableBitmap = bitmap;
        timer = new DispatcherTimer();
        timer.Tick += OnTimerTick;
    }

    public override string Title => document.Title;

    public string FourCc => document.FourCc;

    public int FrameCount => document.FrameCount;

    public int CurrentFrameIndex
    {
        get => currentFrameIndex;
        private set
        {
            if (currentFrameIndex == value)
            {
                return;
            }

            currentFrameIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PlaybackLabel));
        }
    }

    public bool IsPlaying
    {
        get => isPlaying;
        private set
        {
            if (isPlaying == value)
            {
                return;
            }

            isPlaying = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PlaybackLabel));
        }
    }

    public string PlaybackLabel => FrameCount == 0
        ? "0 / 0"
        : $"{CurrentFrameIndex + 1} / {FrameCount}";

    public void Start()
    {
        if (FrameCount <= 1 || IsPlaying)
        {
            return;
        }

        IsPlaying = true;
        timer.Interval = GetFrameDelay(CurrentFrameIndex);
        timer.Start();
    }

    public void Stop()
    {
        timer.Stop();
        IsPlaying = false;
    }

    public void StepNext()
    {
        if (FrameCount == 0)
        {
            return;
        }

        SetFrame((CurrentFrameIndex + 1) % FrameCount);
    }

    public override void Dispose()
    {
        Stop();
        timer.Tick -= OnTimerTick;
        base.Dispose();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (FrameCount == 0)
        {
            Stop();
            return;
        }

        StepNext();
        timer.Interval = GetFrameDelay(CurrentFrameIndex);
    }

    private void SetFrame(int frameIndex)
    {
        var frame = document.Frames[frameIndex];
        ResourceVideoBitmapFactory.CopyFrame(writableBitmap, frame);
        CurrentFrameIndex = frameIndex;
    }

    private TimeSpan GetFrameDelay(int frameIndex)
    {
        if (FrameCount == 0)
        {
            return MinimumFrameDelay;
        }

        var delay = document.Frames[frameIndex].DelayInNanoseconds;
        if (delay <= 0)
        {
            return MinimumFrameDelay;
        }

        var ticks = Math.Max(MinimumFrameDelay.Ticks, delay / 100);
        return TimeSpan.FromTicks(ticks);
    }
}
