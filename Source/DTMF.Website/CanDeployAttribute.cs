using System.Linq;
using System.Web;
using System.Web.Mvc;
using System;
using System.Web.Routing;
using DTMF;

/// <summary>
/// Custom authorization attribute/action filter
/// </summary>
/// <remarks></remarks>
public sealed class CanDeploy : AuthorizeAttribute
{

    protected override bool AuthorizeCore(HttpContextBase httpContext)
    {
        if (httpContext == null)
            throw new ArgumentException("HttpContext does not exist!");
        if (!Utilities.CanDeploy)
        {
            return false;
        }
        return true;
    }

    protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
    {
        base.HandleUnauthorizedRequest(filterContext);
        filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary {
			{
				"area",
				string.Empty
			},
			{
				"action",
				"Denied"
			},
			{
				"controller",
				"Home"
			}
		});
    }
}