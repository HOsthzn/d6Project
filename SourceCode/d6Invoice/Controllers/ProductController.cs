using d6Invoice.Models;
using d6Invoice.Utilities;
using System.Collections.Generic;
using System.Collections;
using System.Configuration;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Web.Mvc;

namespace d6Invoice.Controllers;

public class ProductController : Controller
{
  private readonly AdoNet _net;

  public ProductController()
  {
    _net = new AdoNet( ConfigurationManager.ConnectionStrings[ "DefaultConnection" ].ConnectionString );
  }

  //GET Product/index
  public ActionResult Index() => View();

  //GET Product/GetProducts
  public async Task< ActionResult > GetProducts( int? page, int? recPerPage )
  {
    //Get all Product records from the DB
    Hashtable parameters = new() { { "@page", page }, { "@recPerPage", recPerPage } };

    ProductIndexViewModel result = new()
                                   {
                                     Page       = page
                                   , RecPerPage = recPerPage
                                   , Products   = await _net.StpAsync< Product >( "Product_Get", parameters )
                                   , PageCount = ( await _net.StpAsync< PageCount >( "Product_GetPageCount"
                                                  , new Hashtable { { "@recPerPage", recPerPage } } )
                                                 ).First()
                                                  .Count
                                   };

    return Json( result, JsonRequestBehavior.AllowGet );
  }

  //Get Product/GetAll
  public async Task< ActionResult > GetAll()
    => Json( await _net.StpAsync< Product >( "Product_GetAll", null ), JsonRequestBehavior.AllowGet );

  //GET Product/details
  public async Task< ActionResult > Details( int id )
  {
    Hashtable              parameters = new() { { "@Id", id } };
    IEnumerable< Product > result     = await _net.StpAsync< Product >( "Product_GetDetails", parameters );

    return View( result.First() );
  }

  //GET Product/create
  public ActionResult Create() => View();

  //POST Product/create
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task< ActionResult > Create( [Bind( Include = "Description, Price" )] Product model )
  {
    if ( !ModelState.IsValid ) return View( model );

    try
    {
      Hashtable parameters = new()
                             {
                               { "@Id", model.Id }
                             , { "@Description", model.Description }
                             , { "@Price", model.Price }
                             };

      await _net.StpAsync< Product >( "Product_AddUpdate", parameters );

      return RedirectToAction( "Index" );
    }
    catch ( Exception e )
    {
      ModelState.AddModelError( "", e.Message );
      return View( model );
    }
  }

  //GET Product/edit
  public async Task< ActionResult > Edit( int id )
  {
    Hashtable              parameters = new() { { "@Id", id } };
    IEnumerable< Product > result     = await _net.StpAsync< Product >( "Product_GetDetails", parameters );

    return View( result.First() );
  }

  //POST Product/edit
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task< ActionResult > Edit( [Bind( Include = "Id, Description, Price" )] Product model )
  {
    if ( !ModelState.IsValid ) return View( model );

    try
    {
      Hashtable parameters = new()
                             {
                               { "@Id", model.Id }
                             , { "@Description", model.Description }
                             , { "@Price", model.Price }
                             };

      await _net.StpAsync< Product >( "Product_AddUpdate", parameters );

      return RedirectToAction( "Index" );
    }
    catch ( Exception e )
    {
      ModelState.AddModelError( "", e.Message );
      return View( model );
    }
  }

  //GET Product/delete
  public async Task< ActionResult > Delete( int id )
  {
    Hashtable              parameters = new() { { "@Id", id } };
    IEnumerable< Product > result     = await _net.StpAsync< Product >( "Product_GetDetails", parameters );

    return View( result.First() );
  }

  //POST Product/delete
  [HttpPost]
  [ActionName( "Delete" )]
  [ValidateAntiForgeryToken]
  public ActionResult DeleteConfirmed( int id )
  {
    _net.InLineStp( "DELETE FROM dbo.Products WHERE Id = @Id", new Hashtable { { "@Id", id } } );
    return RedirectToAction( "Index" );
  }
}