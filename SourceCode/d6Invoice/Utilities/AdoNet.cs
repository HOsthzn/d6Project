using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace d6Invoice.Utilities;

public class AdoNet
{
  public AdoNet( string connectionString ) { ConnectionString = connectionString; }

  private string ConnectionString { get; }

  //This function will execute any SQL stored procedure and map it to an object
  public List< TModel > Stp< TModel >( string    stpName
                                     , Hashtable parameters )
    where TModel : class, new()
  {
    if ( string.IsNullOrEmpty( stpName ) ) throw new ArgumentNullException( nameof( stpName ) );
    List< TModel > models = new();
    try
    {
      //TransactionScope works the same as a sql TRANSACTION, the function never successfully executes until TransactionScope.Complete is called
      using TransactionScope transactionScope = new();
      using SqlConnection    connection       = new( ConnectionString );
      using SqlCommand       command          = new();
      //command configuration, set connection, type and the stored procedure to be executed
      command.Connection  = connection;
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = stpName;

      if ( parameters != null ) MapParameters( command, parameters );

      //open the connection to the dataBase
      connection.Open();
      using SqlDataReader reader = command.ExecuteReader( CommandBehavior.CloseConnection );
      try
      {
        if ( reader.HasRows )
          while ( reader.Read() )
          {
            TModel model = new();
            //use reflection to map the results to an object
            Dictionary< string, int > indexer = model.GetType()
                                                     .GetProperties()
                                                     .ToDictionary( propInfo => propInfo.Name
                                                                 , propInfo
                                                                     =>
                                                                   {
                                                                     if( propInfo.PropertyType ==  "".GetType() || !propInfo.PropertyType.IsClass || !propInfo.PropertyType.IsInterface)
                                                                       return reader.GetOrdinal( propInfo.Name );
                                                                     return -1;
                                                                   } );
            foreach ( KeyValuePair< string, int > keyValuePair in indexer.Where( keyValuePair => keyValuePair.Value != -1 ) )
            {
              model.GetType()
                   .GetProperty( keyValuePair.Key )
                   ?.SetValue( model, reader[ keyValuePair.Value ] is DBNull ? null : reader[ keyValuePair.Value ] );
            }

            models.Add( model );
          }
      }
      finally
      {
        //this step is only to make sure the connection was closed and the command disposed of correctly
        command.Dispose();
        if ( connection.State == ConnectionState.Open ) connection.Close();
      }

      // The Complete method commits the transaction. If an exception has been thrown,
      // Complete is not  called and the transaction is rolled back.
      transactionScope.Complete();
    }
    catch ( Exception e )
    {
      Console.WriteLine( e );
      throw;
    }

    return models;
  }

  //This function is exactly the same as the Stp function but executes asynchronously
  //(recommended: this is an I/O function)
  public async Task< List< TModel > > StpAsync< TModel >( string    stpName
                                                        , Hashtable parameters )
    where TModel : class, new()
  {
    if ( string.IsNullOrEmpty( stpName ) ) throw new ArgumentNullException( nameof( stpName ) );
    List< TModel > models = new();
    try
    {
      //TransactionScope works the same as a sql TRANSACTION, the function never successfully executes until TransactionScope.Complete is called
      using TransactionScope transactionScope = new( TransactionScopeAsyncFlowOption.Enabled );
      using SqlConnection    connection       = new( ConnectionString );
      using SqlCommand       command          = new();
      //command configuration, set connection, type and the stored procedure to be executed
      command.Connection  = connection;
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = stpName;

      if ( parameters != null ) MapParameters( command, parameters );

      //open the connection to the dataBase
      connection.Open();
      using SqlDataReader reader = await command.ExecuteReaderAsync( CommandBehavior.CloseConnection );
      try
      {
        if ( reader.HasRows )
          while ( await reader.ReadAsync() )
          {
            TModel model = new();
            //use reflection to map the results to an object
            Dictionary< string, int > indexer = model.GetType()
                                                     .GetProperties()
                                                     .ToDictionary( propInfo => propInfo.Name
                                                                 , propInfo =>
                                                                   {
                                                                     if( propInfo.PropertyType ==  "".GetType() || ( !propInfo.PropertyType.IsClass && !propInfo.PropertyType.IsInterface ))
                                                                       return reader.GetOrdinal( propInfo.Name );
                                                                     return -1;
                                                                   } );
            foreach ( KeyValuePair< string, int > keyValuePair in indexer.Where( keyValuePair => keyValuePair.Value != -1 ) )
            {
              model.GetType()
                   .GetProperty( keyValuePair.Key )
                   ?.SetValue( model, reader[ keyValuePair.Value ] is DBNull ? null : reader[ keyValuePair.Value ] );
            }

            models.Add( model );
          }
      }
      finally
      {
        //this step is only to make sure the connection was closed and the command disposed of correctly
        command.Dispose();
        if ( connection.State == ConnectionState.Open ) connection.Close();
      }

      // The Complete method commits the transaction. If an exception has been thrown,
      // Complete is not  called and the transaction is rolled back.
      transactionScope.Complete();
    }
    catch ( Exception e )
    {
      Console.WriteLine( e );
      throw;
    }

    return models;
  }

