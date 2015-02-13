// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Class used to store information about a <see cref="ITagHelper"/>'s execution lifetime.
    /// </summary>
    public class TagHelperExecutionContext
    {
        private readonly List<ITagHelper> _tagHelpers;
        private readonly Func<Task> _executeChildContentAsync;
        private readonly Action _startWritingScope;
        private readonly Func<TextWriter> _endWritingScope;
        private string _childContent;

        /// <summary>
        /// Internal for testing purposes only.
        /// </summary>
        internal TagHelperExecutionContext(string tagName)
            : this(tagName,
                   uniqueId: string.Empty,
                   executeChildContentAsync: async () => await Task.FromResult(result: true),
                   startWritingScope: () => { },
                   endWritingScope: () => new StringWriter(),
                   parentExecutionContext: null)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="TagHelperExecutionContext"/>.
        /// </summary>
        /// <param name="tagName">The HTML tag name in the Razor source.</param>
        /// <param name="uniqueId">An identifier unique to the HTML element this context is for.</param>
        /// <param name="executeChildContentAsync">A delegate used to execute the child content asynchronously.</param>
        /// <param name="startWritingScope">A delegate used to start a writing scope in a Razor page.</param>
        /// <param name="endWritingScope">A delegate used to end a writing scope in a Razor page.</param>
        /// <param name="parentExecutionContext">The parent <see cref="TagHelperExecutionContext"/>.</param>
        public TagHelperExecutionContext([NotNull] string tagName,
                                         [NotNull] string uniqueId,
                                         [NotNull] Func<Task> executeChildContentAsync,
                                         [NotNull] Action startWritingScope,
                                         [NotNull] Func<TextWriter> endWritingScope,
                                         TagHelperExecutionContext parentExecutionContext)
        {
            _tagHelpers = new List<ITagHelper>();
            _executeChildContentAsync = executeChildContentAsync;
            _startWritingScope = startWritingScope;
            _endWritingScope = endWritingScope;

            // If we're not wrapped by another TagHelper then there will not be a parentExecutionContext.
            if (parentExecutionContext != null)
            {
                Items = new ItemsCopyOnWriteDictionary(parentExecutionContext.Items);
            }
            else
            {
                Items = new Dictionary<string, object>(StringComparer.Ordinal);
            }

            AllAttributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            HTMLAttributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            TagName = tagName;
            UniqueId = uniqueId;
        }

        /// <summary>
        /// Indicates if <see cref="GetChildContentAsync"/> has been called.
        /// </summary>
        public bool ChildContentRetrieved
        {
            get
            {
                return _childContent != null;
            }
        }

        /// <summary>
        /// Gets the collection of items used to communicate with child <see cref="ITagHelper"/>s.
        /// </summary>
        public IDictionary<string, object> Items { get; }

        /// <summary>
        /// HTML attributes.
        /// </summary>
        public IDictionary<string, string> HTMLAttributes { get; }

        /// <summary>
        /// <see cref="ITagHelper"/> bound attributes and HTML attributes.
        /// </summary>
        public IDictionary<string, object> AllAttributes { get; }

        /// <summary>
        /// An identifier unique to the HTML element this context is for.
        /// </summary>
        public string UniqueId { get; }

        /// <summary>
        /// <see cref="ITagHelper"/>s that should be run.
        /// </summary>
        public IEnumerable<ITagHelper> TagHelpers
        {
            get
            {
                return _tagHelpers;
            }
        }

        /// <summary>
        /// The HTML tag name in the Razor source.
        /// </summary>
        public string TagName { get; }

        /// <summary>
        /// The <see cref="ITagHelper"/>s' output.
        /// </summary>
        public TagHelperOutput Output { get; set; }

        /// <summary>
        /// Tracks the given <paramref name="tagHelper"/>.
        /// </summary>
        /// <param name="tagHelper">The tag helper to track.</param>
        public void Add([NotNull] ITagHelper tagHelper)
        {
            _tagHelpers.Add(tagHelper);
        }

        /// <summary>
        /// Tracks the HTML attribute in <see cref="AllAttributes"/> and <see cref="HTMLAttributes"/>.
        /// </summary>
        /// <param name="name">The HTML attribute name.</param>
        /// <param name="value">The HTML attribute value.</param>
        public void AddHtmlAttribute([NotNull] string name, string value)
        {
            HTMLAttributes.Add(name, value);
            AllAttributes.Add(name, value);
        }

        /// <summary>
        /// Tracks the <see cref="ITagHelper"/> bound attribute in <see cref="AllAttributes"/>.
        /// </summary>
        /// <param name="name">The bound attribute name.</param>
        /// <param name="value">The attribute value.</param>
        public void AddTagHelperAttribute([NotNull] string name, object value)
        {
            AllAttributes.Add(name, value);
        }

        /// <summary>
        /// Executes the child content asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> which on completion executes all child content.</returns>
        public Task ExecuteChildContentAsync()
        {
            return _executeChildContentAsync();
        }

        /// <summary>
        /// Execute and retrieve the rendered child content asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> that on completion returns the rendered child content.</returns>
        /// <remarks>
        /// Child content is only executed once. Successive calls to this method or successive executions of the 
        /// returned <see cref="Task{string}"/> return a cached result.
        /// </remarks>
        public async Task<string> GetChildContentAsync()
        {
            if (_childContent == null)
            {
                _startWritingScope();
                await _executeChildContentAsync();
                _childContent = _endWritingScope().ToString();
            }

            return _childContent;
        }

        private class ItemsCopyOnWriteDictionary : IDictionary<string, object>
        {
            private readonly IDictionary<string, object> _sourceDictionary;
            private IDictionary<string, object> _innerDictionary;

            public ItemsCopyOnWriteDictionary([NotNull] IDictionary<string, object> sourceDictionary)
            {
                _sourceDictionary = sourceDictionary;
            }

            private IDictionary<string, object> ReadDictionary
            {
                get
                {
                    return _innerDictionary ?? _sourceDictionary;
                }
            }

            private IDictionary<string, object> WriteDictionary
            {
                get
                {
                    if (_innerDictionary == null)
                    {
                        _innerDictionary = new Dictionary<string, object>(_sourceDictionary, StringComparer.Ordinal);
                    }

                    return _innerDictionary;
                }
            }

            public virtual ICollection<string> Keys
            {
                get
                {
                    return ReadDictionary.Keys;
                }
            }

            public virtual ICollection<object> Values
            {
                get
                {
                    return ReadDictionary.Values;
                }
            }

            public virtual int Count
            {
                get
                {
                    return ReadDictionary.Count;
                }
            }

            public virtual bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public virtual object this[[NotNull] string key]
            {
                get
                {
                    return ReadDictionary[key];
                }
                set
                {
                    WriteDictionary[key] = value;
                }
            }

            public virtual bool ContainsKey([NotNull] string key)
            {
                return ReadDictionary.ContainsKey(key);
            }

            public virtual void Add([NotNull] string key, object value)
            {
                WriteDictionary.Add(key, value);
            }

            public virtual bool Remove([NotNull] string key)
            {
                return WriteDictionary.Remove(key);
            }

            public virtual bool TryGetValue([NotNull] string key, out object value)
            {
                return ReadDictionary.TryGetValue(key, out value);
            }

            public virtual void Add(KeyValuePair<string, object> item)
            {
                WriteDictionary.Add(item);
            }

            public virtual void Clear()
            {
                WriteDictionary.Clear();
            }

            public virtual bool Contains(KeyValuePair<string, object> item)
            {
                return ReadDictionary.Contains(item);
            }

            public virtual void CopyTo([NotNull] KeyValuePair<string, object>[] array, int arrayIndex)
            {
                ReadDictionary.CopyTo(array, arrayIndex);
            }

            public bool Remove(KeyValuePair<string, object> item)
            {
                return WriteDictionary.Remove(item);
            }

            public virtual IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return ReadDictionary.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}