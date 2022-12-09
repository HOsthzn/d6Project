using System;
using System.Globalization;
using System.Web.Mvc;

namespace d6Invoice.Models;

//this file contains all custom model binders

internal class DecimalBinder : DefaultModelBinder
{
  public override object BindModel( ControllerContext controllerContext, ModelBindingContext bindingContext )
  {
    ValueProviderResult valueResult = bindingContext.ValueProvider.GetValue( bindingContext.ModelName );
    ModelState          modelState  = new ModelState { Value = valueResult };
    object              actualValue = null;
    try
    {
      actualValue = Convert.ToDecimal( valueResult.AttemptedValue, CultureInfo.InvariantCulture );
    }
    catch ( FormatException e )
    {
      modelState.Errors.Add( e );
    }

    bindingContext.ModelState.Add( bindingContext.ModelName, modelState );
    return actualValue;
  }

  public class EfModelBinderProvider : IModelBinderProvider
  {
    public IModelBinder GetBinder( Type modelType )
    {
      return modelType == typeof( decimal ) ? new DecimalBinder() : null;
    }
  }
}