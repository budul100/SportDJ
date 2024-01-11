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
            : base(default, default, false)
        {
            for (int index = 0; index < ElementsCount; index += 2)
            {
                Tabs.Add(new GridViewModel());
            }
        }

        #endregion Public Constructors
    }
}