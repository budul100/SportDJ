using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Extensions
{
    public static class StateTypeExtensions
    {
        #region Public Methods

        public static bool IsPlaying(this StateType stateType)
        {
            var result = stateType == StateType.Play || stateType == StateType.Cue;

            return result;
        }

        #endregion Public Methods
    }
}