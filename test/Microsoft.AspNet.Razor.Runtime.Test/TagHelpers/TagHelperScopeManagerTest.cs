﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.Test.TagHelpers
{
    public class TagHelperScopeManagerTest
    {
        private static readonly Action DefaultStartWritingScope = () => { };
        private static readonly Func<TextWriter> DefaultEndWritingScope = () => new StringWriter();
        private static readonly Func<Task> DefaultExecuteChildContentAsync =
            async () => await Task.FromResult(result: true);

        [Fact]
        public void Begin_DoesNotRequireParentExecutionContext()
        {
            // Arrange & Act
            var scopeManager = new TagHelperScopeManager();

            // Act
            var executionContext = scopeManager.Begin(
                parentExecutionContext: null,
                tagName: "p",
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);
            executionContext.Items["test-entry"] = 1234;

            // Assert
            var executionContextItem = Assert.Single(executionContext.Items);
            Assert.Equal("test-entry", (string)executionContextItem.Key, StringComparer.Ordinal);
            Assert.Equal(1234, executionContextItem.Value);
        }

        [Fact]
        public void Begin_ReturnedExecutionContext_ItemsAreRetrievedFromParentExecutionContext()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();
            var parentExecutionContext = new TagHelperExecutionContext("p");
            parentExecutionContext.Items["test-entry"] = 1234;

            // Act
            var executionContext = scopeManager.Begin(
                parentExecutionContext,
                tagName: "p",
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);

            // Assert
            var executionContextItem = Assert.Single(executionContext.Items);
            Assert.Equal("test-entry", (string)executionContextItem.Key, StringComparer.Ordinal);
            Assert.Equal(1234, executionContextItem.Value);
        }

        [Fact]
        public void Begin_ReturnedExecutionContext_ComplexObjectItemEntriesAreVisibleToParent()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();
            var parentComplexObject = new Dictionary<string, int>(StringComparer.Ordinal);
            var parentExecutionContext = new TagHelperExecutionContext("p");
            parentExecutionContext.Items["test-entry"] = parentComplexObject;
            var executionContext = scopeManager.Begin(
                parentExecutionContext,
                tagName: "p",
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);

            // Act
            ((Dictionary<string, int>)executionContext.Items["test-entry"]).Add("from-child", 1234);

            // Assert
            var executionContextItem = Assert.Single(executionContext.Items);
            Assert.Equal("test-entry", (string)executionContextItem.Key, StringComparer.Ordinal);
            Assert.Same(parentComplexObject, executionContextItem.Value);
            var parentEntry = Assert.Single(parentComplexObject);
            Assert.Equal("from-child", parentEntry.Key, StringComparer.Ordinal);
            Assert.Equal(1234, parentEntry.Value);
        }

        [Fact]
        public void Begin_ReturnedExecutionContext_ItemsModificationDoesNotAffectParent()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();
            var parentExecutionContext = new TagHelperExecutionContext("p");
            parentExecutionContext.Items["test-entry"] = 1234;
            var executionContext = scopeManager.Begin(
                parentExecutionContext,
                tagName: "p",
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);

            // Act
            executionContext.Items["test-entry"] = 2222;

            // Assert
            var executionContextItem = Assert.Single(executionContext.Items);
            Assert.Equal("test-entry", (string)executionContextItem.Key, StringComparer.Ordinal);
            Assert.Equal(2222, executionContextItem.Value);
            var parentExecutionContextItem = Assert.Single(parentExecutionContext.Items);
            Assert.Equal("test-entry", (string)parentExecutionContextItem.Key, StringComparer.Ordinal);
            Assert.Equal(1234, parentExecutionContextItem.Value);
        }

        [Fact]
        public void Begin_ReturnedExecutionContext_ItemsInsertionDoesNotAffectParent()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();
            var parentExecutionContext = new TagHelperExecutionContext("p");
            var executionContext = scopeManager.Begin(
                parentExecutionContext,
                tagName: "p",
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);

            // Act
            executionContext.Items["new-entry"] = 2222;

            // Assert
            var executionContextItem = Assert.Single(executionContext.Items);
            Assert.Equal("new-entry", (string)executionContextItem.Key, StringComparer.Ordinal);
            Assert.Equal(2222, executionContextItem.Value);
            Assert.Empty(parentExecutionContext.Items);
        }

        [Fact]
        public void Begin_ReturnedExecutionContext_ItemsRemovalDoesNotAffectParent()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();
            var parentExecutionContext = new TagHelperExecutionContext("p");
            parentExecutionContext.Items["test-entry"] = 1234;
            var executionContext = scopeManager.Begin(
                parentExecutionContext,
                tagName: "p",
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);

            // Act
            executionContext.Items.Remove("test-entry");

            // Assert
            Assert.Empty(executionContext.Items);
            var parentExecutionContextItem = Assert.Single(parentExecutionContext.Items);
            Assert.Equal("test-entry", (string)parentExecutionContextItem.Key, StringComparer.Ordinal);
            Assert.Equal(1234, parentExecutionContextItem.Value);
        }

        [Fact]
        public void Begin_CreatesContextWithAppropriateTagName()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();

            // Act
            var executionContext = scopeManager.Begin(
                parentExecutionContext: null,
                tagName: "p",
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);

            // Assert
            Assert.Equal("p", executionContext.TagName);
        }

        [Fact]
        public void Begin_CanNest()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();

            // Act
            var executionContext = scopeManager.Begin(
                parentExecutionContext: null,
                tagName: "p",
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);
            executionContext = scopeManager.Begin(
                parentExecutionContext: null,
                tagName: "div",
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);

            // Assert
            Assert.Equal("div", executionContext.TagName);
        }

        [Fact]
        public void End_ReturnsParentExecutionContext()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();

            // Act
            var executionContext = scopeManager.Begin(
                parentExecutionContext: null,
                tagName: "p",
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);
            executionContext = scopeManager.Begin(
                parentExecutionContext: null,
                tagName: "div",
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);
            executionContext = scopeManager.End();

            // Assert
            Assert.Equal("p", executionContext.TagName);
        }

        [Fact]
        public void End_ReturnsNullIfNoNestedContext()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();

            // Act
            var executionContext = scopeManager.Begin(
                parentExecutionContext: null,
                tagName: "p",
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);
            executionContext = scopeManager.Begin(
                parentExecutionContext: null,
                tagName: "div",
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);
            executionContext = scopeManager.End();
            executionContext = scopeManager.End();

            // Assert
            Assert.Null(executionContext);
        }

        [Fact]
        public void End_ThrowsIfNoScope()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();
            var expectedError = string.Format(
                "Must call '{2}.{1}' before calling '{2}.{0}'.",
                nameof(TagHelperScopeManager.End),
                nameof(TagHelperScopeManager.Begin),
                nameof(TagHelperScopeManager));

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                scopeManager.End();
            });

            Assert.Equal(expectedError, ex.Message);
        }
    }
}