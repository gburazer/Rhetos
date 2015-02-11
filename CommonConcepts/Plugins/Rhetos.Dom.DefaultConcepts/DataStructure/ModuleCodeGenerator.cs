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

using Microsoft.CSharp.RuntimeBinder;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ModuleInfo))]
    public class ModuleCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<ModuleInfo> UsingTag = "Using";
        public static readonly CsTag<ModuleInfo> NamespaceMembersTag = "Body";
        public static readonly CsTag<ModuleInfo> RepositoryMembersTag = "RepositoryMembers";
        public static readonly CsTag<ModuleInfo> HelperNamespaceMembersTag = "HelperNamespaceMembers";

        private static string GenerateNamespaceSnippet(ModuleInfo info)
        {
            return string.Format(
                @"
namespace {0}
{{
    {1}

    {2}

    {3}
}}

namespace {0}._Helper
{{
    {1}

    {2}

    public class _ModuleRepository
    {{
        private readonly Common.DomRepository _domRepository;
        private readonly Common.ExecutionContext _executionContext;

        public _ModuleRepository(Common.DomRepository domRepository, Common.ExecutionContext executionContext)
        {{
            _domRepository = domRepository;
            _executionContext = executionContext;
        }}
        {4}
    }}
    {5}
}}",
                info.Name,
                DomInitializationCodeGenerator.StandardNamespacesSnippet,
                UsingTag.Evaluate(info),
                NamespaceMembersTag.Evaluate(info),
                RepositoryMembersTag.Evaluate(info),
                HelperNamespaceMembersTag.Evaluate(info));
        }

        private static string ModuleRepositoryInCommonRepositorySnippet(ModuleInfo info)
        {
            return string.Format(
                @"private {0}._Helper._ModuleRepository _{0};
        public {0}._Helper._ModuleRepository {0} {{ get {{ return _{0} ?? (_{0} = new {0}._Helper._ModuleRepository(this, _executionContext)); }} }}

        ",
                info.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ModuleInfo)conceptInfo;

            codeBuilder.InsertCode(GenerateNamespaceSnippet(info));
            // Default .NET framework asseblies:
            codeBuilder.AddReferencesFromDependency(typeof(int)); // Includes reference to mscorlib.dll
            codeBuilder.AddReferencesFromDependency(typeof(Enumerable)); // Includes reference to System.Core.
            codeBuilder.AddReferencesFromDependency(typeof(ISet<>)); // Includes reference to System.
            codeBuilder.AddReferencesFromDependency(typeof(RuntimeBinderException)); // Includes reference to Microsoft.CSharp.
            codeBuilder.AddReferencesFromDependency(typeof(System.Data.Common.DbDataReader)); // Includes reference to System.Data.
            codeBuilder.AddReferencesFromDependency(typeof(System.Data.DataRowExtensions)); // Includes reference to System.Data.DataSetExtensions.
            codeBuilder.AddReferencesFromDependency(typeof(System.Xml.Serialization.XmlSerializer)); // Includes reference to System.Xml.
            codeBuilder.AddReferencesFromDependency(typeof(System.Xml.Linq.XElement)); // Includes reference to System.Xml.Linq.
            // Commonly used Rhetos classes:
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Dom.DefaultConcepts.FilterAll)); // Includes reference to Rhetos.Dom.DefaultConcepts.dll
            // Other classes used in domain object model:
            codeBuilder.AddReferencesFromDependency(typeof(NHibernate.Linq.LinqExtensionMethods)); // Includes reference to NHibernate.dll.
            codeBuilder.AddReferencesFromDependency(typeof(System.Runtime.Serialization.DataContractAttribute)); // Includes reference to System.Runtime.Serialization.dll.
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Persistence.IPersistenceTransaction));

            codeBuilder.InsertCode(ModuleRepositoryInCommonRepositorySnippet(info), CommonDomRepositoryMembersTag);
        }

        // TODO: Move this tags to DomInitializationCodeGenerator (breaking backward compatibility for other DSL packages)
        public const string CommonUsingTag = "/*CommonUsing*/";
        public const string CommonDomRepositoryMembersTag = "/*CommonDomRepositoryMembers*/";
        public const string CommonAutofacConfigurationMembersTag = "/*CommonAutofacConfigurationMembers*/";
        public const string ExecutionContextMemberTag = "/*ExecutionContextMember*/";
        public const string ExecutionContextConstructorArgumentTag = "/*ExecutionContextConstructorArgument*/";
        public const string ExecutionContextConstructorAssignmentTag = "/*ExecutionContextConstructorAssignment*/";
        public const string RegisteredInterfaceImplementationNameTag = "/*RegisteredInterfaceImplementationName*/";
        public const string ApplyFiltersOnClientReadTag = "/*ApplyFiltersOnClientRead*/";
        public const string CommonNamespaceMembersTag = "/*CommonNamespaceMembers*/";
        public const string CommonInfrastructureMembersTag = "/*CommonInfrastructureMembers*/";
        public const string NHibernateConfigurationExtensionTag = "/*NHibernateConfigurationExtension*/";
        public const string LinqToHqlGeneratorsRegistryTag = "/*LinqToHqlGeneratorsRegistry*/"; // Instead of calling Configuration.LinqToHqlGeneratorsRegistry() function, add the registry code using ModuleCodeGenerator.LinqToHqlGeneratorsRegistryTag. NHibernate supports only one LinqToHqlGeneratorsRegistry, so it cannot be registered in more than one plugin.

    }
}
