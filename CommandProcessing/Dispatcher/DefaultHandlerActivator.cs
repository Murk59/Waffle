﻿namespace CommandProcessing.Dispatcher
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Threading;
    using CommandProcessing.Filters;

    /// <summary>
    /// Default implementation of an <see cref="IHandlerActivator"/>.
    /// A different implementation can be registered via the <see cref="T:CommandProcessing.Dependencies.IDependencyResolver"/>.
    /// </summary>
    public class DefaultHandlerActivator : IHandlerActivator
    {
        private readonly object cacheKey = new object();

        private Tuple<HandlerDescriptor, Func<ICommandHandler>> fastCache;

        /// <summary>
        /// Creates the <see cref="ICommandHandler"/> specified by <paramref name="descriptor"/> using the given <paramref name="request"/>.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <param name="descriptor">
        /// The controller descriptor.
        /// </param>
        /// <returns>
        /// The <see cref="ICommandHandler"/>.
        /// </returns>
        public ICommandHandler Create(HandlerRequest request, HandlerDescriptor descriptor)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (descriptor == null)
            {
                throw new ArgumentNullException("descriptor");
            }

            try
            {
                return this.TryCreate(request, descriptor);
            }
            catch (Exception innerException)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.DefaultHandlerActivator_ErrorCreatingHandler, descriptor.HandlerType.Name), innerException);
            }
        }

        /// <summary>
        /// Returns the handler instance from the dependency resolver if there is one registered
        /// else returns the activator that calls the default constructor for the give handlerType.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <param name="handlerType">
        /// The handler Type.
        /// </param>
        /// <param name="activator">
        /// The activator.
        /// </param>
        /// <returns>
        /// The <see cref="ICommandHandler"/>.
        /// </returns>
        private static ICommandHandler GetInstanceOrActivator(HandlerRequest request, Type handlerType, out Func<ICommandHandler> activator)
        {
            Contract.Assert(request != null);
            Contract.Assert(handlerType != null);

            // If dependency resolver returns handler object then use it.
            ICommandHandler instance = (ICommandHandler)request.GetDependencyScope().GetService(handlerType);
            if (instance != null)
            {
                activator = null;
                return instance;
            }

            // Otherwise create a delegate for creating a new instance of the type
            activator = TypeActivator.Create<ICommandHandler>(handlerType);
            return null;
        }

        private ICommandHandler TryCreate(HandlerRequest request, HandlerDescriptor descriptor)
        {
            Contract.Assert(request != null);
            Contract.Assert(descriptor != null);

            Func<ICommandHandler> activator;

            // First check in the local fast cache and if not a match then look in the broader 
            // HandlerDescriptor.Properties cache
            if (this.fastCache == null)
            {
                ICommandHandler handler = GetInstanceOrActivator(request, descriptor.HandlerType, out activator);
                if (handler != null)
                {
                    // we have a handler registered with the dependency resolver for this handler type                      
                    return handler;
                }

                Tuple<HandlerDescriptor, Func<ICommandHandler>> cacheItem = Tuple.Create(descriptor, activator);
                Interlocked.CompareExchange(ref this.fastCache, cacheItem, null);
            }
            else if (this.fastCache.Item1 == descriptor)
            {
                // If the key matches and we already have the delegate for creating an instance.
                activator = this.fastCache.Item2;
            }
            else
            {
                // If the key doesn't match then lookup/create delegate in the HandlerDescriptor.Properties for
                // that HandlerDescriptor instance
                object result;
                if (descriptor.Properties.TryGetValue(this.cacheKey, out result))
                {
                    activator = (Func<ICommandHandler>)result;
                }
                else
                {
                    ICommandHandler handler = GetInstanceOrActivator(request, descriptor.HandlerType, out activator);
                    if (handler != null)
                    {
                        // we have a handler registered with the dependency resolver for this handler type
                        return handler;
                    }

                    descriptor.Properties.TryAdd(this.cacheKey, activator);
                }
            }

            return activator();
        }
    }
}