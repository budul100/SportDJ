using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Prism.Mvvm;
using System.Collections.Generic;

namespace SportDJ.Modules.ContentModule.Helpers
{
    public class ViewLocator
        : IDataTemplate
    {
        #region Public Properties

        public bool SupportsRecycling => false;

        [Content]
        public Dictionary<string, IDataTemplate> Templates { get; } = new Dictionary<string, IDataTemplate>();

        #endregion Public Properties

        #region Public Methods

        Control ITemplate<object, Control>.Build(object data)
        {
            return Templates[data.GetType().Name].Build(data);
        }

        public bool Match(object data)
        {
            return data is BindableBase;
        }

        #endregion Public Methods
    }
}