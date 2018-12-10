using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Dapper;

namespace Agoda.Frameworks.DB
{
    public static class StoredProcHelper
    {
        /// <summary>
        /// Set column type mapping for Dapper by scanning derived types of IStoredProc from assemblies.
        /// Types must be public to be scanned.
        /// </summary>
        /// <param name="assemblies">Assemblies to scan.</param>
        public static void SetTypeMap(params Assembly[] assemblies)
        {
            var types = assemblies
                .SelectMany(x => x.GetExportedTypes())
                .Where(x => x.IsClass && !x.IsAbstract);
            SetTypeMap(types);
        }

        /// <summary>
        /// Set column type mapping for Dapper.
        /// </summary>
        /// <param name="spTypes">Collection of types that implement IStoredProc.</param>
        public static void SetTypeMap(IEnumerable<Type> spTypes)
        {
            var spInterfaces = new[] { typeof(IStoredProc<>), typeof(IStoredProc<,>) };
            var dbTypes = spTypes
                .SelectMany(x => x.GetInterfaces())
                .Where(x => x.IsGenericType)
                .Where(x =>
                {
                    var typeDef = x.GetGenericTypeDefinition();
                    return spInterfaces.Any(sp => typeDef == sp);
                })
                .SelectMany(x => x.GetGenericArguments());

            foreach (var modelType in dbTypes)
            {
                SqlMapper.SetTypeMap(modelType, CreateMap(modelType));
            }
        }

        private static CustomPropertyTypeMap CreateMap(Type dbModelType)
        {
            return new CustomPropertyTypeMap(dbModelType,
                (type, columnName) =>
                    type.GetProperties().FirstOrDefault(prop =>
                        prop.GetCustomAttributes<ColumnAttribute>()
                            .Any(attr => attr.Name == columnName)) ??
                    type.GetProperties().FirstOrDefault(prop =>
                        prop.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