  //This function will execute any SQL stored procedure but does not map to an object
  public DataSet Stp( string stpName, Hashtable parameters )
  {
    DataSet dataSet = new();
    try
    {
      //TransactionScope works the same as a sql TRANSACTION, the function never successfully executes until TransactionScope.Complete is called
      using TransactionScope transactionScope = new();
      using SqlConnection    connection       = new( ConnectionString );
      using SqlCommand       command          = new();
      //command configuration, set connection, type and the stored procedure to be executed
      command.Connection  = connection;
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = stpName;

      if ( parameters != null ) MapParameters( command, parameters );

      //open the connection to the dataBase
      connection.Open();
      using SqlDataAdapter adapter = new();
      try
      {
        adapter.SelectCommand = command;
        adapter.Fill( dataSet );
      }
      finally
      {
        //this step is only to make sure the connection was closed and the command disposed of correctly
        command.Dispose();
        if ( connection.State == ConnectionState.Open ) connection.Close();
      }

      // The Complete method commits the transaction. If an exception has been thrown,
      // Complete is not  called and the transaction is rolled back.
      transactionScope.Complete();
    }
    catch ( Exception e )
    {
      Console.WriteLine( e );
      throw;
    }

    return dataSet;
  }

  //DataSets doesn't provide asynchronous functions, but putting the execution in a separate thread will prevent the main thread from locking up
  public async Task< DataSet > StpAsync( string    stpName
                                       , Hashtable parameters )
  {
    Task< DataSet > T = Task.Run( () => Stp( stpName, parameters ) );
    return await T;
  }

  //Execute T-SQL code in line (Directly from string)
  //Result indicates the number of affected rows
  public int InLineStp( string sql, Hashtable parameters )
  {
    if ( string.IsNullOrEmpty( sql ) ) throw new ArgumentNullException( nameof( sql ) );
    int result = 0;
    try
    {
      //TransactionScope works the same as a sql TRANSACTION, the function never successfully executes until TransactionScope.Complete is called
      using TransactionScope transactionScope = new();
      using SqlConnection    connection       = new( ConnectionString );
      using SqlCommand       command          = new();
      //command configuration, set connection, type and the SQL code to be executed
      command.Connection  = connection;
      command.CommandType = CommandType.Text;
      command.CommandText = sql;

      if ( parameters != null ) MapParameters( command, parameters );

      //open the connection to the dataBase
      connection.Open();

      try { result = command.ExecuteNonQuery(); }
      finally
      {
        //this step is only to make sure the connection was closed and the command disposed of correctly
        command.Dispose();
        if ( connection.State == ConnectionState.Open ) connection.Close();
      }

      // The Complete method commits the transaction. If an exception has been thrown,
      // Complete is not  called and the transaction is rolled back.
      transactionScope.Complete();
    }
    catch ( Exception e )
    {
      Console.WriteLine( e );
      throw;
    }

    return result;
  }

  private static void MapParameters( SqlCommand command, Hashtable parameters )
  {
    if ( command    == null ) throw new ArgumentNullException( nameof( command ) );
    if ( parameters == null ) throw new ArgumentNullException( nameof( parameters ) );
    //add the parameter values to the command
    foreach ( DictionaryEntry parameter in parameters )
      command.Parameters.AddWithValue( ( parameter.Key.ToString().StartsWith( "@" )
                                           ? parameter.Key
                                           : $@"{parameter.Key}" ).ToString()
                                    , parameter.Value ?? DBNull.Value );
  }
}