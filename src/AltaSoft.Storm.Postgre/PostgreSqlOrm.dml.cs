using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm;

public static partial class PostgreSqlOrm
{
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static async Task<T?> FirstAsync<T>(this DbConnection self, uint partialLoadFlags, Expression<Func<T, bool>>? whereExpression, IEnumerable<int>? orderBy, QueryParameters? queryParameters, CancellationToken cancellationToken = default)
        where T : IDataBindable
    {
        throw new NotImplementedException();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static Task<List<T>> ListAsync<T>(this DbConnection self, uint partialLoadFlags, Expression<Func<T, bool>>? whereExpression, IEnumerable<int>? orderBy, QueryParameters? queryParameters, CancellationToken cancellationToken = default)
        where T : IDataBindable
    {
        throw new NotImplementedException();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static IAsyncEnumerable<T> StreamAsync<T>(this DbConnection self, uint partialLoadFlags, Expression<Func<T, bool>>? whereExpression, IEnumerable<int>? orderBy,
        QueryParameters? queryParameters, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : IDataBindable
    {
        throw new NotImplementedException();
    }
}
