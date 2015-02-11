<Query Kind="Program">
  <Reference Relative="bin\Iesi.Collections.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Iesi.Collections.dll</Reference>
  <Reference Relative="bin\NHibernate.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\NHibernate.dll</Reference>
  <Reference Relative="bin\NLog.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\NLog.dll</Reference>
  <Reference Relative="bin\Oracle.DataAccess.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Oracle.DataAccess.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.AspNetFormsAuth.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.AspNetFormsAuth.dll</Reference>
  <Reference Relative="bin\Rhetos.Configuration.Autofac.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Configuration.Autofac.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Dom.DefaultConcepts.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Dom.DefaultConcepts.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Dom.DefaultConcepts.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Dom.DefaultConcepts.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Dom.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Dom.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Logging.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Logging.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Persistence.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Persistence.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Persistence.NHibernate.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Persistence.NHibernate.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Persistence.NHibernateDefaultConcepts.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Persistence.NHibernateDefaultConcepts.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Processing.DefaultCommands.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Processing.DefaultCommands.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Processing.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Processing.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Security.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Security.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Utilities.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Utilities.dll</Reference>
  <Reference Relative="bin\ServerDom.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\ServerDom.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.AccountManagement.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Serialization.dll</Reference>
  <Namespace>NHibernate</Namespace>
  <Namespace>NHibernate.Cfg</Namespace>
  <Namespace>NHibernate.Tool.hbm2ddl</Namespace>
  <Namespace>Oracle.DataAccess.Client</Namespace>
  <Namespace>Rhetos.Configuration.Autofac</Namespace>
  <Namespace>Rhetos.Dom</Namespace>
  <Namespace>Rhetos.Dom.DefaultConcepts</Namespace>
  <Namespace>Rhetos.Logging</Namespace>
  <Namespace>Rhetos.Persistence</Namespace>
  <Namespace>Rhetos.Persistence.NHibernate</Namespace>
  <Namespace>Rhetos.Persistence.NHibernateDefaultConcepts</Namespace>
  <Namespace>Rhetos.Security</Namespace>
  <Namespace>Rhetos.Utilities</Namespace>
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.DirectoryServices</Namespace>
  <Namespace>System.DirectoryServices.AccountManagement</Namespace>
  <Namespace>System.IO</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Reflection</Namespace>
  <Namespace>System.Runtime.Serialization.Json</Namespace>
  <Namespace>System.Text</Namespace>
  <Namespace>System.Xml</Namespace>
  <Namespace>System.Xml.Serialization</Namespace>
</Query>

void Main()
{
    ConsoleLogger.MinLevel = EventType.Info;
    var rhetosServerPath = Path.GetDirectoryName(Util.CurrentQueryPath);
    Directory.SetCurrentDirectory(rhetosServerPath);
    using (var container = new RhetosTestContainer(commitChanges: false))
    {
        var repository = container.Resolve<Common.DomRepository>();
        
        // PRINT 3 CLAIMS:
        var claimsAll = repository.Common.Claim.Query();
        claimsAll.Take(3).Dump();
        
        // PRINT CLAIM RESOURCES FROM COMMON MODULE, THAT HAVE 'New' CLAIM RIGHT:
        string.Join(", ", claimsAll
            .Where(c => c.ClaimResource.StartsWith("Common.") && c.ClaimRight == "New")
            .Select(c => c.ClaimResource)).Dump("Claim resources:");
        
        // ADD AND REMOVE A PRINCIPAL:
        var testUser = new Common.Principal { Name = "Test123ABC", ID = Guid.NewGuid() };
        repository.Common.Principal.Insert(new[] { testUser });
        repository.Common.Principal.Delete(new[] { testUser });
        
        // PRINT LOGGED EVENTS FOR THE 'Common.Principal' INSTANCE:
        repository.Common.LogReader.Query()
            .Where(log => log.TableName == "Common.Principal" && log.ItemId == testUser.ID)
            /*.Select(log => new {
                log.Created,
                log.Action,
                Client = SqlUtility.ExtractUserInfo(log.ContextInfo).UserName,
                log.Description })*/
            .ToList().Dump();
    }
}