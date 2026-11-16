using System;
using System.Collections.Generic;
using System.Text;
using Prism.Mvvm;

namespace SportDJ.Modules.DefinitionsModule.ViewModels;

public abstract class TabViewModel
    : BindableBase
{
    #region Public Properties

    public abstract string Title { get; }

    #endregion Public Properties
}