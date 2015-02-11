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
using System.Linq;
using System.Text;
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Compiler;
using Rhetos.Extensibility;
using Rhetos.Dsl;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    [ExportMetadata(MefProvider.DependsOn, typeof(OrmDataStructureCodeGenerator))]
    public class FilterIdRepositoryCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DataStructureInfo)conceptInfo;

            if (info.Module.Name == "Common" && info.Name == "FilterId")
                codeBuilder.InsertCode(snippet, RepositoryHelper.RepositoryMembers, info);

            codeBuilder.AddReferencesFromDependency(typeof(DeactivateInsteadOfDelete));
        }

        const string snippet =
@"        public IQueryable<Guid> CreateQueryableFilterIds(IEnumerable<Guid> ids)
        {
            if (!(ids is System.Collections.IList))
                ids = ids.ToList();

            var handle = Guid.NewGuid();
            string sqlInsertIdFormat = ""INSERT INTO Common.FilterId (Handle, Value) SELECT '"" + SqlUtility.GuidToString(handle) + ""', '{0}';"";

            const int chunkSize = 10000; // Keeping a moderate SQL script size.
            for (int start = 0; start < ids.Count(); start += chunkSize)
            {
                string sqlInsertIds = string.Join(""\r\n"", ids.Skip(start).Take(chunkSize)
                    .Select(id => string.Format(sqlInsertIdFormat, SqlUtility.GuidToString(id))));
                _executionContext.NHibernateSession.CreateSQLQuery(sqlInsertIds).ExecuteUpdate();
            }

            // Delete temporary data when closing the connection. The data must remain in the database until the returned query is used.
            string deleteFilterIds = ""DELETE FROM Common.FilterId WHERE Handle = "" + SqlUtility.QuoteGuid(handle);
            var deleteFilterIdsQuery = _executionContext.NHibernateSession.CreateSQLQuery(deleteFilterIds);
            _executionContext.PersistenceTransaction.BeforeClose += () =>
                {
                    try
                    {
                        deleteFilterIdsQuery.ExecuteUpdate();
                    }
                    catch
                    {
                        // Cleanup error may be ignored. The temporary data may be deleted on regular maintenance.
                    }
                };

            return Query().Where(filterId => filterId.Handle == handle).Select(filterId => filterId.Value.Value);
        }

";
    }
}
