using Prism.Ioc;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public class ControllerViewModel
        : CellViewModel
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;

        private ITrackService trackService;

        #endregion Private Fields

        #region Public Constructors

        public ControllerViewModel(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Initialize(Controller controller)
        {
            base.Initialize(
                cell: controller);

            this.trackService = containerProvider.Resolve<ITrackService>();
            // trackService.Initialize()
        }

        #endregion Public Methods
    }
}