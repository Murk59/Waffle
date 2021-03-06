﻿namespace Waffle.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Transactions;
    using Waffle.Commands;
    using Waffle.Internal;

    /// <summary>
    /// Represents a filter to make handlers transactional.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public sealed class TransactionFilterAttribute : CommandHandlerFilterAttribute
    {
        private const string Key = "__TransactionFilterKey";

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionFilterAttribute"/> class.
        /// </summary>
        public TransactionFilterAttribute()
        {
            this.ScopeOption = TransactionScopeOption.Required;
            this.Timeout = TransactionManager.DefaultTimeout;
            this.IsolationLevel = IsolationLevel.Serializable;
        }

        /// <summary>
        /// Gets or sets the <see cref="TransactionScopeOption"/> for creating the transaction scope.
        /// </summary>
        /// <value>The <see cref="TransactionScopeOption"/> for creating the transaction scope.</value>
        public TransactionScopeOption ScopeOption { get; set; }

        /// <summary>
        ///  Gets or sets the timeout period for the transaction.
        /// </summary>
        /// <value>A <see cref="System.TimeSpan"/> value that specifies the timeout period for the transaction.</value>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Gets or sets the isolation level of the transaction.
        /// </summary>
        /// <value>A <see cref="System.Transactions.IsolationLevel"/> enumeration that specifies the isolation level of the transaction.</value>
        public IsolationLevel IsolationLevel { get; set; }

        /// <summary>
        /// Occurs before the handle method is invoked.
        /// </summary>
        /// <param name="handlerContext">The handler context.</param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The TransactionScope is wrapped in the stack for late disposing.")]
        public override void OnCommandExecuting(CommandHandlerContext handlerContext)
        {
            if (handlerContext == null)
            {
                throw Error.ArgumentNull("CommandHandlerContext");
            }

            Stack<TransactionScope> stack = GetStack(handlerContext);
            if (stack == null)
            {
                stack = new Stack<TransactionScope>();
                handlerContext.Items[Key] = stack;
            }

            TransactionOptions options = new TransactionOptions { Timeout = this.Timeout, IsolationLevel = this.IsolationLevel };
            TransactionScope transactionScope = new TransactionScope(this.ScopeOption, options);
            stack.Push(transactionScope);
        }

        /// <summary>
        /// Occurs after the handle method is invoked.
        /// </summary>
        /// <param name="handlerExecutedContext">The handler executed context.</param>
        public override void OnCommandExecuted(CommandHandlerExecutedContext handlerExecutedContext)
        {
            if (handlerExecutedContext == null)
            {
                throw Error.ArgumentNull("handlerExecutedContext");
            }

            Stack<TransactionScope> stack = GetStack(handlerExecutedContext.HandlerContext);
            if (stack != null && stack.Count > 0)
            {
                using (var scope = stack.Pop())
                {
                    if (null != scope && handlerExecutedContext.Response != null)
                    {
                        scope.Complete();
                    }
                }
            }
        }

        private static Stack<TransactionScope> GetStack(CommandHandlerContext context)
        {
            Contract.Requires(context != null);
            Contract.Requires(context.Items != null);

            object value;
            if (context.Items.TryGetValue(Key, out value))
            {
                return value as Stack<TransactionScope>;    
            }
            
            return null;
        }
    }
}
