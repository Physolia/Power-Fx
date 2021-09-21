﻿//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AppMagic.Common.Telemetry;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Logging.Trackers;

namespace Microsoft.AppMagic.Authoring.Texl
{
    // Abstract base class for all statistical functions with similar signatures that take
    // a table as the first argument, and a value function as the second argument.
    internal abstract class StatisticalTableFunction : FunctionWithTableInput
    {
        public override bool SupportsParamCoercion => true;
        public override bool IsSelfContained => true;

        public StatisticalTableFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc)
            : base(name, description, fc, DType.Number, 0x02, 2, 2, DType.EmptyTable, DType.Number)
        {
            ScopeInfo = new FunctionScopeInfo(this, usesAllFieldsInScope: false, acceptsLiteralPredicates: false);
        }

        public override bool SupportsPaging(CallNode callNode, TexlBinding binding) { return false; }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.StatisticalTArg1, TexlStrings.StatisticalTArg2 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_T");
        }

        public override bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!OldFeatureGates.AggregateFunctionSupport || FunctionDelegationCapability.Capabilities == DelegationCapability.None)
                return false;

            if (!CheckArgsCount(callNode, binding))
                return false;

            IExternalDataSource dataSource;
            if (!TryGetValidDataSourceForDelegation(callNode, binding, FunctionDelegationCapability, out dataSource))
            {
                if (dataSource != null && dataSource.IsDelegatable)
                    binding.ErrorContainer.EnsureError(DocumentErrorSeverity.Warning, callNode, TexlStrings.OpNotSupportedByServiceSuggestionMessage_OpNotSupportedByService, Name);

                return false;
            }

            var args = callNode.Args.Children.VerifyValue();
            if (binding.GetType(args[0]).HasExpandInfo
            || (!binding.IsFullRecordRowScopeAccess(args[1]) && args[1].Kind != NodeKind.FirstName)
            || !binding.IsRowScope(args[1])
            || (!OldFeatureGates.AggregateDateTimeSupport && binding.GetType(args[1]) != DType.Number)
            || ExpressionContainsView(callNode, binding))
            {
                SuggestDelegationHint(callNode, binding);

                if (binding.GetType(args[1]) != DType.Number)
                    DelegationTrackerCore.SetDelegationTrackerStatus(DelegationStatus.NotANumberArgType, callNode, binding, this, DelegationTelemetryInfo.CreateEmptyDelegationTelemetryInfo());
                else
                    DelegationTrackerCore.SetDelegationTrackerStatus(DelegationStatus.InvalidArgType, callNode, binding, this, DelegationTelemetryInfo.CreateEmptyDelegationTelemetryInfo());

                return false;
            }

            if (binding.IsFullRecordRowScopeAccess(args[1]))
                return GetDottedNameNodeDelegationStrategy().IsValidDottedNameNode(args[1].AsDottedName(), binding, null, null);

            var firstNameStrategy = GetFirstNameNodeDelegationStrategy().VerifyValue();
            return firstNameStrategy.IsValidFirstNameNode(args[1].AsFirstName(), binding, null);
        }

        private bool ExpressionContainsView(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            var viewFinderVisitor = new ViewFinderVisitor(binding);
            callNode.Accept(viewFinderVisitor);

            return viewFinderVisitor.ContainsView;
        }
    }
}