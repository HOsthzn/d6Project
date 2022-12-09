using d6Invoice.Models;
using d6Invoice.Utilities;
using System.Collections.Generic;
using System.Collections;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System;
using System.Web.Http;

namespace d6Invoice.Controllers;

public class InvoiceController : Controller
{
  private readonly AdoNet _net;

  public InvoiceController()
  {
    _net = new AdoNet( ConfigurationManager.ConnectionStrings[ "DefaultConnection" ].ConnectionString );
  }

  //GET Invoice/index
  public ActionResult Index()
  {
    SetViewBags();
    return View();
  }

  public void SetViewBags()
  {
    ViewBag.Clients  = _net.Stp< Client >( "Clients_GetAll", null );
    ViewBag.Products = _net.Stp< Product >( "Product_GetAll", null );
  }

  //GET Invoice/GetInvoices
  public async Task< ActionResult > GetInvoices( int? page, int? recPerPage, DateTime? date, int? clientId )
  {
    //Get all Client records from the DB
    Hashtable parameters = new()
                           {
                             { "@page", page }, { "@recPerPage", recPerPage }, { "@date", date }
                           , { "@clientId", clientId }
                           };

    InvoiceIndexViewModel result = new()
                                   {
                                     Page       = page
                                   , RecPerPage = recPerPage
                                   , Invoices = ( await _net.StpAsync< InvoiceHeader >( "Invoice_Get", parameters ) )
                                       .Select( ih => new InvoiceHeaderViewModel
                                                      {
                                                        Id      = ih.Id, ClientId = ih.ClientId
                                                      , Date    = ih.Date.ToString( "D" )
                                                      , DueDate = ih.DueDate.ToString( "D" ), Status = ih.Status
                                                      , Total   = ih.Total, IsPaid                   = ih.IsPaid
                                                      } )
                                   , PageCount = ( await _net.StpAsync< PageCount >( "Invoice_GetPageCount"
                                                  , new Hashtable { { "@recPerPage", recPerPage } } )
                                                 ).First()
                                                  .Count
                                   };

    return Json( result, JsonRequestBehavior.AllowGet );
  }

  //GET Invoice/details
  public async Task< ActionResult > Details( int id )
  {
    Hashtable     parameters = new() { { "@Id", id } };
    InvoiceHeader result     = ( await _net.StpAsync< InvoiceHeader >( "Invoice_GetDetails", parameters ) ).First();

    result.Client
      = ( await _net.StpAsync< Client >( "Client_GetDetails", new Hashtable { { "@Id", result.ClientId } } ) )
      .First();
    result.InvoiceDetails
      = await _net.StpAsync< InvoiceDetails >( "InvoiceDetails_Get"
                                            , new Hashtable { { "@InvoiceHeaderId", result.Id } } );

    foreach ( InvoiceDetails resultInvoiceDetail in result.InvoiceDetails )
    {
      resultInvoiceDetail.Products
        = ( await _net.StpAsync< Product >( "Product_GetDetails"
                                         , new Hashtable { { "@Id", resultInvoiceDetail.ProductId } } ) ).First();
    }

    return View( result );
  }

  //GET Invoice/create
  public ActionResult Create()
  {
    SetViewBags();
    return View( new InvoiceHeader() );
  }

  //POST Invoice/create
  [System.Web.Mvc.HttpPost]
  [ValidateAntiForgeryToken]
  public async Task< ActionResult > Create(
    [Bind( Include
             = "Date, Client, ClientId, DueDate, InvoiceDetails, Subtotal, Taxable, TaxRate, TaxDue, Other, Total" )]
    InvoiceHeader model )
  {
    if ( !ModelState.IsValid )
    {
      SetViewBags();
      //reset all values to prevent unwanted values and risks
      ModelState.AddModelError( "", "Please recheck all products and values." );
      model = new InvoiceHeader() { Subtotal = 0, Total = 0, Taxable = 0, TaxDue = 0 };
      return View( model );
    }

    try
    {
      Hashtable HeadParameters = new()
                                 {
                                   { "@Id", model.Id }, { "@Date", model.Date }, { "@DueDate", model.DueDate }
                                 , { "@ClientId", model.ClientId }, { "@Subtotal", model.Subtotal }
                                 , { "@Taxable", model.Taxable }, { "@TaxRate", model.TaxRate }
                                 , { "@TaxDue", model.TaxDue }
                                 , { "@Other", model.Other }, { "@Total", model.Total }
                                 };

      List< InvoiceDetails > details = model.InvoiceDetails;
      model                = ( await _net.StpAsync< InvoiceHeader >( "Invoice_AddUpdate", HeadParameters ) ).First();
      model.InvoiceDetails = new List< InvoiceDetails >();

      foreach ( Hashtable DetParameters in from invoiceDetails in details
                                           select new Hashtable()
                                                  {
                                                    { "@Id", invoiceDetails.Id }, { "@InvoiceHeaderId", model.Id }
                                                  , { "@ProductId", invoiceDetails.ProductId }
                                                  , { "@Quantity", invoiceDetails.Quantity }
                                                  , { "@Taxed", invoiceDetails.Taxed }
                                                  } )
      {
        model.InvoiceDetails.Add( ( await _net.StpAsync< InvoiceDetails >( "InvoiceDetails_AddUpdate", DetParameters ) )
                                 .First() );
      }

      return RedirectToAction( "Details", new { id = model.Id } );
    }
    catch ( Exception e )
    {
      ModelState.AddModelError( "", e.Message );
      return View( model );
    }
  }

