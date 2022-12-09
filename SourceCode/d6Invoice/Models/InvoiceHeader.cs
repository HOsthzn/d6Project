using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace d6Invoice.Models;

public class InvoiceHeader
{
  public                     int      Id       { get; set; }
  [Required]          public int      ClientId { get; set; }
  [Required]          public DateTime Date     { get; set; }
  [Required]          public DateTime DueDate  { get; set; }
  [DefaultValue( 0 )] public short    Status   { get; set; }
  [DefaultValue( 0 )] public decimal  Total    { get; set; }
  [DefaultValue( 0 )] public decimal  Subtotal { get; set; }
  [DefaultValue( 0 )] public decimal  Taxable  { get; set; }
  [DefaultValue( 0 )] public decimal  TaxRate  { get; set; }
  [DefaultValue( 0 )] public decimal  TaxDue   { get; set; }
  [DefaultValue( 0 )] public decimal  Other    { get; set; }
  public                     bool     IsPaid   { get; set; }

  public Client                 Client         { get; set; }
  public List< InvoiceDetails > InvoiceDetails { get; set; } = new();
}

public class InvoiceHeaderViewModel
{
  public                     int     Id       { get; set; }
  [Required]          public int     ClientId { get; set; }
  [Required]          public string  Date     { get; set; }
  [Required]          public string  DueDate  { get; set; }
  [DefaultValue( 0 )] public short   Status   { get; set; }
  [DefaultValue( 0 )] public decimal Total    { get; set; }
  public                     bool    IsPaid   { get; set; }
}

public class InvoiceIndexViewModel
{
  public int                                   PageCount  { get; set; }
  public int?                                  Page       { get; set; }
  public int?                                  RecPerPage { get; set; } = 10;
  public IEnumerable< InvoiceHeaderViewModel > Invoices   { get; set; } = new List< InvoiceHeaderViewModel >();
}