﻿// Any comments, input: @KevinDockx
// Any issues, requests: https://github.com/KevinDockx/JsonPatch
//
// Enjoy :-)

using Marvin.JsonPatch.Adapters;
using Marvin.JsonPatch.Converters;
using Marvin.JsonPatch.Helpers;
using Marvin.JsonPatch.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

// Implementation details: the purpose of this type of patch document is to ensure we can do type-checking
// when producing a JsonPatchDocument.  However, we cannot send this "typed" over the wire, as that would require
// including type data in the JsonPatchDocument serialized as JSON (to allow for correct deserialization) - that's
// not according to RFC 6902, and would thus break cross-platform compatibility. 

namespace Marvin.JsonPatch
{  
    [JsonConverter(typeof(TypedJsonPatchDocumentConverter))]
    public class JsonPatchDocument<T>: IJsonPatchDocument where T:class 
    {
        public List<Operation<T>> Operations { get; private set; }

        [JsonIgnore]
        public IContractResolver ContractResolver { get; set; }

        [JsonIgnore]
        public CaseTransformType CaseTransformType { get; set; }

        /// <summary>
        /// Create a new JsonPatchDocument
        /// </summary>
        public JsonPatchDocument() :
            this(new List<Operation<T>>(), new DefaultContractResolver(), CaseTransformType.LowerCase)
        {
        }

        /// <summary>
        /// Create a new JsonPatchDocument, and pass in a custom contract resolver
        /// to use when applying the document.
        /// </summary>
        /// <param name="contractResolver">A custom IContractResolver</param>
        public JsonPatchDocument(IContractResolver contractResolver)
            : this (new List<Operation<T>>(), contractResolver, CaseTransformType.LowerCase)
        {
        }
        
        /// <summary>
        /// Create a new JsonPatchDocument from a list of operations
        /// </summary>
        /// <param name="operations">A list of operations</param>
        public JsonPatchDocument(List<Operation<T>> operations)
            : this(operations, new DefaultContractResolver(), CaseTransformType.LowerCase)
        {
        }

        /// <summary>
        /// Create a new JsonPatchDocument and pass in a CaseTransformType
        /// </summary>
        /// <param name="caseTransformType">Defines the case used when seralizing the object to JSON</param>
        public JsonPatchDocument(CaseTransformType caseTransformType)
            : this(new List<Operation<T>>(), new DefaultContractResolver(), caseTransformType)
        {
        }

        /// <summary>
        /// Create a new JsonPatchDocument from a list of operations and pass in a CaseTransformType
        /// </summary>
        /// <param name="operations">A list of operations</param>
        /// <param name="caseTransformType">Defines the case used when seralizing the object to JSON</param>
        public JsonPatchDocument(List<Operation<T>> operations, CaseTransformType caseTransformType)
            : this(operations, new DefaultContractResolver(), caseTransformType)
        {
        }

        /// <summary>
        /// Create a new JsonPatchDocument, and pass in a CaseTransformType and
        /// custom contract resolver to use when applying the document.
        /// </summary>
        /// <param name="contractResolver">A custom IContractResolver</param>
        /// <param name="caseTransformType">Defines the case used when seralizing the object to JSON</param>
        public JsonPatchDocument(IContractResolver contractResolver, CaseTransformType caseTransformType)
            : this(new List<Operation<T>>(), contractResolver, caseTransformType)
        {
        }

        /// <summary>
        /// Create a new JsonPatchDocument from a list of operations, and pass in a custom contract resolver 
        /// to use when applying the document.
        /// </summary>
        /// <param name="operations">A list of operations</param>
        /// <param name="contractResolver">A custom IContractResolver</param>
        public JsonPatchDocument(List<Operation<T>> operations, IContractResolver contractResolver)
            : this(operations, contractResolver, CaseTransformType.LowerCase)
        {
        }

        public JsonPatchDocument(List<Operation<T>> operations, IContractResolver contractResolver, CaseTransformType caseTransformType)
        {
            Operations = operations;
            ContractResolver = contractResolver;
            CaseTransformType = caseTransformType;
        }

