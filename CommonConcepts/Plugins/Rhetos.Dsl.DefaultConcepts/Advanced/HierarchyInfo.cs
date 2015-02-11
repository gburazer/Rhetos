﻿/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Rhetos.Compiler;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Hierarchy")]
    public class HierarchyInfo : ReferencePropertyInfo, IAlternativeInitializationConcept
    {
        public static readonly CsTag<HierarchyInfo> BeforeRecomputeTag = "BeforeRecompute";

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Referenced" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            if (Referenced != null && Referenced != DataStructure)
                throw new DslSyntaxException(this, string.Format(
                    "Incorrectly constructed Hierarchy property: it should reference itself. Reference='{0}', DataStructure='{1}'.",
                    Referenced.GetUserDescription(),
                    DataStructure.GetUserDescription()));

            Referenced = DataStructure;
            createdConcepts = null;
        }

        public ComputedInfo GetComputedDataStructure()
        {
            return new ComputedInfo
            {
                Module = DataStructure.Module,
                Name = "Compute" + DataStructure.Name + Name + "Hierarchy",
                Expression = ComputedDataStructureExpression()
            };
        }

        public PersistedDataStructureInfo GetPersistedDataStructure()
        {
            return new PersistedDataStructureInfo
            {
                Module = DataStructure.Module,
                Name = DataStructure.Name + Name + "Hierarchy",
                Source = GetComputedDataStructure()
            };
        }

        protected virtual string ComputedDataStructureExpression()
        {
            return string.Format(@"repository =>
            {{
                try
                {{
                    var hierarchyItems = repository.{0}.{1}.Query().Select(item =>
                        new Rhetos.Dom.DefaultConcepts.HierarchyItem
                            {{
                                ID = item.ID,
                                ParentID = item.{2}.ID
                            }}).ToArray();

                    {3}
                    var hierarchyIndexes = Rhetos.Dom.DefaultConcepts.HierarchyInfo.Compute(hierarchyItems, null);
                    return hierarchyIndexes.Select(hi => new {0}.Compute{1}{2}Hierarchy
                    {{
                        ID = hi.ID,
                        LeftIndex = hi.LeftIndex,
                        RightIndex = hi.RightIndex,
                        Level = hi.Level
                    }}).ToArray();
                }}
                catch (Rhetos.Dom.DefaultConcepts.HierarchyCircularReferenceException)
                {{
                    throw new Rhetos.UserException(""It is not allowed to enter a circular dependency between records in hierarchy {0}.{1} by {2}."");
                }}
            }}",
               DataStructure.Module.Name,
               DataStructure.Name,
               Name,
               BeforeRecomputeTag.Evaluate(this));
        }

        public virtual string FilterAncestorsExpression()
        {
            return string.Format(@"(items, repository, filterParameter) =>
            {{
                var child = repository.{0}.{1}.Query().Where(item => item.ID == filterParameter.ID).SingleOrDefault();
                if (child == null)
                    throw new Rhetos.UserException(""Given record does not exist: {0}.{1}, ID "" + filterParameter.ID + ""."");
                int leftIndex = child.Extension_{1}{2}Hierarchy.LeftIndex.Value;

                return items.Where(item =>
                    item.Extension_{1}{2}Hierarchy.LeftIndex < leftIndex
                    && item.Extension_{1}{2}Hierarchy.RightIndex > leftIndex);
            }}",
                DataStructure.Module.Name,
                DataStructure.Name,
                Name);
        }

        public virtual string FilterDescendantsExpression()
        {
            return string.Format(@"(items, repository, filterParameter) =>
            {{
                var parent = repository.{0}.{1}.Query().Where(item => item.ID == filterParameter.ID).SingleOrDefault();
                if (parent == null)
                    throw new Rhetos.UserException(""Given record does not exist: {0}.{1}, ID "" + filterParameter.ID + ""."");
                int leftIndex = parent.Extension_{1}{2}Hierarchy.LeftIndex.Value;
                int rightIndex = parent.Extension_{1}{2}Hierarchy.RightIndex.Value;

                return items.Where(item =>
                    item.Extension_{1}{2}Hierarchy.LeftIndex > leftIndex
                    && item.Extension_{1}{2}Hierarchy.LeftIndex < rightIndex);
            }}",
                DataStructure.Module.Name,
                DataStructure.Name,
                Name);
        }
    }

    [Export(typeof(IConceptMacro))]
    public class HierarchyMacro : IConceptMacro<HierarchyInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(HierarchyInfo conceptInfo, IDslModel existingConcepts)
        {
            ComputedInfo computedDataStructure = conceptInfo.GetComputedDataStructure();
            PersistedDataStructureInfo persistedDataStructure = conceptInfo.GetPersistedDataStructure();
            var persistedLeftIndexProperty = new IntegerPropertyInfo { DataStructure = persistedDataStructure, Name = "LeftIndex" };

            var dependencies = GetDependsOnWriteableDataStructure(conceptInfo.DataStructure, existingConcepts, conceptInfo);
            var computedDataStructureDependencies = dependencies.Select(dependsOn =>
                new ChangesOnChangedItemsInfo
                {
                    Computation = computedDataStructure,
                    DependsOn = dependsOn,
                    FilterType = "FilterAll",
                    FilterFormula = "changedItems => new FilterAll()"
                });

            var filterAncestorsParameter = new ParameterInfo { Module = conceptInfo.DataStructure.Module, Name = conceptInfo.Name + "HierarchyAncestors" };
            var filterDescendantsParameter = new ParameterInfo { Module = conceptInfo.DataStructure.Module, Name = conceptInfo.Name + "HierarchyDescendants" };

            return new IConceptInfo[]
            {
                // Computing the hierarcy information:
                computedDataStructure,
                new ModuleExternalReferenceInfo { Module = computedDataStructure.Module, TypeOrAssembly = @"Plugins\Rhetos.Dom.DefaultConcepts.dll" },
                new DataStructureExtendsInfo { Extension = computedDataStructure, Base = conceptInfo.DataStructure },
                new IntegerPropertyInfo { DataStructure = computedDataStructure, Name = "LeftIndex" },
                new IntegerPropertyInfo { DataStructure = computedDataStructure, Name = "RightIndex" },
                new IntegerPropertyInfo { DataStructure = computedDataStructure, Name = "Level" },

                // Persisting the hierarcy information:
                persistedDataStructure,
                new PersistedAllPropertiesInfo { Persisted = persistedDataStructure }, // This will copy all properties from computedDataStructure.
                new PersistedKeepSynchronizedInfo { Persisted = persistedDataStructure },
                persistedLeftIndexProperty,
                new SqlIndexInfo { Property = persistedLeftIndexProperty },

                // Implement filters for finding ancestors and descendants, using indexed pesisted data:
                filterAncestorsParameter,
                filterDescendantsParameter,
                new GuidPropertyInfo { DataStructure = filterAncestorsParameter, Name = "ID" },
                new GuidPropertyInfo { DataStructure = filterDescendantsParameter, Name = "ID" },
                new ComposableFilterByInfo { Source = conceptInfo.DataStructure, Parameter = conceptInfo.Name + "HierarchyAncestors", Expression = conceptInfo.FilterAncestorsExpression() },
                new ComposableFilterByInfo { Source = conceptInfo.DataStructure, Parameter = conceptInfo.Name + "HierarchyDescendants", Expression = conceptInfo.FilterDescendantsExpression() },

            }.Concat(computedDataStructureDependencies);
        }

        /// <summary>
        /// Returns all entites that a given data structure is constructed from.
        /// If the given data structure depends is an entity, it will be the only item in the result.
        /// </summary>
        public static List<DataStructureInfo> GetDependsOnWriteableDataStructure(DataStructureInfo dataStructure, IDslModel allConcepts, IConceptInfo errorContext)
        {
            var dependencies = new List<DataStructureInfo>();
            GetDependsOnWriteableDataStructure(dataStructure, dependencies, allConcepts, errorContext, new HashSet<string>());
            return dependencies;
        }

        private static void GetDependsOnWriteableDataStructure(DataStructureInfo dataStructure, List<DataStructureInfo> dependencies, IDslModel allConcepts, IConceptInfo errorContext, HashSet<string> done)
        {
            var conceptKey = dataStructure.GetKey();
            if (done.Contains(conceptKey))
                return;
            done.Add(conceptKey);

            if (dataStructure is EntityInfo)
                dependencies.Add(dataStructure);
            else if (dataStructure is SqlQueryableInfo)
            {
                var deps = allConcepts.FindByType<SqlDependsOnDataStructureInfo>().Where(dep => dep.Dependent == dataStructure).ToArray();
                foreach (var dep in deps)
                    GetDependsOnWriteableDataStructure(dep.DependsOn, dependencies, allConcepts, errorContext, done);
            }
            else
                throw new DslSyntaxException(errorContext.GetKeywordOrTypeName()
                    + " is not supported on dependency type '" + dataStructure.GetKeywordOrTypeName() + "'. "
                    + errorContext.GetUserDescription() + " depends on " + dataStructure.GetUserDescription() + ".");
        }
    }
}
