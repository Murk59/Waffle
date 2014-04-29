﻿namespace Waffle.Unity.Tests
{
    using Microsoft.Practices.Unity;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using Waffle;
    using Waffle.Commands;
    using Xunit;

    public sealed class CommandProcessorWithUnityFixture : IDisposable
    {
        private readonly ICollection<IDisposable> disposableResources = new Collection<IDisposable>();

        private readonly ProcessorConfiguration configuration = new ProcessorConfiguration();
        
        private readonly IUnityContainer container = new UnityContainer();

        private readonly Mock<ICommandHandlerTypeResolver> resolver = new Mock<ICommandHandlerTypeResolver>();

        [Fact]
        public async void WhenProcessingValidCommandThenCommandIsProcessed()
        {
            // Arrange
            this.resolver
                .Setup(r => r.GetCommandHandlerTypes(It.IsAny<IAssembliesResolver>()))
                .Returns(new[] { typeof(ValidCommandHandler) });
            this.configuration.Services.Replace(typeof(ICommandHandlerTypeResolver), this.resolver.Object);
            var service = new Mock<ISimpleService>();
            
            this.container.RegisterInstance(service.Object);
            MessageProcessor processor = this.CreateTestableProcessor();
            ValidCommand command = new ValidCommand();

            // Act
            var result = await processor.ProcessAsync<string>(command);

            // Assert
            Assert.Equal("OK", result.Value);
            service.Verify(s => s.Execute(), Times.Once());
        }
        
        [Fact]
        public async Task WhenProcessingCommandWithoutResultThenCommandIsProcessed()
        {
            // Arrange
            this.resolver
                .Setup(r => r.GetCommandHandlerTypes(It.IsAny<IAssembliesResolver>()))
                .Returns(new[] { typeof(ValidCommandHandlerWithoutResult) });
            this.configuration.Services.Replace(typeof(ICommandHandlerTypeResolver), this.resolver.Object);
            var service = new Mock<ISimpleService>();

            this.container.RegisterInstance(service.Object);
            MessageProcessor processor = this.CreateTestableProcessor();
            ValidCommand command = new ValidCommand();

            // Act
            await processor.ProcessAsync(command);

            // Assert
            service.Verify(s => s.Execute(), Times.Once());
        }
        
        private MessageProcessor CreateTestableProcessor(ProcessorConfiguration config = null)
        {
            try
            {
                config = config ?? this.configuration;
                config.RegisterContainer(this.container);
                MessageProcessor processor = new MessageProcessor(config);
                this.disposableResources.Add(processor);
                config = null;
                return processor;
            }
            finally
            {
                if (config != null)
                {
                    config.Dispose();
                }
            }
        }

        public class InvalidCommand : ICommand
        {
            [Required]
            public string Property { get; set; }
        }

        public class ValidCommand : ICommand
        {
            public ValidCommand()
            {
                this.Property = "test";
            }

            [Required]
            public string Property { get; set; }
        }

        public class ValidCommandHandler : CommandHandler, ICommandHandler<ValidCommand, string>
        {
            public ValidCommandHandler(ISimpleService service)
            {
                this.Service = service;
            }

            public ISimpleService Service { get; set; }

            public string Handle(ValidCommand command)
            {
                this.Service.Execute();
                return "OK";
            }
        }

        public class ValidCommandHandlerWithoutResult : CommandHandler, ICommandHandler<ValidCommand>
        {
            public ValidCommandHandlerWithoutResult(ISimpleService service)
            {
                this.Service = service;
            }

            public ISimpleService Service { get; set; }

            public void Handle(ValidCommand command)
            {
                this.Service.Execute();
            }
        }

        public interface ISimpleService
        {
            void Execute();
        }

        public class SimpleService : ISimpleService
        {
            public void Execute()
            {
            }
        }

        public void Dispose()
        {
            this.configuration.Dispose();
            foreach (IDisposable disposable in this.disposableResources)
            {
                disposable.Dispose();
            }
        }
    }
}