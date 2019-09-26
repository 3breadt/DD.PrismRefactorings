// <copyright file="SampleViewModelWithCtor.cs" company="Daniel Dreibrodt">
// Copyright (c) Daniel Dreibrodt. All rights reserved.
// </copyright>

namespace DD.PrismRefactorings.Tests.ViewModels
{
    using Prism.Mvvm;

    /// <summary>
    /// A sample view model.
    /// </summary>
    /// <seealso cref="Prism.Mvvm.BindableBase" />
    public class SampleViewModelWithCtor : BindableBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SampleViewModelWithCtor"/> class.
        /// </summary>
        public SampleViewModelWithCtor()
        {
            this.Name = "Bla";
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }
    }
}