        /// <summary>
        /// Add operation.  Will result in, for example, 
        /// { "op": "add", "path": "/a/b/c", "value": [ "foo", "bar" ] }
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">path</param>
        /// <param name="value">value</param>
        /// <returns></returns>
        public JsonPatchDocument<T> Add<TProp>(Expression<Func<T, TProp>> path, TProp value)
        {
            Operations.Add(new Operation<T>("add", ExpressionHelpers.GetPath<T, TProp>(path, CaseTransformType), null, value));
            return this;
        }

        /// <summary>
        /// Add value to list at given position
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">path</param>
        /// <param name="value">value</param>
        /// <param name="position">position</param>
        /// <returns></returns>
        public JsonPatchDocument<T> Add<TProp>(Expression<Func<T, IList<TProp>>> path, TProp value, int position)
        {
            Operations.Add(new Operation<T>("add", ExpressionHelpers.GetPath<T, IList<TProp>>(path, CaseTransformType) + "/" + position, null, value));
            return this;
        }

         /// <summary>
         /// Add value at end of list
         /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">path</param>
        /// <param name="value">value</param>
        /// <returns></returns>
        public JsonPatchDocument<T> Add<TProp>(Expression<Func<T, IList<TProp>>> path, TProp value)
        {
            Operations.Add(new Operation<T>("add", ExpressionHelpers.GetPath<T, IList<TProp>>(path, CaseTransformType) + "/-", null, value));
            return this;
        }

        /// <summary>
        /// Remove value at target location.  Will result in, for example,
        /// { "op": "remove", "path": "/a/b/c" }
        /// </summary>
        /// <param name="remove"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public JsonPatchDocument<T> Remove<TProp>(Expression<Func<T, TProp>> path)
        {
            Operations.Add(new Operation<T>("remove", ExpressionHelpers.GetPath<T, TProp>(path, CaseTransformType), null));
            return this;
        }

        /// <summary>
        /// Remove value from list at given position
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="position">position</param>
        /// <returns></returns>
        public JsonPatchDocument<T> Remove<TProp>(Expression<Func<T, IList<TProp>>> path, int position)
        {
            Operations.Add(new Operation<T>("remove", ExpressionHelpers.GetPath<T, IList<TProp>>(path, CaseTransformType) + "/" + position, null));
            return this;
        }

        /// <summary>
        /// Remove value from end of list
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <returns></returns>
        public JsonPatchDocument<T> Remove<TProp>(Expression<Func<T, IList<TProp>>> path)
        {
            Operations.Add(new Operation<T>("remove", ExpressionHelpers.GetPath<T, IList<TProp>>(path, CaseTransformType) + "/-", null));
            return this;
        }

        /// <summary>
        /// Replace value.  Will result in, for example,
        /// { "op": "replace", "path": "/a/b/c", "value": 42 }
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public JsonPatchDocument<T> Replace<TProp>(Expression<Func<T, TProp>> path, TProp value)
        {
            Operations.Add(new Operation<T>("replace", ExpressionHelpers.GetPath<T, TProp>(path, CaseTransformType), null, value));
            return this;
        }

        /// <summary>
        /// Replace value in a list at given position
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="position">position</param>
        /// <returns></returns>
        public JsonPatchDocument<T> Replace<TProp>(Expression<Func<T, IList<TProp>>> path,  TProp value, int position)
        {
            Operations.Add(new Operation<T>("replace", ExpressionHelpers.GetPath<T, IList<TProp>>(path, CaseTransformType) + "/" + position, null, value));
            return this;
        }

        /// <summary>
        /// Replace value at end of a list
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <returns></returns>
        public JsonPatchDocument<T> Replace<TProp>(Expression<Func<T, IList<TProp>>> path, TProp value)
        {
            Operations.Add(new Operation<T>("replace", ExpressionHelpers.GetPath<T, IList<TProp>>(path, CaseTransformType) + "/-", null, value));
            return this;
        }

