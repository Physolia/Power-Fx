﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Errors
{
    // TASK: 67034: Cleanup: Eliminate StringIds.
    internal sealed class TexlError : BaseError, IRuleError
    {
        private readonly List<string> _nameMapIDs;

        // Node may be null.
        public readonly TexlNode Node;

        // Tok will always be non-null.
        public readonly Token Tok;

        // TextSpan for the rule error.
        public override Span TextSpan { get; }

        public override IEnumerable<string> SinkTypeErrors => _nameMapIDs;

        public TexlError(Token tok, DocumentErrorSeverity severity, ErrorResourceKey errKey, params object[] args)
            : base(null, null, DocumentErrorKind.AXL, severity, errKey, args)
        {
            Contracts.AssertValue(tok);

            Tok = tok;
            TextSpan = new Span(tok.VerifyValue().Span.Min, tok.VerifyValue().Span.Lim);

            _nameMapIDs = new List<string>();
        }

        public TexlError(TexlNode node, DocumentErrorSeverity severity, ErrorResourceKey errKey, params object[] args)
            : base(null, null, DocumentErrorKind.AXL, severity, errKey, args)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(node.Token);

            Node = node;
            Tok = node.Token;
            TextSpan = node.GetTextSpan();

            _nameMapIDs = new List<string>();
        }

        public void MarkSinkTypeError(DName name)
        {
            Contracts.AssertValid(name);

            Contracts.Assert(!_nameMapIDs.Contains(name.Value));
            _nameMapIDs.Add(name.Value);
        }

        protected override void FormatCore(StringBuilder sb)
        {
            Contracts.AssertValue(sb);

            sb.AppendFormat(PowerFxConfig.GetCurrentCulture(), TexlStrings.FormatSpan_Min_Lim(), Tok.Span.Min, Tok.Span.Lim);

            if (Node != null)
            {
                sb.AppendFormat(PowerFxConfig.GetCurrentCulture(), TexlStrings.InfoNode_Node(), Node.ToString());
            }
            else
            {
                sb.AppendFormat(PowerFxConfig.GetCurrentCulture(), TexlStrings.InfoTok_Tok(), Tok.ToString());
            }

            sb.AppendFormat(PowerFxConfig.GetCurrentCulture(), TexlStrings.FormatErrorSeparator());
            base.FormatCore(sb);
        }
    }
}