using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using d6Invoice.Models;
using d6Invoice.Utilities;

namespace d6Invoice.Controllers;

public class ClientController : Controller
{
  private readonly AdoNet _net;

  public ClientController()
  {
    _net = new AdoNet( ConfigurationManager.ConnectionStrings[ "DefaultConnection" ].ConnectionString );
  }

  //GET Client/index
  public ActionResult Index() => View();

  //GET Client/GetClients
  public async Task< ActionResult > GetClients( int? page, int? recPerPage )
  {
    //Get all Client records from the DB
    Hashtable parameters = new() { { "@page", page }, { "@recPerPage", recPerPage } };

    ClientIndexViewModel result = new()
                                  {
                                    Page       = page
                                  , RecPerPage = recPerPage
                                  , Clients    = await _net.StpAsync< Client >( "Client_Get", parameters )
                                  , PageCount = ( await _net.StpAsync< PageCount >( "Client_GetPageCount"
                                                 , new Hashtable { { "@recPerPage", recPerPage } } )
                                                ).First()
                                                 .Count
                                  };

    return Json( result, JsonRequestBehavior.AllowGet );
  }

  //GET Client/GetClients
  public async Task< ActionResult > GetClientDetails( int id )
  {
    return Json( ( await _net.StpAsync< Client >( "Client_GetDetails", new Hashtable { { "@Id", id } } ) ).First(), JsonRequestBehavior.AllowGet );
  }

  //GET Client/details
  public async Task< ActionResult > Details( int id )
  {
    Hashtable             parameters = new() { { "@Id", id } };
    IEnumerable< Client > result     = await _net.StpAsync< Client >( "Client_GetDetails", parameters );

    return View( result.First() );
  }

  //GET Client/create
  public ActionResult Create() => View();

  //POST Client/create
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task< ActionResult > Create( [Bind( Include = "Name, CompanyName, StreetAddress, CityStateZip, Phone" )] Client model )
  {
    if ( !ModelState.IsValid ) return View( model );

    try
    {
      Hashtable parameters = new()
                             {
                               { "@Id", model.Id }
                             , { "@Name", model.Name }
                             , { "@CompanyName", model.CompanyName }
                             , { "@StreetAddress", model.StreetAddress }
                             , { "@CityStateZip", model.CityStateZip }
                             , { "@Phone", model.Phone }
                             };

      await _net.StpAsync< Client >( "Client_AddUpdate", parameters );

      return RedirectToAction( "Index" );
    }
    catch ( Exception e )
    {
      ModelState.AddModelError( "", e.Message );
      return View( model );
    }
  }

  //GET Client/edit
  public async Task< ActionResult > Edit( int id )
  {
    Hashtable             parameters = new() { { "@Id", id } };
    IEnumerable< Client > result     = await _net.StpAsync< Client >( "Client_GetDetails", parameters );

    return View( result.First() );
  }

  //POST Client/edit
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task< ActionResult > Edit(
    [Bind( Include = "Id,Name, CompanyName, StreetAddress, CityStateZip, Phone" )]
    Client model )
  {
    if ( !ModelState.IsValid ) return View( model );

    try
    {
      Hashtable parameters = new()
                             {
                               { "@Id", model.Id }
                             , { "@Name", model.Name }
                             , { "@CompanyName", model.CompanyName }
                             , { "@StreetAddress", model.StreetAddress }
                             , { "@CityStateZip", model.CityStateZip }
                             , { "@Phone", model.Phone }
                             };

      await _net.StpAsync< Client >( "Client_AddUpdate", parameters );

      return RedirectToAction( "Index" );
    }
    catch ( Exception e )
    {
      ModelState.AddModelError( "", e.Message );
      return View( model );
    }
  }

  //GET Client/delete
  public async Task< ActionResult > Delete( int id )
  {
    Hashtable             parameters = new() { { "@Id", id } };
    IEnumerable< Client > result     = await _net.StpAsync< Client >( "Client_GetDetails", parameters );

    return View( result.First() );
  }

  //POST Client/delete
  [HttpPost]
  [ActionName( "Delete" )]
  [ValidateAntiForgeryToken]
  public ActionResult DeleteConfirmed( int id )
  {
    _net.InLineStp( "DELETE FROM dbo.Client WHERE Id = @Id", new Hashtable { { "@Id", id } } );
    return RedirectToAction( "Index" );
  }
}