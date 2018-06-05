using System;
using System.Linq.Expressions;

namespace JsonApiDotNetCore.Marten
{
    public static class ExpressionBuilder
    {
        public static Expression<Func<TClass, TProperty>> Build<TClass, TProperty>(string fieldName)
        {
            var param = Expression.Parameter(typeof(TClass));
            var field = Expression.PropertyOrField(param, fieldName);
            return Expression.Lambda<Func<TClass, TProperty>>(field, param);
        }
    }
}
