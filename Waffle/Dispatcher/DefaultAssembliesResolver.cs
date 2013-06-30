﻿namespace Waffle.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Provides an implementation of <see cref="IAssembliesResolver"/> with no external dependencies.
    /// </summary>
    public class DefaultAssembliesResolver : IAssembliesResolver
    { 
        /// <summary>
        /// Returns a list of assemblies available for the application.
        /// </summary>
        /// <returns>A <see cref="Collection{T}"/> of assemblies.</returns>
        public ICollection<Assembly> GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().ToList();
        }
    }
}