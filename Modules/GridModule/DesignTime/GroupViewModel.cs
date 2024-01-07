using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.GridModule.DesignTime
{
    public class GroupViewModel
        : ViewModels.GroupViewModel
    {
        #region Private Fields

        private const int ElementsCount = 2;

        #endregion Private Fields

        #region Public Constructors

        public GroupViewModel()
            : base(default)
        {
            SetElements();
        }

        #endregion Public Constructors

        #region Private Methods

        private void SetElements()
        {
            for (int index = 0; index < ElementsCount; index += 2)
            {
                Tabs.Add(new GridViewModel());

                this.Initialize(new Group() { Column = index, Row = 0 });
            }
        }

        #endregion Private Methods
    }
}