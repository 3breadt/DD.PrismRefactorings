// <copyright file="SampleViewModelWithCtorAndFields.cs" company="Daniel Dreibrodt">
// Copyright (c) Daniel Dreibrodt. All rights reserved.
// </copyright>

namespace DD.PrismRefactorings.Tests.ViewModels
{
    using Prism.Mvvm;

    /// <summary>
    /// A sample view model.
    /// </summary>
    /// <seealso cref="Prism.Mvvm.BindableBase" />
    public class SampleViewModelWithCtorAndFields : BindableBase
    {
        private readonly string name = "Mike";
        private readonly int answer = 42;

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleViewModelWithCtorAndFields"/> class.
        /// </summary>
        public SampleViewModelWithCtorAndFields()
        {
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }
    }
}
