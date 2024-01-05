using SportDJ.Services.VolumeService.Enums;
using System.Collections.Generic;

namespace SportDJ.Services.VolumeService.Models
{
    internal sealed class AudioDevice
    {
        #region Internal Fields

        internal bool ha;

        #endregion Internal Fields

        #region Public Constructors

        public AudioDevice(string id, AudioDeviceState state, IDictionary<string, object> properties)
        {
            Id = id;
            State = state;
            Properties = properties;
        }

        #endregion Public Constructors

        #region Public Properties

        public string ContainerId
        {
            get
            {
                const string PKEY_Devices_ContainerId = "{8c7ed206-3f8a-4827-b3ab-ae9e1faefc6c} 2";
                Properties.TryGetValue(PKEY_Devices_ContainerId, out var value);
                return string.Format("{0}", value);
            }
        }

        public string Description
        {
            get
            {
                const string PKEY_Device_DeviceDesc = "{a45c254e-df1c-4efd-8020-67d146a850e0} 2";
                Properties.TryGetValue(PKEY_Device_DeviceDesc, out var value);
                return string.Format("{0}", value);
            }
        }

        public string EnumeratorName
        {
            get
            {
                const string PKEY_Device_EnumeratorName = "{a45c254e-df1c-4efd-8020-67d146a850e0} 24";
                Properties.TryGetValue(PKEY_Device_EnumeratorName, out var value);
                return string.Format("{0}", value);
            }
        }

        public string FriendlyName
        {
            get
            {
                const string DEVPKEY_Device_FriendlyName = "{a45c254e-df1c-4efd-8020-67d146a850e0} 14";
                Properties.TryGetValue(DEVPKEY_Device_FriendlyName, out var value);
                return string.Format("{0}", value);
            }
        }

        public string Id { get; private set; }

        public string InterfaceFriendlyName
        {
            get
            {
                const string DEVPKEY_DeviceInterface_FriendlyName = "{026e516e-b814-414b-83cd-856d6fef4822} 2";
                Properties.TryGetValue(DEVPKEY_DeviceInterface_FriendlyName, out var value);
                return string.Format("{0}", value);
            }
        }

        public IDictionary<string, object> Properties { get; private set; }

        public AudioDeviceState State { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public override string ToString()
        {
            return FriendlyName;
        }

        #endregion Public Methods
    }
}