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
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Rhetos.Utilities;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using System.Globalization;
using Rhetos.Compiler;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(AutoCodePropertyInfo))]
    [ConceptImplementationVersion(2, 0)]
    public class AutoCodeDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        public static readonly SqlTag<AutoCodePropertyInfo> ForEachGroupColumnTag = "ForEachGroupColumn";
        public static readonly SqlTag<AutoCodePropertyInfo> ForEachGroupValueTag = "ForEachGroupValue";

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            AutoCodePropertyInfo info = (AutoCodePropertyInfo)conceptInfo;
            createdDependencies = null;

            if (AutoCodeTriggerDatabaseDefinition.IsSupported(info.Property.DataStructure))
            {
                codeBuilder.InsertCode(Sql.Format("AutoCodeDatabaseDefinition_ColumnDefinition", 
                            info.Property.Name, 
                            ShortStringPropertyInfo.MaxLength,
                            ForEachGroupColumnTag.Evaluate(info),
                            ForEachGroupValueTag.Evaluate(info)
                        ),
                    AutoCodeTriggerDatabaseDefinition.ColumnsForAutoCodeSelectTag, info.Dependency_TriggerInfo);
            }
        }
        
    }
}
