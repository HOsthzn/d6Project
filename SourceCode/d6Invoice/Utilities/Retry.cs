using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace d6Invoice.Utilities;

public static class Retry
{
  private static List< Exception > Exceptions { get; } = new();

  /// <summary>
  ///   Retry
  /// </summary>
  /// <param name="action">the action to retry</param>
  /// <param name="retryInterval">retry intervals</param>
  /// <param name="maxAttemptCount">retry for this count</param>
  /// <returns>void</returns>
  /// <exception cref="AggregateException">List of exceptions for each retry</exception>
  public static void Do(
    Action   action
  , TimeSpan retryInterval
  , int      maxAttemptCount = 3 )
  {
    Do< object >( () =>
                 {
                   action();
                   return null;
                 }
               , retryInterval
               , maxAttemptCount );
  }

  /// <summary>
  ///   Retry
  /// </summary>
  /// <param name="action">the action to retry</param>
  /// <param name="retryInterval">retry intervals</param>
  /// <param name="maxAttemptCount">retry for this count</param>
  /// <returns>void</returns>
  /// <exception cref="AggregateException">List of exceptions for each retry</exception>
  public static async Task DoAsync(
    Func< Task > action
  , TimeSpan     retryInterval
  , int          maxAttemptCount = 3 )
  {
    await Do( async () => { await action(); }, retryInterval, maxAttemptCount );
  }

  /// <summary>
  ///   Retry
  /// </summary>
  /// <param name="action">the action to retry</param>
  /// <param name="retryInterval">retry intervals</param>
  /// <param name="maxAttemptCount">retry for this count</param>
  /// <typeparam name="T">return type</typeparam>
  /// <returns>T</returns>
  /// <exception cref="AggregateException">List of exceptions for each retry</exception>
  public static T Do< T >(
    Func< T > action
  , TimeSpan  retryInterval
  , int       maxAttemptCount = 3 )
  {
    for ( int attempted = 0; attempted < maxAttemptCount; attempted++ )
      try
      {
        if ( attempted > 0 ) Thread.Sleep( retryInterval );

        return action();
      }
      catch ( Exception ex ) { Exceptions.Add( ex ); }

    throw new AggregateException( Exceptions );
  }

  /// <summary>
  ///   Retry
  /// </summary>
  /// <param name="action">the action to retry</param>
  /// <param name="retryInterval">retry intervals</param>
  /// <param name="maxAttemptCount">retry for this count</param>
  /// <typeparam name="T">return type</typeparam>
  /// <returns>T</returns>
  /// <exception cref="AggregateException">List of exceptions for each retry</exception>
  public static async Task< T > DoAsync< T >(
    Func< Task< T > > action
  , TimeSpan          retryInterval
  , int               maxAttemptCount = 3 )
  {
    for ( int attempted = 0; attempted < maxAttemptCount; attempted++ )
      try
      {
        if ( attempted > 0 ) Thread.Sleep( retryInterval );

        return await action();
      }
      catch ( Exception ex ) { Exceptions.Add( ex ); }

    throw new AggregateException( Exceptions );
  }
}