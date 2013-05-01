﻿namespace CommandProcessing.Tests.Descriptions
{
    using System;
    using System.Collections.Generic;
    using CommandProcessing;
    using CommandProcessing.Descriptions;
    using CommandProcessing.Filters;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class DefaultCommandExplorerFixture : IDisposable
    {
        private readonly ProcessorConfiguration config;

        private readonly Mock<IHandlerDescriptorProvider> descriptorProvider;

        public DefaultCommandExplorerFixture()
        {
            this.descriptorProvider = new Mock<IHandlerDescriptorProvider>(MockBehavior.Strict);

            this.config = new ProcessorConfiguration();
            this.config.Services.Replace(typeof(IHandlerDescriptorProvider), this.descriptorProvider.Object);
        }

        [TestMethod]
        public void WhenCreatingInstanceWithoutKnowMappingThenDescriptionIsEmpty()
        {
            // Assign
            this.descriptorProvider.Setup(selector => selector.GetHandlerMapping()).Returns(new Dictionary<Type, HandlerDescriptor>());

            // Act
            DefaultCommandExplorer explorer = new DefaultCommandExplorer(this.config);

            // Assert
            Assert.IsNotNull(explorer.Descriptions);
        }

        [TestMethod]
        public void WhenCreatingInstanceThenDescriptionIsDefined()
        {
            // Assign
            // TODO : Try to use AutoFixture instead
            Dictionary<Type, HandlerDescriptor> mapping = new Dictionary<Type, HandlerDescriptor> { { typeof(string), new HandlerDescriptor(this.config, typeof(SimpleCommand), typeof(Handler<SimpleCommand, string>)) } };
            this.descriptorProvider.Setup(selector => selector.GetHandlerMapping()).Returns(mapping);

            // Act
            DefaultCommandExplorer explorer = new DefaultCommandExplorer(this.config);

            // Assert
            Assert.IsNotNull(explorer.Descriptions);
            Assert.AreEqual(mapping.Count, explorer.Descriptions.Count);
        }

        [TestCleanup]
        public void Dispose()
        {
            this.config.Dispose();
        }
    }
}