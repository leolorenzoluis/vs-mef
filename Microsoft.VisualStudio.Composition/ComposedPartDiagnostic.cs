﻿namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;

    public class ComposedPartDiagnostic
    {
        public ComposedPartDiagnostic(ComposedPart part, string formattedMessage)
            : this(ImmutableHashSet.Create(part), formattedMessage)
        {
        }

        public ComposedPartDiagnostic(ComposedPart part, string unformattedMessage, params object[] args)
            : this(part, string.Format(CultureInfo.CurrentCulture, unformattedMessage, args))
        {
        }

        public ComposedPartDiagnostic(IEnumerable<ComposedPart> parts, string formattedMessage)
        {
            Requires.NotNull(parts, "parts");
            Requires.NotNullOrEmpty(formattedMessage, "formattedMessage");

            this.Parts = ImmutableList.CreateRange(parts);
            this.Message = formattedMessage;
        }

        public ComposedPartDiagnostic(IEnumerable<ComposedPart> parts, string unformattedMessage, params object[] args)
            : this(parts, string.Format(CultureInfo.CurrentCulture, unformattedMessage, args))
        {
        }

        public IReadOnlyCollection<ComposedPart> Parts { get; private set; }

        public string Message { get; private set; }
    }
}
