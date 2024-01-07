using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Commons.Extensions
{
    public static class ContainerProviderExtensions
    {
        #region Public Methods

        public static IEnumerable<T> Resolve<T, TEnum>(this IContainerProvider containerProvider)
            where TEnum : Enum
        {
            if (containerProvider != default)
            {
                var names = Enum.GetValues(typeof(TEnum))
                    .Cast<TEnum>().Select(t => t.ToString()).ToArray();

                foreach (var name in names)
                {
                    var result = containerProvider.Resolve<T>(name);

                    yield return result;
                }
            }
        }

        #endregion Public Methods
    }
}