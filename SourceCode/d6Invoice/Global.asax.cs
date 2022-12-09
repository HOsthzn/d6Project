using System.Globalization;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using d6Invoice.Models;

namespace d6Invoice;

public class MvcApplication : HttpApplication
{
  protected void Application_Start()
  {
    AreaRegistration.RegisterAllAreas();
    GlobalConfiguration.Configure( WebApiConfig.Register );
    FilterConfig.RegisterGlobalFilters( GlobalFilters.Filters );
    RouteConfig.RegisterRoutes( RouteTable.Routes );
    BundleConfig.RegisterBundles( BundleTable.Bundles );
    ModelBinders.Binders.Add(typeof(decimal), new DecimalBinder());
  }

  protected void Application_BeginRequest()
  {
    //this configuration is for binding decimal values
    CultureInfo currentCulture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
    currentCulture.NumberFormat.NumberDecimalSeparator   = ".";
    currentCulture.NumberFormat.NumberGroupSeparator     = " ";
    currentCulture.NumberFormat.CurrencyDecimalSeparator = ".";

    Thread.CurrentThread.CurrentCulture = currentCulture;
  }
}