        /// <summary>
        /// Removes value at specified location and add it to the target location.  Will result in, for example:
        /// { "op": "move", "from": "/a/b/c", "path": "/a/b/d" }
        /// </summary>
        /// <param name="from"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public JsonPatchDocument<T> Move<TProp>(Expression<Func<T, TProp>> from, Expression<Func<T, TProp>> path)
        {
            Operations.Add(new Operation<T>("move", ExpressionHelpers.GetPath<T, TProp>(path, CaseTransformType)
                , ExpressionHelpers.GetPath<T, TProp>(from, CaseTransformType)));
            return this;
        }
        
        /// <summary>
        /// Move from a position in a list to a new location
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from"></param>
        /// <param name="positionFrom"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public JsonPatchDocument<T> Move<TProp>(Expression<Func<T, IList<TProp>>> from, int positionFrom, Expression<Func<T, TProp>> path)
        {
            Operations.Add(new Operation<T>("move", ExpressionHelpers.GetPath<T, TProp>(path, CaseTransformType)
              , ExpressionHelpers.GetPath<T, IList<TProp>>(from, CaseTransformType) + "/" + positionFrom));
            return this;
        }

        /// <summary>
        /// Move from a property to a location in a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from"></param>
        /// <param name="positionFrom"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public JsonPatchDocument<T> Move<TProp>(Expression<Func<T, TProp>> from,
            Expression<Func<T, IList<TProp>>> path, int positionTo)
        {
            Operations.Add(new Operation<T>("move", ExpressionHelpers.GetPath<T, IList<TProp>>(path, CaseTransformType)
                + "/" + positionTo
              , ExpressionHelpers.GetPath<T, TProp>(from, CaseTransformType)));
            return this;
        }

