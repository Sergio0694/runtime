// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Marks <see cref="Type"/> only if all of its generic arguments (starting at <see cref="NextArgument"/>) are present
    /// in the compilation. A type that could be created by the type loader at runtime can only be created if all
    /// of the arguments exist, so there is no need to eagerly create it when some argument is not otherwise needed.
    /// </summary>
    public sealed class TypeLoaderArgumentGateNode : DependencyNodeCore<NodeFactory>
    {
        private readonly IEETypeNode _type;
        private readonly int _nextArgument;

        public TypeLoaderArgumentGateNode(IEETypeNode type, int nextArgument)
        {
            _type = type;
            _nextArgument = nextArgument;
        }

        public IEETypeNode Type => _type;
        public int NextArgument => _nextArgument;

        private bool IsLast => _nextArgument >= _type.Type.Instantiation.Length;

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => !IsLast;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            if (IsLast)
                yield return new DependencyListEntry(_type, "Type loader candidate with all generic arguments present");
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context)
        {
            if (!IsLast)
            {
                yield return new CombinedDependencyListEntry(
                    context.TypeLoaderArgumentGate(_type, _nextArgument + 1),
                    context.NecessaryTypeSymbol(_type.Type.Instantiation[_nextArgument]),
                    "Generic argument of a type loader candidate is present");
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;

        protected override string GetName(NodeFactory context) => $"Type loader candidate gate: {_type.Type} (argument {_nextArgument})";
    }
}
