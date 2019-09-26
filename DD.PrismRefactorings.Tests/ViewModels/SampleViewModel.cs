// <copyright file="SampleViewModel.cs" company="Daniel Dreibrodt">
// Copyright (c) Daniel Dreibrodt. All rights reserved.
// </copyright>

namespace DD.PrismRefactorings.Tests.ViewModels
{
    using Prism.Mvvm;

    /// <summary>
    /// A sample view model.
    /// </summary>
    /// <seealso cref="Prism.Mvvm.BindableBase" />
    public class SampleViewModel : BindableBase
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }
    }
}
