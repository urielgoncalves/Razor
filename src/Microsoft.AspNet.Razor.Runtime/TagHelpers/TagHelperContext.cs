// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Contains information related to the execution of <see cref="ITagHelper"/>s.
    /// </summary>
    public class TagHelperContext
    {
        private readonly Func<Task<string>> _getChildContentAsync;

        /// <summary>
        /// Instantiates a new <see cref="TagHelperContext"/>.
        /// </summary>
        /// <param name="allAttributes">Every attribute associated with the current HTML element.</param>
        /// <param name="parentItems">Items that have been created by parent <see cref="ITagHelper"/>s.</param>
        /// <param name="uniqueId">The unique identifier for the source element this <see cref="TagHelperContext" /> 
        /// applies to.</param>
        /// <param name="getChildContentAsync">A delegate used to execute and retrieve the rendered child content 
        /// asynchronously.</param>
        public TagHelperContext(
            [NotNull] IDictionary<string, object> allAttributes,
            [NotNull] IDictionary<string, object> parentItems,
            [NotNull] string uniqueId,
            [NotNull] Func<Task<string>> getChildContentAsync)
        {
            AllAttributes = allAttributes;
            Items = parentItems;
            UniqueId = uniqueId;
            _getChildContentAsync = getChildContentAsync;
        }

        /// <summary>
        /// Every attribute associated with the current HTML element.
        /// </summary>
        public IDictionary<string, object> AllAttributes { get; }

        /// <summary>
        /// Gets the collection of items used to communicate with child <see cref="ITagHelper"/>s.
        /// </summary>
        /// <remarks>
        /// This <see cref="IDictionary{string, object}"/> is copy-on-write in order to not affect parent
        /// <see cref="ITagHelper"/>s.
        /// </remarks>
        public IDictionary<string, object> Items { get; }

        /// <summary>
        /// An identifier unique to the HTML element this context is for.
        /// </summary>
        public string UniqueId { get; }

        /// <summary>
        /// A delegate used to execute and retrieve the rendered child content asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> that when executed returns content rendered by children.</returns>
        public Task<string> GetChildContentAsync()
        {
            return _getChildContentAsync();
        }
    }
}