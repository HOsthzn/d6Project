using System.Web.Script.Serialization;

namespace d6Invoice.Utilities;

public class JsSerializer
{
  private static JavaScriptSerializer Serializer { get; } = new();

  public static string Serialize( object             obj ) => Serializer.Serialize( obj );
  public static TModel Deserialize< TModel >( string obj ) => Serializer.Deserialize< TModel >( obj );
}