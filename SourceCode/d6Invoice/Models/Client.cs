using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace d6Invoice.Models;

public class Client
{
  public int Id { get; set; }

  [Required]
  [DisplayName( "Name" )]
  [MaxLength( 256 )]
  public string Name { get; set; }

  [DisplayName( "Company Name" )]
  [MaxLength( 256 )]
  public string CompanyName { get; set; }

  [DisplayName( "Street Address" )]
  [MaxLength( 256 )]
  public string StreetAddress { get; set; }

  [DisplayName( "City, State, Zip" )]
  [MaxLength( 256 )]
  public string CityStateZip { get; set; }

  [DisplayName( "Phone" )]
  [MaxLength( 256 )]
  public string Phone { get; set; }
}

public class ClientIndexViewModel
{
  public int                   PageCount  { get; set; }
  public int?                  Page       { get; set; }
  public int?                  RecPerPage { get; set; } = 10;
  public IEnumerable< Client > Clients    { get; set; } = new List< Client >();
}