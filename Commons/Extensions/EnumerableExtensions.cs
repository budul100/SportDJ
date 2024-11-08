using System;
using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Commons.Extensions
{
    public static class EnumerableExtensions
    {
        #region Public Methods

        public static void OnEach<T>(this IEnumerable<T> instances, Action<T> action)
            where T : class
        {
            if (action != default
                && instances?.Any() == true)
            {
                var relevants = instances.ToArray();

                for (var index = 0; index < relevants.Length; index++)
                {
                    if (relevants[index] != default)
                    {
                        action.Invoke(relevants[index]);
                    }
                }
            }
        }

        #endregion Public Methods
    }
}