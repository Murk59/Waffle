﻿namespace Waffle.Sample.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http.Filters;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Waffle.Sample.Orders;
    using System.ComponentModel.DataAnnotations;
    
    public class OrdersController : ApiController
    {
        // POST api/orders
        [ValidateModelState]
        [AcceptVerbs("GET", "HEAD")]
        public Task Post([FromUri]PlaceOrder placeOrderCommand)
        {
            return GlobalProcessorConfiguration.DefaultProcessor.ProcessAsync(placeOrderCommand);
        }

        //// GET api/orders
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}
    }

    /// <summary>
    /// Ensures the ModelState is valid before to execute the action.
    /// Otherwise it return an HTTP 400 error with the ModelState.
    /// </summary>
    public class ValidateModelStateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            if (!actionContext.ModelState.IsValid)
            {
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, actionContext.ModelState);
            }
            
            base.OnActionExecuting(actionContext);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);
        }
    }
}