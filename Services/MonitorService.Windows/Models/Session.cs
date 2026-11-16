using NAudio.CoreAudioApi;

namespace SportDJ.Services.MonitorService.Windows.Models;

public class Session(AudioSessionControl control, float volumeInitial)
{
    #region Public Properties

    public float VolumeCurrent
    {
        get { return control.SimpleAudioVolume.Volume; }
        set { control.SimpleAudioVolume.Volume = value; }
    }

    public float VolumeInitial { get; } = volumeInitial;

    public float VolumeLower { get; set; }

    #endregion Public Properties
}