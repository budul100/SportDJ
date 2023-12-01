using System;
using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Commons.Extensions
{
    public static class EnumerableExtensions
    {
        #region Public Methods

        public static void OnEach<T>(this IEnumerable<T> instances, Action<T> action)
        {
            if (action != default
                && instances?.Any() == true)
            {
                foreach (var instance in instances)
                {
                    action(instance);
                }
            }
        }

        #endregion Public Methods
    }
}