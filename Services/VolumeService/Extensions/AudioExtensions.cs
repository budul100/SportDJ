using SportDJ.Services.VolumeService.Enums;
using SportDJ.Services.VolumeService.Interfaces;
using SportDJ.Services.VolumeService.Models;
using SportDJ.Services.VolumeService.Structs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SportDJ.Services.VolumeService.Extensions
{
    internal static class AudioExtensions
    {
        #region Public Methods

        public static IList<AudioSession> GetAllSessions(int deviceNumber = -1)
        {
            List<AudioSession> result = new List<AudioSession>();

            foreach (IAudioSessionManager2 currentManager in GetAllAudioSessionManagers(deviceNumber))
            {
                IAudioSessionEnumerator sessionEnumerator;
                currentManager.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);

                for (int i = 0; i < count; i++)
                {
                    IAudioSessionControl ctl;
                    sessionEnumerator.GetSession(i, out ctl);
                    if (ctl == null) continue;

                    IAudioSessionControl2 ctl2 = ctl as IAudioSessionControl2;
                    if (ctl2 != null) result.Add(new AudioSession(ctl2));
                }

                Marshal.ReleaseComObject(sessionEnumerator);
                Marshal.ReleaseComObject(currentManager);
            }

            return result;
        }

        public static AudioSession GetProcessSession()
        {
            int id = Process.GetCurrentProcess().Id;

            foreach (AudioSession session in GetAllSessions())
            {
                if (session.ProcessId == id) return session;
                session.Dispose();
            }

            return null;
        }

        public static AudioDevice GetSpeakersDevice()
        {
            return CreateDevice(GetSpeakers());
        }

        #endregion Public Methods

        #region Private Methods

        private static AudioDevice CreateDevice(IMMDevice dev)
        {
            if (dev == null)
                return null;

            string id;
            dev.GetId(out id);
            DEVICE_STATE state;
            dev.GetState(out state);
            Dictionary<string, object> properties = new Dictionary<string, object>();
            IPropertyStore store;
            dev.OpenPropertyStore(STGM.STGM_READ, out store);
            if (store != null)
            {
                int propCount;
                store.GetCount(out propCount);
                for (int j = 0; j < propCount; j++)
                {
                    PROPERTYKEY pk;
                    if (store.GetAt(j, out pk) == 0)
                    {
                        PROPVARIANT value = new PROPVARIANT();
                        int hr = store.GetValue(ref pk, ref value);
                        object v = value.GetValue();
                        try
                        {
                            if (value.vt != VARTYPE.VT_BLOB) // for some reason, this fails?
                            {
                                PropVariantClear(ref value);
                            }
                        }
                        catch
                        {
                        }
                        string name = pk.ToString();
                        properties[name] = v;
                    }
                }
            }
            return new AudioDevice(id, (AudioDeviceState)state, properties);
        }

        private static List<IAudioSessionManager2> GetAllAudioSessionManagers(int deviceNumber)
        {
            List<IAudioSessionManager2> result = new List<IAudioSessionManager2>();

            List<IMMDevice> allDevices = GetAllSpeakers(deviceNumber);
            foreach (IMMDevice currentSpeakers in allDevices)
            {
                object o;
                if (currentSpeakers.Activate(typeof(IAudioSessionManager2).GUID, CLSCTX.CLSCTX_ALL, IntPtr.Zero, out o) != 0 || o == null)
                    return null;

                IAudioSessionManager2 currentManager = o as IAudioSessionManager2;
                if (currentManager != null) result.Add(currentManager);
            }

            return result;
        }

        private static List<IMMDevice> GetAllSpeakers(int deviceNumber)
        {
            List<IMMDevice> result = new List<IMMDevice>();

            try
            {
                IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                IMMDeviceCollection allSpeakers;

                deviceEnumerator.EnumAudioEndpoints(EDataFlow.eRender, DEVICE_STATE.ACTIVE, out allSpeakers);

                int speakersNumber = 0;
                allSpeakers.GetCount(out speakersNumber);

                int itemInd = deviceNumber >= 0 && deviceNumber < speakersNumber ? deviceNumber : 0;
                do
                {
                    IMMDevice currentSpeakers;
                    allSpeakers.Item(itemInd, out currentSpeakers);
                    result.Add(currentSpeakers);

                    itemInd++;
                } while ((deviceNumber < 0 || deviceNumber >= speakersNumber) && itemInd < speakersNumber);
            }
            catch
            { }

            return result;
        }

        private static IMMDevice GetSpeakers()
        {
            IMMDevice result = null;

            try
            {
                IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out result);
            }
            catch
            { }

            return result;
        }

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PROPVARIANT pvar);

        #endregion Private Methods
    }
}