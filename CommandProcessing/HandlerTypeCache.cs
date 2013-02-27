﻿namespace CommandProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CommandProcessing.Dispatcher;
    using CommandProcessing.Services;

    internal sealed class HandlerTypeCache
    {
        private static readonly Type CommandHandlerType = typeof(ICommandHandler<,>);

        private readonly ProcessorConfiguration configuration;

        private readonly Lazy<Dictionary<Type, ILookup<Type, Type>>> cache;

        public HandlerTypeCache(ProcessorConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            this.configuration = configuration;
            this.cache = new Lazy<Dictionary<Type, ILookup<Type, Type>>>(this.InitializeCache);
        }

        internal Dictionary<Type, ILookup<Type, Type>> Cache
        {
            get
            {
                return this.cache.Value;
            }
        }

        public ICollection<Type> GetHandlerTypes(Type commandType)
        {
            if (commandType == null)
            {
                throw new ArgumentNullException("commandType");
            }

            HashSet<Type> matchingTypes = new HashSet<Type>();
            ILookup<Type, Type> lookup;
            if (this.cache.Value.TryGetValue(commandType, out lookup))
            {
                foreach (IGrouping<Type, Type> current in lookup)
                {
                    matchingTypes.UnionWith(current);
                }
            }

            return matchingTypes;
        }

        private static bool IsAssignableFromGenericType(Type givenType, Type genericType)
        {
            if (genericType == null)
            {
                return false;
            }

            if (genericType.IsGenericType && genericType.GetGenericTypeDefinition() == givenType)
            {
                return true;
            }

            Type baseType = genericType.BaseType;
            if (baseType == null)
            {
                return false;
            }

            return (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == givenType) || IsAssignableFromGenericType(baseType, givenType);
        }

        private static IEnumerable<IGrouping<Type, Type>> GetCommandType(Type handlerType)
        {
            return handlerType.GetInterfaces().Where(i => i.IsGenericType && IsAssignableFromGenericType(CommandHandlerType, i.GetGenericTypeDefinition())).GroupBy(i => i.GetGenericArguments()[0], i => handlerType);
        }

        private Dictionary<Type, ILookup<Type, Type>> InitializeCache()
        {
            IAssembliesResolver assembliesResolver = this.configuration.Services.GetAssembliesResolver();
            IHandlerTypeResolver handlerTypeResolver = this.configuration.Services.GetHandlerTypeResolver();
            ICollection<Type> handlerTypes = handlerTypeResolver.GetHandlerTypes(assembliesResolver);
            IEnumerable<IGrouping<Type, Type>> source = handlerTypes.SelectMany(t => GetCommandType(t));
            return source.ToDictionary((IGrouping<Type, Type> g) => g.Key, (IGrouping<Type, Type> g) => g.ToLookup((Type t) => t));
        }
    }
}