using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Services.Configuration;

namespace d6Invoice.Models;

public class InvoiceDetails
{
  public int Id { get; set; }

  [Required] public int InvoiceHeaderId { get; set; }

  [Required] public int ProductId { get; set; }

  [DefaultValue( 0 )] public int Quantity { get; set; }

  public bool Taxed { get; set; } = false;

  public InvoiceHeader InvoiceHeader { get; set; }
  public Product Products { get; set; }
}