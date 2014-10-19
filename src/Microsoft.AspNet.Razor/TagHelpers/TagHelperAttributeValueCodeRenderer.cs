// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// Renders code for tag helper property initialization.
    /// </summary>
    public class TagHelperAttributeValueCodeRenderer
    {
        /// <summary>
        /// Called during Razor's code generation process to generate code that instantiates the value of the tag 
        /// helper's property. Last value written should not be or end with a semicolon.
        /// </summary>
        /// <param name="attributeDescriptor">
        /// The <see cref="TagHelperAttributeDescriptor"/> to generate code for.
        /// </param>
        /// <param name="writer">The <see cref="CSharpCodeWriter"/> that's used to write code.</param>
        /// <param name="context">A <see cref="CodeGeneratorContext"/> instance that contains information about 
        /// the current code generation process.</param>
        /// <param name="valueText">
        /// <see cref="string"/> containing the original attribute value (from the Razor source). <c>null</c> if this
        /// is a <see cref="string"/> attribute set using a Razor expression e.g.
        /// <c>someAttribute="Time: @DateTime.Now"</c>.
        /// </param>
        /// <param name="renderAttributeValue">
        /// <see cref="Action"/> that renders the raw value of the HTML attribute.
        /// </param>
        public virtual void RenderAttributeValue([NotNull] TagHelperAttributeDescriptor attributeDescriptor,
                                                 [NotNull] CSharpCodeWriter writer,
                                                 [NotNull] CodeBuilderContext context,
                                                 string valueText,
                                                 [NotNull] Action<CSharpCodeWriter> renderAttributeValue)
        {
            renderAttributeValue(writer);
        }
    }
}