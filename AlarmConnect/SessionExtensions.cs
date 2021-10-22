using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AlarmConnect.Models;
using AlarmConnect.Models.ActionModels;
using AlarmConnect.Models.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AlarmConnect
{
    public static class SessionExtensions
    {
        private const int MaxIdsPerRequest = 20;

        private static readonly Dictionary<object, (DateTime expiration, Dictionary<string, object> meta)> Meta
            = new Dictionary<object, (DateTime expiration, Dictionary<string, object> meta)>();

        internal static T SetMeta<T>(T key, Dictionary<string, object> meta)
        {
            var now = DateTime.Now;

            foreach (var k in Meta.Where(x => x.Value.expiration < now).Select(x => x.Key).ToArray())
            {
                Meta.Remove(k);
            }

            if (!(key is null))
            {
                Meta[key] = (DateTime.Now.AddMinutes(1), meta);
            }

            return key;
        }

        internal static T LinkMeta<T>(T key, IDataObject orig)
        {
            if (!(key is null))
            {
                if (Meta.ContainsKey(orig))
                {
                    Meta[key] = Meta[orig];
                }
            }

            return key;
        }

        internal static T[] LinkMeta<T>(T[] key, IDataObject[] orig)
        {
            if (!(key is null))
            {
                if (Meta.ContainsKey(orig))
                {
                    Meta[key] = Meta[orig];
                }
            }

            return key;
        }

        /// <summary>
        /// Gets the metadata associated with the DataObject or DataObject array returned from a request.
        /// </summary>
        /// <param name="key">A DataObject or DataObject array.</param>
        /// <returns></returns>
        public static Dictionary<string, object> GetMeta(object key)
        {
            if (Meta.ContainsKey(key)) return Meta[key].meta;
            return new Dictionary<string, object>();
        }

        internal static void LogDebug(this ISession session, string s)
        {
            lock (session.Logger)
            {
                session.Logger.LogDebug(s);
            }
        }

        internal static void LogInformation(this ISession session, string s)
        {
            lock (session.Logger)
            {
                session.Logger.LogInformation(s);
            }
        }

        internal static void LogWarning(this ISession session, string s)
        {
            lock (session.Logger)
            {
                session.Logger.LogWarning(s);
            }
        }

        internal static void LogWarning(this ISession session, Exception e, string s)
        {
            lock (session.Logger)
            {
                session.Logger.LogWarning(e, s);
            }
        }

        internal static void LogError(this ISession session, string s)
        {
            lock (session.Logger)
            {
                session.Logger.LogError(s);
            }
        }

        internal static void LogError(this ISession session, Exception e, string s)
        {
            lock (session.Logger)
            {
                session.Logger.LogError(e, s);
            }
        }

        internal static IDataObject ApiGetOneRaw(this ISession session, string endpoint, string id = null, string command = null, string[] query = null, bool reqMfa = true)
        {
            try
            {
                var content = session.ApiGet(endpoint, id, command, query, reqMfa).Result;
                if (string.IsNullOrEmpty(content))
                {
                    session.LogError("No content returned.");
                    return null;
                }

                var data = JsonSerializer.Deserialize<DataObjectWrapper>(content);
                if (data is null) return null;

                return SetMeta(data.Data, data.Meta);
            }
            catch (JsonException e)
            {
                session.LogError(e, "Failed to parse item.");
                return null;
            }
        }

        internal static IDataObject[] ApiGetManyRaw(this ISession session, string endpoint, string[] ids = null, string command = null, string[] query = null, bool reqMfa = true)
        {
            try
            {
                if (ids is null ||
                    ids.Length < 1)
                {
                    var result = session.ApiGet(endpoint, null, command, query, reqMfa).Result;
                    if (string.IsNullOrEmpty(result))
                    {
                        session.LogError("No content returned.");
                        return null;
                    }

                    var data = JsonSerializer.Deserialize<DataObjectCollection>(result);
                    return data?.Data.Cast<IDataObject>().ToArray() ?? Array.Empty<IDataObject>();
                }

                if (ids.Length == 2 &&
                    ids[1] is null)
                {
                    var result = session.ApiGet(endpoint, ids[0], command, query, reqMfa).Result;
                    if (string.IsNullOrEmpty(result))
                    {
                        session.LogError("No content returned.");
                        return null;
                    }

                    var data = JsonSerializer.Deserialize<DataObjectCollection>(result);
                    return data?.Data.Cast<IDataObject>().ToArray() ?? Array.Empty<IDataObject>();
                }

                var ret  = new List<IDataObject>();
                var meta = new Dictionary<string, object>();

                while (ids.Length > 0)
                {
                    string[] q;
                    if (ids.Length > MaxIdsPerRequest)
                    {
                        q   = ids.Take(MaxIdsPerRequest).SelectMany(x => new[] { "ids[]", x }).Concat(query ?? Array.Empty<string>()).ToArray();
                        ids = ids.Skip(MaxIdsPerRequest).ToArray();
                    }
                    else
                    {
                        q   = ids.SelectMany(x => new[] { "ids[]", x }).Concat(query ?? Array.Empty<string>()).ToArray();
                        ids = Array.Empty<string>();
                    }

                    var result = session.ApiGet(endpoint, null, command, q, reqMfa).Result;
                    if (string.IsNullOrEmpty(result))
                    {
                        session.LogError("No content returned.");
                        return null;
                    }

                    var data = JsonSerializer.Deserialize<DataObjectCollection>(result);
                    if (!(data?.Data is null))
                    {
                        ret.AddRange(data.Data);
                    }

                    if (!(data?.Meta is null))
                    {
                        foreach (var (k, v) in data.Meta)
                        {
                            meta[k] = v;
                        }
                    }
                }

                return SetMeta(ret.ToArray(), meta);
            }
            catch (JsonException e)
            {
                session.LogError(e, "Failed to parse items.");
                return null;
            }
        }

        /// <summary>
        /// Get one item from the API.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="id">(Optional) The ID of the item to retrieve.</param>
        /// <param name="command">(Optional) The command to execute.</param>
        /// <param name="query">(Optional) Query parameters to pass to the API (must be in pairs).</param>
        /// <param name="requireMfa">(Optional) Set to false if the API call does not require MFA validation.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Returns the item or null.</returns>
        /// <exception cref="ArgumentException">The type returned by the API is not compatible.</exception>
        public static T GetOne<T>(this ISession session, string id = null, string command = null, string[] query = null, bool requireMfa = true)
            where T : class, IDataObjectFillable, new()
        {
            var tmp  = new T();
            var data = session.ApiGetOneRaw(tmp.ApiEndpoint, id, command, query, requireMfa);
            if (data is null) return null;
            if (data.ApiType != tmp.AcceptedApiType) throw new ArgumentException($"The type {typeof(T)} does not accept the '{data.ApiType}' type.");
            tmp.FillFromDataObject(data, session);
            return LinkMeta(tmp, data);
        }

        /// <summary>
        /// Get multiple items from the API.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ids">(Optional) The IDs of the items to retrieve.  If a single ID is meant to be part of the URL instead of a query parameter, provide two values with the second being null.</param>
        /// <param name="command">(Optional) The command to execute.</param>
        /// <param name="query">(Optional) Query parameters to pass to the API (must be in pairs).</param>
        /// <param name="requireMfa">(Optional) Set to false if the API call does not require MFA validation.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Returns an array of items.</returns>
        /// <exception cref="ArgumentException">The type returned by the API is not compatible.</exception>
        public static T[] GetMany<T>(this ISession session, string[] ids = null, string command = null, string[] query = null, bool requireMfa = true)
            where T : class, IDataObjectFillable, new()
        {
            var tmp = new T();

            var data = session.ApiGetManyRaw(tmp.ApiEndpoint, ids, command, query, requireMfa);
            if (data is null) return Array.Empty<T>();

            var invalid = data.Where(x => x.ApiType != tmp.AcceptedApiType).Select(x => x.ApiType).Distinct().ToArray();
            if (invalid.Any()) throw new ArgumentException($"The type {typeof(T)} does not accept the type(s): " + string.Join(", ", invalid));

            var ret = data.Select(
                              x =>
                              {
                                  var z = new T();
                                  z.FillFromDataObject(x, session);
                                  return z;
                              }
                          )
                          .ToArray();
            return LinkMeta(ret, data);
        }

        private static FieldInfo GetBackingFieldFor(Type t, PropertyInfo propertyInfo)
        {
            return t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .FirstOrDefault(f => f.Name.Contains($"<{propertyInfo.Name}>", StringComparison.OrdinalIgnoreCase));
        }

        private static (PropertyInfo navProp, Action<TParent, TNavValueType> navSet, PropertyInfo fkProp) GetNavHelpers<TParent, TNavValueType>(Expression<Func<TParent, TNavValueType>> expression)
        {
            var tParent = typeof(TParent);
            var tValue  = typeof(TNavValueType);

            var isMany = typeof(IEnumerable).IsAssignableFrom(tValue);
            var fkType =  isMany ? typeof(IReadOnlyList<string>) : typeof(string);
            
            if (expression is null) throw new ArgumentNullException(nameof(expression));
            var navProp = (expression.Body is MemberExpression mex ? mex
                           : expression.Body is UnaryExpression { NodeType: ExpressionType.Convert } uex ? uex.Operand as MemberExpression : null)?.Member as PropertyInfo;

            if (navProp is null) throw new ArgumentException("Nav property expression must be a single property expression.");
            
            Action<TParent, TNavValueType> setNavValue;

            if (!navProp.CanWrite)
            {
                setNavValue = (item, value) => navProp.SetValue(item, value);
            }
            else
            {
                var navField = GetBackingFieldFor(tParent, navProp);
                if (navField is null) throw new ArgumentException("Nav property does not appear to have a backing field.");
                setNavValue = (item, value) => navField.SetValue(item, value);
            }
            
            var fkAttrib = navProp.GetCustomAttribute<ForeignKeyAttribute>();
            var fkNames  = new List<string>();
            if (fkAttrib != null)
            {
                fkNames.Add(fkAttrib.Name);
            }
            else
            {
                var nn = navProp.Name;
                if (isMany)
                {
                    fkNames.Add(nn + "Ids");
                    if (nn.EndsWith("ies")) fkNames.Add(nn[..^3] + "Ids");
                    if (nn.EndsWith("es")) fkNames.Add(nn[..^2] + "Ids");
                    if (nn.EndsWith("s")) fkNames.Add(nn[..^1] + "Ids");
                }
                else
                {
                    fkNames.Add(nn + "Id");
                }
            }

            var fkProp = tParent.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                .FirstOrDefault(p => fkNames.Any(n => p.Name.Equals(n, StringComparison.OrdinalIgnoreCase)));

            if (fkProp is null) throw new InvalidOperationException($"Failed to locate FK property for {navProp.Name} nav property.");
            if (!fkType.IsAssignableFrom(fkProp.PropertyType)) throw new InvalidOperationException($"The FK property for {navProp.Name} does not return a valid ID type.");

            return (navProp, setNavValue, fkProp);
        }
        
        /// <summary>
        /// Loads the related property.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="navProperty">The property to load.</param>
        /// <param name="andThen">A function to process the related property after loading it.</param>
        /// <typeparam name="TParent"></typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <returns>Returns the source object with the related property filled.  Related property will be null if FK property value is null.</returns>
        /// <exception cref="ArgumentNullException">The source object or nav property is null.</exception>
        /// <exception cref="ArgumentException">The session is missing from the source object -or- The nav property is not a valid property expression -or- The nav property does not have an apparent backing field -or- The related object could not be found with the FK property value.</exception>
        /// <exception cref="InvalidOperationException">The FK property could not be located.  Use the ForeignKeyAttribute to specify one.  The FK property must return a single string value.</exception>
        public static TParent With<TParent, TChild>(this TParent self, Expression<Func<TParent, TChild>> navProperty, Action<TChild> andThen = null)
            where TParent : class, IDataObjectFillable, new()
            where TChild : class, IDataObjectFillable, new()
        {
            if (self is null) throw new ArgumentNullException(nameof(self));
            if (navProperty is null) throw new ArgumentNullException(nameof(navProperty));
            if (self.Session is null) throw new ArgumentException("Missing session from self.");

            var (navProp, setNavValue, fkProp) = GetNavHelpers(navProperty);
            
            var fkValue = fkProp.GetValue(self) as string;
            if (fkValue is null)
            {
                setNavValue(self, null);
            }
            else
            {
                var navValue = self.Session.GetOne<TChild>(fkValue);
                if (navValue is null) throw new ArgumentException($"Failed to locate {typeof(TChild)} object with ID={fkValue}.");
                andThen?.Invoke(navValue);
                setNavValue(self, navValue);
            }

            return self;
        }
        
        /// <summary>
        /// Loads the related property.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="navProperty">The property to load.</param>
        /// <param name="andThen">A function to process the related property after loading it.</param>
        /// <typeparam name="TParent"></typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <returns>Returns the source object with the related property filled.  Related property will be null if FK property value is null.</returns>
        /// <exception cref="ArgumentNullException">The source object or nav property is null.</exception>
        /// <exception cref="ArgumentException">The session is missing from the source object -or- The nav property is not a valid property expression -or- The nav property does not have an apparent backing field -or- The related object could not be found with the FK property value.</exception>
        /// <exception cref="InvalidOperationException">The FK property could not be located.  Use the ForeignKeyAttribute to specify one.  The FK property must return a single string value.</exception>
        public static IReadOnlyList<TParent> With<TParent, TChild>(this IReadOnlyList<TParent> self, Expression<Func<TParent, TChild>> navProperty, Action<TChild> andThen = null)
            where TParent : class, IDataObjectFillable, new()
            where TChild : class, IDataObjectFillable, new()
        {
            if (self is null) throw new ArgumentNullException(nameof(self));
            if (self.Count < 1) return self;
            
            if (navProperty is null) throw new ArgumentNullException(nameof(navProperty));
            var session = self[0].Session;
            if (session is null) throw new ArgumentException("Missing session from first item.");

            var (navProp, setNavValue, fkProp) = GetNavHelpers(navProperty);
            
            var fkValues  = self.Select(x => fkProp.GetValue(x) as string).Where(x => !(x is null)).Distinct().ToArray();
            var navValues = fkValues.Any() ? session.GetMany<TChild>(fkValues) : Array.Empty<TChild>();
            if (navValues.Length != fkValues.Length)throw new ArgumentException($"Loaded {navValues.Length} nav values, but was expecting {fkValues.Length} values for {navProp.Name}.");

            foreach (var item in self)
            {
                var fkVal = fkProp.GetValue(item) as string;
                if (fkVal is null)
                {
                    setNavValue(item, null);
                }
                else
                {
                    var navValue = navValues.FirstOrDefault(x => x.Id == fkVal);
                    if (navValue is null) throw new ArgumentException($"Failed to locate {typeof(TChild)} object with ID={fkVal}.");
                    andThen?.Invoke(navValue);
                    setNavValue(item, navValue);
                }
            }
            
            return self;
        }

        /// <summary>
        /// Loads the related property.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="navProperty">The property to load.</param>
        /// <param name="andThen">A function to process the related property after loading it.</param>
        /// <typeparam name="TParent"></typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <returns>Returns the source object with the related property filled.  Related property will be null if FK property value is null.</returns>
        /// <exception cref="ArgumentNullException">The source object or nav property is null.</exception>
        /// <exception cref="ArgumentException">The session is missing from the source object -or- The nav property is not a valid property expression -or- The nav property does not have an apparent backing field -or- The related object could not be found with the FK property value.</exception>
        /// <exception cref="InvalidOperationException">The FK property could not be located.  Use the ForeignKeyAttribute to specify one.  The FK property must return a single string value.</exception>
        public static IEnumerable<TParent> With<TParent, TChild>(this IEnumerable<TParent> self, Expression<Func<TParent, TChild>> navProperty, Action<TChild> andThen = null)
            where TParent : class, IDataObjectFillable, new()
            where TChild : class, IDataObjectFillable, new()
            => With(self?.ToArray(), navProperty, andThen);
        
        /// <summary>
        /// Loads the related property.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="navProperty">The property to load.</param>
        /// <param name="andThen">A function to process the related property after loading it.</param>
        /// <typeparam name="TParent"></typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <returns>Returns the source object with the related property filled.  Related property will always be assigned, will be a zero-length array when FK value is null or empty.</returns>
        /// <exception cref="ArgumentNullException">The source object or nav property is null.</exception>
        /// <exception cref="ArgumentException">The session is missing from the source object -or- The nav property is not a valid property expression -or- The nav property does not have an apparent backing field -or- The related object could not be found with the FK property value.</exception>
        /// <exception cref="InvalidOperationException">The FK property could not be located.  Use the ForeignKeyAttribute to specify one.  The FK property must return an array of string values.</exception>
        public static TParent With<TParent, TChild>(this TParent self, Expression<Func<TParent, IReadOnlyList<TChild>>> navProperty, Action<IReadOnlyList<TChild>> andThen = null)
            where TParent : class, IDataObjectFillable, new()
            where TChild : class, IDataObjectFillable, new()
        {
            if (self is null) throw new ArgumentNullException(nameof(self));
            if (navProperty is null) throw new ArgumentNullException(nameof(navProperty));
            if (self.Session is null) throw new ArgumentException("Missing session from self.");

            var (navProp, setNavValue, fkProp) = GetNavHelpers(navProperty);

            var fkValue = fkProp.GetValue(self) as IReadOnlyList<string>;
            if (fkValue is null || fkValue.Count < 1)
            {
                setNavValue(self, Array.Empty<TChild>());
            }
            else
            {
                var navValue = self.Session.GetMany<TChild>(fkValue.ToArray());
                if (navValue is null) throw new InvalidOperationException("A null array should never be returned from GetMany.");
                if (navValue.Length != fkValue.Count) throw new ArgumentException($"Loaded {navValue.Length} nav values, but was expecting {fkValue.Count} values for {navProp.Name}.");
                if (navValue.Any(x => x is null)) throw new ArgumentException($"Failed to load at least one nav value for {navProp.Name}.");
                andThen?.Invoke(navValue);
                setNavValue(self, navValue);
            }

            return self;
        }

        /// <summary>
        /// Loads the related property.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="navProperty">The property to load.</param>
        /// <param name="andThen">A function to process the related property after loading it.</param>
        /// <typeparam name="TParent"></typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <returns>Returns the source object with the related property filled.  Related property will always be assigned, will be a zero-length array when FK value is null or empty.</returns>
        /// <exception cref="ArgumentNullException">The source object or nav property is null.</exception>
        /// <exception cref="ArgumentException">The session is missing from the source object -or- The nav property is not a valid property expression -or- The nav property does not have an apparent backing field -or- The related object could not be found with the FK property value.</exception>
        /// <exception cref="InvalidOperationException">The FK property could not be located.  Use the ForeignKeyAttribute to specify one.  The FK property must return an array of string values.</exception>
        public static IReadOnlyList<TParent> With<TParent, TChild>(this IReadOnlyList<TParent> self, Expression<Func<TParent, IReadOnlyList<TChild>>> navProperty, Action<IReadOnlyList<TChild>> andThen = null)
            where TParent : class, IDataObjectFillable, new()
            where TChild : class, IDataObjectFillable, new()
        {
            if (self is null) throw new ArgumentNullException(nameof(self));
            if (navProperty is null) throw new ArgumentNullException(nameof(navProperty));
            var session = self[0].Session;
            if (session is null) throw new ArgumentException("Missing session from first item.");

            var (navProp, setNavValue, fkProp) = GetNavHelpers(navProperty);

            var fkValues  = self.SelectMany(x => fkProp.GetValue(x) as IReadOnlyList<string>).Where(x => !(x is null)).Distinct().ToArray();
            var navValues = fkValues.Any() ? session.GetMany<TChild>(fkValues) : Array.Empty<TChild>();
            if (navValues.Length != fkValues.Length)throw new ArgumentException($"Loaded {navValues.Length} nav values, but was expecting {fkValues.Length} values for {navProp.Name}.");
            
            foreach (var item in self)
            {
                var fkVal = fkProp.GetValue(item) as IReadOnlyList<string>;
                if (fkVal is null || fkVal.Count < 1)
                {
                    setNavValue(item, Array.Empty<TChild>());
                }
                else
                {
                    var navValue = fkVal.Select(x => navValues.FirstOrDefault(y => y.Id == x)).ToArray();
                    if (navValue.Length != fkVal.Count) throw new ArgumentException($"Loaded {navValue.Length} nav values, but was expecting {fkVal.Count} values for {navProp.Name}.");
                    if (navValue.Any(x => x is null)) throw new ArgumentException($"Failed to load at least one nav value for {navProp.Name}.");
                    andThen?.Invoke(navValue);
                    setNavValue(item, navValue);
                }
            }
            
            return self;
        }

        /// <summary>
        /// Loads the related property.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="navProperty">The property to load.</param>
        /// <param name="andThen">A function to process the related property after loading it.</param>
        /// <typeparam name="TParent"></typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <returns>Returns the source object with the related property filled.  Related property will always be assigned, will be a zero-length array when FK value is null or empty.</returns>
        /// <exception cref="ArgumentNullException">The source object or nav property is null.</exception>
        /// <exception cref="ArgumentException">The session is missing from the source object -or- The nav property is not a valid property expression -or- The nav property does not have an apparent backing field -or- The related object could not be found with the FK property value.</exception>
        /// <exception cref="InvalidOperationException">The FK property could not be located.  Use the ForeignKeyAttribute to specify one.  The FK property must return an array of string values.</exception>
        public static IEnumerable<TParent> With<TParent, TChild>(this IEnumerable<TParent> self, Expression<Func<TParent, IReadOnlyList<TChild>>> navProperty, Action<IReadOnlyList<TChild>> andThen = null)
            where TParent : class, IDataObjectFillable, new()
            where TChild : class, IDataObjectFillable, new()
            => With(self?.ToArray(), navProperty, andThen);

        /// <summary>
        /// Get the current identities.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static IReadOnlyList<AlarmIdentity> GetIdentities(this ISession session)
            => session.GetMany<AlarmIdentity>(requireMfa: false);

        /// <summary>
        /// Get a dealer.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static AlarmDealer GetDealer(this ISession session, string id)
            => session.GetOne<AlarmDealer>(id);

        private static bool HasSelectedSystemBeenSet(this ISession session)
        {
            return session.GetStateValue("selected-system-set") == "1";
        }
        
        private static void SetSelectedSystem(this ISession session, string id)
        {
            session.SetStateValue("selected-system", id);
            session.SetStateValue("selected-unit", "");
            if (!string.IsNullOrEmpty(id))
            {
                var sys = session.GetOne<AlarmSystem>(id);
                session.SetStateValue("selected-unit", sys?.UnitId ?? "");
            }

            session.SetStateValue("selected-system-set", "1");
        }
        
        /// <summary>
        /// Gets the currently selected system.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="refresh"></param>
        /// <returns></returns>
        public static string GetSelectedSystem(this ISession session, bool refresh = false)
        {
            if (refresh || !session.HasSelectedSystemBeenSet())
            {
                session.GetAvailableSystems();
            }
            return session.GetStateValue("selected-system");
        }

        /// <summary>
        /// Gets the currently selected system's unit ID.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="refresh"></param>
        /// <returns></returns>
        public static string GetSelectedUnitId(this ISession session, bool refresh = false)
        {
            if (refresh || !session.HasSelectedSystemBeenSet())
            {
                session.GetAvailableSystems();
            }

            return session.GetStateValue("selected-unit");
        }

        /// <summary>
        /// Get the available systems.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static IReadOnlyList<AlarmAvailableSystem> GetAvailableSystems(this ISession session)
        {
            var ret   = session.GetMany<AlarmAvailableSystem>();
            var sysId = ret.FirstOrDefault(x => x.IsSelected)?.Id ?? "";
            session.SetSelectedSystem(sysId);
            return ret;
        }

        /// <summary>
        /// Get a single system.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="id">The system ID.</param>
        /// <returns></returns>
        public static AlarmSystem GetSystem(this ISession session, string id) => session.GetOne<AlarmSystem>(id);

        /// <summary>
        /// Get multiple systems.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static IReadOnlyList<AlarmSystem> GetSystems(this ISession session, string[] ids) => session.GetMany<AlarmSystem>(ids);
        
        /// <summary>
        /// Selects the current system.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="id">The system ID to select.</param>
        /// <returns></returns>
        public static bool SelectSystem(this ISession session, string id)
        {
            session.SetSelectedSystem("");
            var result = session.ApiPost("systems/availableSystemItems", null, id, "selectSystemOrGroup").Result;
            if (string.IsNullOrEmpty(result)) return false;
            session.GetAvailableSystems();
            return true;
        }
        
        /// <summary>
        /// Get a user from the currently selected system.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static AlarmUser GetUser(this ISession session, string id) => session.GetOne<AlarmUser>(id);

        /// <summary>
        /// Get users from the currently selected system.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static IReadOnlyList<AlarmUser> GetUsers(this ISession session, string[] ids) => session.GetMany<AlarmUser>(ids);
        
        /// <summary>
        /// Get the users for the currently selected system.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="startIndex"></param>
        /// <param name="batchSize"></param>
        /// <param name="includeChildScope"></param>
        /// <param name="searchString"></param>
        /// <param name="sortByAccess"></param>
        /// <returns></returns>
        public static IReadOnlyList<AlarmUser> GetUsers(this ISession session, int startIndex = -1, int batchSize = 50, bool includeChildScope = false, string searchString = "", bool sortByAccess = true)
        {
            var haveMore = true;
            var results  = new List<AlarmUser>();
            var loadAll  = startIndex < 0;
            
            if (loadAll) startIndex = 0;

            if (batchSize < 5) batchSize   = 5;
            if (batchSize > 100) batchSize = 100;
            
            while (haveMore)
            {
                haveMore = false;

                var batch = session.GetMany<AlarmUser>(
                                query: new[]
                                {
                                    "batchSize", batchSize.ToString(),
                                    "includeChildScope", includeChildScope.ToString().ToLower(),
                                    "searchString", searchString,
                                    "sortByAccess", sortByAccess.ToString().ToLower(),
                                    "startIndex", startIndex.ToString()
                                }
                            );
                var meta       = GetMeta(batch);
                var totalCount = 0L;

                if (meta.TryGetValue("totalCount", out var tcElement))
                {
                    if (tcElement is int tcI)
                    {
                        totalCount = tcI;
                    }
                    else if (tcElement is long tcL)
                    {
                        totalCount = tcL;
                    }
                    else if (tcElement is string tcS)
                    {
                        long.TryParse(tcS, out totalCount);
                    }
                    else if (tcElement is JsonElement element)
                    {
                        if (element.ValueKind == JsonValueKind.Number)
                        {
                            totalCount = element.GetInt64();
                        }
                        else
                        {
                            long.TryParse(element.GetString(), out totalCount);
                        }
                    }
                }
                
                results.AddRange(batch);

                startIndex += batch.Length;

                // if we don't have a total, and we received a full batch, go one past the current count.
                if (totalCount == 0 &&
                    batch.Length == batchSize)
                {
                    totalCount = startIndex + 1;
                }

                if (totalCount > startIndex && loadAll)
                {
                    haveMore = true;
                }
            }

            return results.ToArray();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="user"></param>
        /// <param name="emailAddress"></param>
        /// <param name="useHtmlFormat"></param>
        /// <returns></returns>
        public static AlarmEmailAddress AddEmailToUser(this ISession session, AlarmUser user, string emailAddress, bool useHtmlFormat = true)
        {
            if (session is null) throw new ArgumentNullException(nameof(session));
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (emailAddress is null) throw new ArgumentNullException(nameof(emailAddress));
            if (string.IsNullOrWhiteSpace(emailAddress)) throw new ArgumentException("email address cannot be blank");

            if (session.GetSelectedSystem() != user.LoadedFromSystemId)
            {
                if (!session.SelectSystem(user.LoadedFromSystemId))
                {
                    throw new InvalidOperationException("Failed to select the system the user was loaded from.");
                }
            }

            var reload = session.GetUser(user.Id).With(u => u.EmailAddresses);

            var email = reload.EmailAddresses.FirstOrDefault(x => x.Address.Equals(emailAddress, StringComparison.OrdinalIgnoreCase));
            if (!(email is null)) return email;

            var content = new AddEmailToUserModel()
            {
                Address = emailAddress,
                EmailSendingFormat = useHtmlFormat ? 1 : 0,
                User    = { Id = user.Id }
            };

            var                 result = new AlarmEmailAddress();
            IDataObjectFillable info   = result;

            var response = session.ApiPost(info.ApiEndpoint, content).Result;
            if (response is null) return null;
            var data = JsonSerializer.Deserialize<DataObjectWrapper>(response);
            info.FillFromDataObject(data.Data, session);
            
            return result;
        }
    }
}
