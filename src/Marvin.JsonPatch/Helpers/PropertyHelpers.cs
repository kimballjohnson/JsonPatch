﻿// Kevin Dockx
//
// Any comments, input: @KevinDockx
// Any issues, requests: https://github.com/KevinDockx/JsonPatch
//
// Enjoy :-)

using Marvin.JsonPatch.Exceptions;
using Marvin.JsonPatch.Operations;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Marvin.JsonPatch.Helpers
{
    internal static class PropertyHelpers
    {


        public static object GetValue(PropertyInfo propertyToGet, object targetObject, string pathToProperty)
        {
            // it is possible the path refers to a nested property.  In that case, we need to 
            // get from a different target object: the nested object. 

            var splitPath = pathToProperty.Split('/');

            // skip the first one if it's empty
            var startIndex = (string.IsNullOrWhiteSpace(splitPath[0]) ? 1 : 0);


            for (int i = startIndex; i < splitPath.Length - 1; i++)
            {
                // if the current part of the path is numeric, this means we're trying
                // to get the propertyInfo of a specific object in an array.  To allow
                // for this, the previous value (targetObject) must be an IEnumerable, and
                // the position must exist.

                int numericValue = -1;
                if (int.TryParse(splitPath[i], out numericValue))
                {
                    var element = GetElementAtFromObject(targetObject, numericValue);
                    if (element != null)
                    {
                        targetObject = element;
                    }
                    else
                    {
                        // will result in JsonPatchException in calling class, as expected
                        return null;
                    }

                }
                else
                {
                    var propertyInfoToGet = GetPropertyInfo(targetObject, splitPath[i]
                    , BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    targetObject = propertyInfoToGet.GetValue(targetObject, null);
                }
            }


            //for (int i = startIndex; i < splitPath.Length - 1; i++)
            //{
            //    var propertyInfoToGet = GetPropertyInfo(targetObject, splitPath[i]
            //        , BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            //    targetObject = propertyInfoToGet.GetValue(targetObject, null);
            //}


            return propertyToGet.GetValue(targetObject, null);
        }

        public static bool SetValue(PropertyInfo propertyToSet, object targetObject, string pathToProperty, object value)
        {
            // it is possible the path refers to a nested property.  In that case, we need to 
            // set on a different target object: the nested object.


            var splitPath = pathToProperty.Split('/');

            // skip the first one if it's empty
            var startIndex = (string.IsNullOrWhiteSpace(splitPath[0]) ? 1 : 0);

            for (int i = startIndex; i < splitPath.Length - 1; i++)
            {
                // if the current part of the path is numeric, this means we're trying
                // to get the propertyInfo of a specific object in an array.  To allow
                // for this, the previous value (targetObject) must be an IEnumerable, and
                // the position must exist.

                int numericValue = -1;
                if (int.TryParse(splitPath[i], out numericValue))
                {
                    var element = GetElementAtFromObject(targetObject, numericValue);
                    if (element != null)
                    {
                        targetObject = element;
                    }
                    else
                    {
                        // will result in JsonPatchException in calling class, as expected
                        return false;
                    }

                }
                else
                {
                    var propertyInfoToGet = GetPropertyInfo(targetObject, splitPath[i]
                    , BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    targetObject = propertyInfoToGet.GetValue(targetObject, null);
                }
            }

            propertyToSet.SetValue(targetObject, value, null);
            return true;
        }


        public static PropertyInfo FindProperty(object targetObject, string propertyPath)
        {
            try
            {
                var splitPath = propertyPath.Split('/');

                // skip the first one if it's empty
                var startIndex = (string.IsNullOrWhiteSpace(splitPath[0]) ? 1 : 0);

                for (int i = startIndex; i < splitPath.Length - 1; i++)
                {
                    // if the current part of the path is numeric, this means we're trying
                    // to get the propertyInfo of a specific object in an array.  To allow
                    // for this, the previous value (targetObject) must be an IEnumerable, and
                    // the position must exist.

                    int numericValue = -1;
                    if (int.TryParse(splitPath[i], out numericValue))
                    {
                        var element = GetElementAtFromObject(targetObject, numericValue);
                        if (element != null)
                        {
                            targetObject = element;
                        }
                        else
                        {
                            // will result in JsonPatchException in calling class, as expected
                            return null;
                        }

                    }
                    else
                    {
                        var propertyInfoToGet = GetPropertyInfo(targetObject, splitPath[i]
                        , BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        targetObject = propertyInfoToGet.GetValue(targetObject, null);
                    }
                }


                var propertyToFind = targetObject.GetType().GetProperty(splitPath.Last(),
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                return propertyToFind;


            }
            catch (Exception)
            {
                // will result in JsonPatchException in calling class, as expected
                return null;
            }
        }

        private static object GetElementAtFromObject(object targetObject, int numericValue)
        {

            if (numericValue > -1)
            {
                // Check if the targetobject is an IEnumerable,
                // and if the position is valid.
                if (targetObject is IEnumerable)
                {
                    var indexable = ((IEnumerable)targetObject).Cast<object>();

                    if (indexable.Count() >= numericValue)
                    {
                        return indexable.ElementAt(numericValue);
                    }
                    else { return null; }
                }
                else { return null; ; }
            }
            else { return null; }
        }

        internal static ConversionResult ConvertToActualType(Type propertyType, object value)
        {
            try
            {
                var o = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value), propertyType);
                return new ConversionResult(true, o);
            }
            catch (Exception)
            {
                return new ConversionResult(false, null);
            }
        }


        internal static Type GetEnumerableType(Type type)
        {
            if (type == null) throw new ArgumentNullException();
            foreach (Type interfaceType in type.GetInterfaces())
            {

                if (interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return interfaceType.GetGenericArguments()[0];
                }
            }
            return null;
        }


        internal static int GetNumericEnd(string path)
        {
            var possibleIndex = path.Substring(path.LastIndexOf("/") + 1);
            var castedIndex = -1;

            if (int.TryParse(possibleIndex, out castedIndex))
            {
                return castedIndex;
            }

            return -1;

        }


        private static PropertyInfo GetPropertyInfo(object targetObject, string propertyName,
        BindingFlags bindingFlags)
        {
            return targetObject.GetType().GetProperty(propertyName, bindingFlags);
        }


        internal static ActualPropertyPathResult GetActualPropertyPath(
            string propertyPath,
            object objectToApplyTo,
            Operation operationToReport,
            bool forPath)
        {
            if (propertyPath.EndsWith("/-"))
            {
                return new ActualPropertyPathResult(-1, propertyPath.Substring(0, propertyPath.Length - 2), true);
            }
            else
            {

                var possibleIndex = propertyPath.Substring(propertyPath.LastIndexOf("/") + 1);
                int castedIndex = -1;
                if (int.TryParse(possibleIndex, out castedIndex))
                {
                    // has numeric end.  
                    if (castedIndex > -1)
                    {
                        var pathToProperty = propertyPath.Substring(
                           0,
                           propertyPath.LastIndexOf('/' + castedIndex.ToString()));

                        return new ActualPropertyPathResult(castedIndex, pathToProperty, false);
                    }
                    else
                    {
                        string message = forPath ?
                             string.Format("Patch failed: provided path is invalid, position too small: {0}", propertyPath)
                             : string.Format("Patch failed: provided from is invalid, position too small: {0}", propertyPath);

                        // negative position - invalid path
                        throw new JsonPatchException(
                             new JsonPatchError(objectToApplyTo,
                                 operationToReport,
                              message), 422);
                    }
                }

                return new ActualPropertyPathResult(-1, propertyPath, false);
            }
        }

    }
}