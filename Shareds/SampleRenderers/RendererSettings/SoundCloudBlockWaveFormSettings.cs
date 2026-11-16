using SkiaSharp;

namespace NAudio.WaveFormRenderer.RendererSettings;

public class SoundCloudBlockWaveFormSettings
    : WaveFormRendererSettings
{
    #region Private Fields

    private readonly SKColor topSpacerStartColor;
    private SKShader bottomShader;
    private SKShader bottomSpacerShader;
    private int lastBottomHeight;
    private int lastTopHeight;
    private SKShader topShader;
    private SKShader topSpacerShader;

    #endregion Private Fields

    #region Public Constructors

    public SoundCloudBlockWaveFormSettings(SKColor topPeakColor, SKColor topSpacerStartColor, SKColor bottomPeakColor, SKColor bottomSpacerColor)
    {
        this.topSpacerStartColor = topSpacerStartColor;
        topShader = SKShader.CreateColor(topPeakColor);
        bottomShader = SKShader.CreateColor(bottomPeakColor);
        bottomSpacerShader = SKShader.CreateColor(bottomSpacerColor);
        PixelsPerPeak = 4;
        SpacerPixels = 2;
        BackgroundColor = SKColors.White;
        TopSpacerGradientStartColor = SKColors.White;
    }

    #endregion Public Constructors

    #region Public Properties

    public override SKShader BottomPeakShader
    {
        get { return bottomShader; }
        set { bottomShader = value; }
    }

    public override SKShader BottomSpacerShader
    {
        get { return bottomSpacerShader; }
        set { bottomSpacerShader = value; }
    }

    public override SKShader TopPeakShader
    {
        get { return topShader; }
        set { topShader = value; }
    }

    public SKColor TopSpacerGradientStartColor { get; set; }

    public override SKShader TopSpacerShader
    {
        get
        {
            if (topSpacerShader == null || lastBottomHeight != BottomHeight || lastTopHeight != TopHeight)
            {
                topSpacerShader = CreateGradientShader(TopHeight, TopSpacerGradientStartColor, topSpacerStartColor);
                lastBottomHeight = BottomHeight;
                lastTopHeight = TopHeight;
            }
            return topSpacerShader;
        }
        set { topSpacerShader = value; }
    }

    #endregion Public Properties
}