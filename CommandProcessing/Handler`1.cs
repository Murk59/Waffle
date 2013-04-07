﻿namespace CommandProcessing
{    
    /// <summary>
    /// Represents the command handler. 
    /// It is responsible to do the real processing.
    /// </summary>
    /// <remarks>
    /// This override is a result-less handler.
    /// </remarks>
    /// <typeparam name="TCommand">The command type.</typeparam>
    public abstract class Handler<TCommand> : Handler, IHandler<TCommand, EmptyResult>
        where TCommand : ICommand
    {
        EmptyResult IHandler<TCommand, EmptyResult>.Handle(TCommand command)
        {
            this.Handle(command);
            return EmptyResult.Instance;
        }

        /// <summary>
        /// Handle the command.
        /// </summary>
        /// <param name="command">The <see cref="ICommand"/> to process.</param>
        public abstract void Handle(TCommand command);

        /// <summary>
        /// Handle the command.
        /// </summary>
        /// <remarks>You should prefer the result-less method.</remarks>
        /// <param name="command">The <see cref="ICommand"/> to process.</param>
        /// <returns>The result object.</returns>
        public override object Handle(ICommand command)
        {
            return ((IHandler<TCommand, EmptyResult>)this).Handle((TCommand)command);
        }
    }
}