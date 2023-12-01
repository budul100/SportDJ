using System.Linq;
using Xunit;

namespace SportDJ.Tests.MasterServiceTests
{
    public class Tests
    {
        #region Public Methods

        [Fact]
        public void GetDevices()
        {
            var service = new Services.MasterService.Service();

            var devices = service.GetDevices();

            Assert.True(devices.Any());
        }

        #endregion Public Methods
    }
}