        /// <summary>
        /// Move from a position in a list to another location in a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from"></param>
        /// <param name="positionFrom"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public JsonPatchDocument<T> Move<TProp>(Expression<Func<T, IList<TProp>>> from, int positionFrom,
            Expression<Func<T, IList<TProp>>> path, int positionTo)
        {
            Operations.Add(new Operation<T>("move", ExpressionHelpers.GetPath<T, IList<TProp>>(path, CaseTransformType)
                + "/" + positionTo
              , ExpressionHelpers.GetPath<T, IList<TProp>>(from, CaseTransformType)+ "/" + positionFrom));
            return this;
        }

        /// <summary>
        /// Move from a position in a list to the end of another list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from"></param>
        /// <param name="positionFrom"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public JsonPatchDocument<T> Move<TProp>(Expression<Func<T, IList<TProp>>> from, int positionFrom,
            Expression<Func<T, IList<TProp>>> path)
        {
            Operations.Add(new Operation<T>("move", ExpressionHelpers.GetPath<T, IList<TProp>>(path, CaseTransformType)
                + "/-"
              , ExpressionHelpers.GetPath<T, IList<TProp>>(from, CaseTransformType) + "/" + positionFrom));
            return this;
        }

        /// <summary>
        /// Move to the end of a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from"></param>
        /// <param name="positionFrom"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public JsonPatchDocument<T> Move<TProp>(Expression<Func<T, TProp>> from, Expression<Func<T, IList<TProp>>> path)
        {
            Operations.Add(new Operation<T>("move", ExpressionHelpers.GetPath<T, IList<TProp>>(path, CaseTransformType) + "/-"
              , ExpressionHelpers.GetPath<T, TProp>(from, CaseTransformType)));
            return this;
        }

        /// <summary>
        /// Copy the value at specified location to the target location.  Willr esult in, for example:
        /// { "op": "copy", "from": "/a/b/c", "path": "/a/b/e" }
        /// </summary>
        /// <param name="from"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public JsonPatchDocument<T> Copy<TProp>(Expression<Func<T, TProp>> from, Expression<Func<T, TProp>> path)
        {
            Operations.Add(new Operation<T>("copy", ExpressionHelpers.GetPath<T, TProp>(path, CaseTransformType)
              , ExpressionHelpers.GetPath<T, TProp>(from, CaseTransformType)));
            return this;
        }

        /// <summary>
        /// Copy from a position in a list to a new location
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from"></param>
        /// <param name="positionFrom"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public JsonPatchDocument<T> Copy<TProp>(Expression<Func<T, IList<TProp>>> from, int positionFrom, Expression<Func<T, TProp>> path)
        {
            Operations.Add(new Operation<T>("copy", ExpressionHelpers.GetPath<T, TProp>(path, CaseTransformType)
              , ExpressionHelpers.GetPath<T, IList<TProp>>(from, CaseTransformType) + "/" + positionFrom));
            return this;
        }

        /// <summary>
        /// Copy from a property to a location in a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from"></param>
        /// <param name="positionFrom"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public JsonPatchDocument<T> Copy<TProp>(Expression<Func<T, TProp>> from, 
            Expression<Func<T, IList<TProp>>> path,  int positionTo)
        {
            Operations.Add(new Operation<T>("copy", ExpressionHelpers.GetPath<T,IList< TProp>>(path, CaseTransformType)
                + "/" + positionTo
              , ExpressionHelpers.GetPath<T, TProp>(from, CaseTransformType)));
            return this;
        }

        /// <summary>
        /// Copy from a position in a list to a new location in a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from"></param>
        /// <param name="positionFrom"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public JsonPatchDocument<T> Copy<TProp>(Expression<Func<T, IList<TProp>>> from, int positionFrom, 
            Expression<Func<T, IList<TProp>>> path, int positionTo)
        {
            Operations.Add(new Operation<T>("copy", ExpressionHelpers.GetPath<T, IList<TProp>>(path, CaseTransformType) 
                + "/" + positionTo
              , ExpressionHelpers.GetPath<T, IList<TProp>>(from, CaseTransformType) + "/" + positionFrom));
            return this;
        }

        /// <summary>
        /// Copy from a position in a list to the end of another list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from"></param>
        /// <param name="positionFrom"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public JsonPatchDocument<T> Copy<TProp>(Expression<Func<T, IList<TProp>>> from, int positionFrom,
            Expression<Func<T, IList<TProp>>> path)
        {
            Operations.Add(new Operation<T>("copy", ExpressionHelpers.GetPath<T, IList<TProp>>(path, CaseTransformType) 
                + "/-"
              , ExpressionHelpers.GetPath<T, IList<TProp>>(from, CaseTransformType) + "/" + positionFrom));
            return this;
        }

        /// <summary>
        /// Copy to the end of a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from"></param>
        /// <param name="positionFrom"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public JsonPatchDocument<T> Copy<TProp>(Expression<Func<T, TProp>> from, Expression<Func<T, IList<TProp>>> path)
        {
            Operations.Add(new Operation<T>("copy", ExpressionHelpers.GetPath<T, IList<TProp>>(path, CaseTransformType) + "/-"
              , ExpressionHelpers.GetPath<T, TProp>(from, CaseTransformType)));
            return this;
        }         

        /// <summary>
        /// Apply the patch document.  This method will change the passed-in object.
        /// </summary>
        /// <param name="objectToApplyTo">The object to apply the JsonPatchDocument to</param>
        public void ApplyTo(T objectToApplyTo)
        {
            ApplyTo(objectToApplyTo, new ObjectAdapter<T>(ContractResolver));
        }

        /// <summary>
        /// Apply the patch document, passing in a custom IObjectAdapter<typeparamref name=">"/>. 
        /// This method will change the passed-in object.
        /// </summary>
        /// <param name="objectToApplyTo">The object to apply the JsonPatchDocument to</param>
        /// <param name="adapter">The IObjectAdapter instance to use</param>
        public void ApplyTo(T objectToApplyTo, IObjectAdapter<T> adapter)
        {
            // apply each operation in order
            foreach (var op in Operations)
            {
                op.Apply(objectToApplyTo, adapter);
            }
        }

        IList<Operation> IJsonPatchDocument.GetOperations()
        {
            var allOps = new List<Operation>();

            if (Operations != null)
            {
                foreach (var op in Operations)
                {                    
                    var untypedOp = new Operation();

                    untypedOp.op = op.op;
                    untypedOp.value = op.value;
                    untypedOp.path = op.path;
                    untypedOp.from = op.from;

                    allOps.Add(untypedOp);
                }
            }
            return allOps;
        }
    }
}
