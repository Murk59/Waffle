﻿namespace CommandProcessing.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using CommandProcessing;
    using CommandProcessing.Tests.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class ProcessorConfigurationFixture : IDisposable
    {
        private readonly ICollection<IDisposable> disposableResources = new Collection<IDisposable>();

        private readonly ProcessorConfiguration defaultConfig = new ProcessorConfiguration();

        public ProcessorConfigurationFixture()
        {
            this.disposableResources.Add(this.defaultConfig);
        }

        [TestMethod]
        public void WhenCreatingProcessorWithDefaultCtorThenPropertiesAreDefined()
        {
            // Act
            ProcessorConfiguration config = new ProcessorConfiguration();
            this.disposableResources.Add(config);

            // Assert
            Assert.IsTrue(config.AbortOnInvalidCommand);
            Assert.IsFalse(config.ServiceProxyCreationEnabled);
            Assert.IsNotNull(config.DependencyResolver);
            Assert.IsNotNull(config.Services);
            Assert.IsNotNull(config.Initializer);
            Assert.AreEqual(0, config.Filters.Count);
        }

        [TestMethod]
        public void WhenSettingNullValuesThenThrowsArgumentNullException()
        {
            // Arrange
            ProcessorConfiguration config = new ProcessorConfiguration();
            this.disposableResources.Add(config);
            
            // Act & Assert
            ExceptionAssert.ThrowsArgumentNull(() => config.Initializer = null, "value");
            ExceptionAssert.ThrowsArgumentNull(() => config.DependencyResolver = null, "value");
        }

        [TestMethod]
        public void WhenRegisteringResourceToDisposeThenResouseIsDisposed()
        {
            // Arrange
            Mock<IDisposable> disposable = new Mock<IDisposable>();
            using (ProcessorConfiguration config = new ProcessorConfiguration())
            {
                disposable.Setup(d => d.Dispose());
                config.RegisterForDispose(disposable.Object);

                // Act
                config.Dispose();
            }

            // Assert
            disposable.Verify(d => d.Dispose(), Times.Once());
        }
        
        [TestCleanup]
        public void Dispose()
        {
            foreach (IDisposable disposable in this.disposableResources)
            {
                disposable.Dispose();
            }
        }
    }
}