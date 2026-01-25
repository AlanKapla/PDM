using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> IncludeMultiple<T>(this IQueryable<T> query, params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes) where T : class
        {
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = include(query);
                }
            }

            return query;
        }
    }
}
