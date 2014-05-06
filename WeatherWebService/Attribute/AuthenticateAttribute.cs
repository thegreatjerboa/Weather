using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Shared;

namespace WeatherWebService.Attribute
{
    public class AuthenticateFilterAttribute : ActionFilterAttribute    
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            if (filterContext.Result != null) return;

            var cookies = filterContext.HttpContext.Request.Cookies;
            var httpCookie = cookies.Get("APIKEY");
            if (httpCookie == null || !ConfigHelper.GetValidKeys().Contains(httpCookie.Value))
            {
                filterContext.Result = new EmptyResult();
            }
        }
    }
}