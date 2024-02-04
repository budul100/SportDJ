using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Extensions
{
    public static class TypeExtensions
    {
        #region Public Methods

        public static bool IsRunning(this PlayType playType)
        {
            return playType == PlayType.Playing || playType == PlayType.Stopping;
        }

        #endregion Public Methods
    }
}