  //GET Invoice/edit
  public async Task< ActionResult > Edit( int id )
  {
    Hashtable     parameters = new() { { "@Id", id } };
    InvoiceHeader result     = ( await _net.StpAsync< InvoiceHeader >( "Invoice_GetDetails", parameters ) ).First();

    result.Client
      = ( await _net.StpAsync< Client >( "Client_GetDetails", new Hashtable { { "@Id", result.ClientId } } ) ).First();
    result.InvoiceDetails
      = await _net.StpAsync< InvoiceDetails >( "InvoiceDetails_Get", new() { { "@InvoiceHeaderId", result.Id } } );

    foreach ( InvoiceDetails resultInvoiceDetail in result.InvoiceDetails )
    {
      resultInvoiceDetail.Products
        = ( await _net.StpAsync< Product >( "Product_GetDetails"
                                         , new Hashtable { { "@Id", resultInvoiceDetail.ProductId } } ) ).First();
    }

    SetViewBags();
    return View( result );
  }

  //POST Invoice/edit
  [System.Web.Mvc.HttpPost]
  [ValidateAntiForgeryToken]
  public async Task< ActionResult > Edit(
    [Bind( Include
             = "Id, Date, Client, ClientId, DueDate, InvoiceDetails, Subtotal, Taxable, TaxRate, TaxDue, Other, Total" )]
    InvoiceHeader model )
  {
    if ( !ModelState.IsValid )
    {
      SetViewBags();
      ModelState.AddModelError( "", "Please recheck all products and values." );
      return View( model );
    }

    if ( model.Status != 0 )
    {
      ModelState.AddModelError( "", "This invoice is no-longer in an active state so it my not be edited " );

      return View( model );
    }

    try
    {
      Hashtable HeadParameters = new()
                                 {
                                   { "@Id", model.Id }, { "@Date", model.Date }, { "@DueDate", model.DueDate }
                                 , { "@ClientId", model.ClientId }, { "@Subtotal", model.Subtotal }
                                 , { "@Taxable", model.Taxable }, { "@TaxRate", model.TaxRate }
                                 , { "@TaxDue", model.TaxDue }
                                 , { "@Other", model.Other }, { "@Total", model.Total }
                                 };
      List< InvoiceDetails > details = model.InvoiceDetails;
      model                = ( await _net.StpAsync< InvoiceHeader >( "Invoice_AddUpdate", HeadParameters ) ).First();
      model.InvoiceDetails = new List< InvoiceDetails >();

      foreach ( Hashtable DetParameters in from invoiceDetails in details
                                           select new Hashtable()
                                                  {
                                                    { "@Id", invoiceDetails.Id }, { "@InvoiceHeaderId", model.Id }
                                                  , { "@ProductId", invoiceDetails.ProductId }
                                                  , { "@Quantity", invoiceDetails.Quantity }
                                                  , { "@Taxed", invoiceDetails.Taxed }
                                                  } )
      {
        model.InvoiceDetails.Add( ( await _net.StpAsync< InvoiceDetails >( "InvoiceDetails_AddUpdate", DetParameters ) )
                                 .First() );
      }


      return RedirectToAction( "Details", new { id = model.Id } );
    }
    catch ( Exception e )
    {
      ModelState.AddModelError( "", e.Message );
      return View( model );
    }
  }

  public ActionResult PaidInvoice( int id )
  {
    try
    {
      _net.InLineStp( $"UPDATE InvoiceHeader SET IsPaid = 1 where Id = {id}", null );
      return RedirectToAction( "Details", new { id = id } );
    }
    catch ( Exception e )
    {
      ModelState.AddModelError( "", e.Message );
      return RedirectToAction( "Details", new { id = id }  );
    }
  }

  public ActionResult CancelInvoice( int id )
  {
    _net.InLineStp( $"UPDATE InvoiceHeader SET Status = 1 where Id = {id}", null );
    return RedirectToAction( "Details", new { id = id } );
  }

  public ActionResult SendInvoice( int id )
  {
    try
    {
      //the idea behind this function is to set the invoice status to 2 (Invoiced)
      //the invoice will then be both printed and sent via email to the client

      _net.InLineStp( "UPDATE InvoiceHeader SET Status = 2 where Id = @Id", new Hashtable { { "@Id", id } } );
      return RedirectToAction( "Details", new { id = id } );
    }
    catch ( Exception e )
    {
      ModelState.AddModelError( "", e.Message );
      return View( "Details" );
    }
  }
}