using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace d6Invoice.Models;

public class Product
{
  public int Id { get; set; }

  [Required]
  [DisplayName( "Description" )]
  [MaxLength( 256 )]
  public string Description { get; set; }

  [DefaultValue( 0 )]
  [DisplayName( "Price" )]
  public decimal Price { get; set; }
}

public class ProductIndexViewModel
{
  public int                    PageCount  { get; set; }
  public int?                   Page       { get; set; }
  public int?                   RecPerPage { get; set; } = 10;
  public IEnumerable< Product > Products   { get; set; } = new List< Product >();
}