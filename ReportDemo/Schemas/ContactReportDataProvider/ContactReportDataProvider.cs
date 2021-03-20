namespace Terrasoft.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Terrasoft.Configuration.Reporting.FastReport;
	using Terrasoft.Configuration.SocialLeadGen;
	using Terrasoft.Core;
	using Terrasoft.Core.Entities;
	using Terrasoft.Core.Factories;
	using Terrasoft.Web.Common;
	using Terrasoft.Web.Http.Abstractions;

	#region Class: ContactReportDataProvider

	[DefaultBinding(typeof(IFastReportDataSourceDataProvider), Name = "ContactReportDataProvider")]
	public class ContactReportDataProvider : IFastReportDataSourceDataProvider
	{

		#region Fields: Private

		private readonly Guid _contactEntitySchemaUId = new Guid("16BE3651-8FE2-4159-8DD0-A803D4683DD3");

		#endregion

		#region Methods: Private
		private IEnumerable<IReadOnlyDictionary<string, object>> GetSettingsData(UserConnection userConnection)
		{
			IHttpContextAccessor hca = ClassFactory.Get<IHttpContextAccessor>();
			var request = hca.GetInstance().Request;
			var applicationUrl = WebUtilities.GetBaseApplicationUrl(request);

			var result = new List<IReadOnlyDictionary<string, object>>()
			{
				new Dictionary<string, object>
				{
					["ApplicationUrl"] = applicationUrl,
					["CurrentUser"] = userConnection.CurrentUser.ContactName
				}
			};
			return result;
		}


		private IEnumerable<IReadOnlyDictionary<string, object>> GetContactData(UserConnection userConnection,
			Guid entitySchemaUId, IEntitySchemaQueryFilterItem filter)
		{
			var entitySchema = userConnection.EntitySchemaManager.GetInstanceByUId(_contactEntitySchemaUId);
			EntitySchemaQuery query = new EntitySchemaQuery(entitySchema);
			var idColumn = query.AddColumn("Id");
			query.AddColumn("Name");
			query.AddColumn("Phone");
			query.AddColumn("HomePhone");
			query.AddColumn("MobilePhone");
			query.AddColumn("Email");
			query.AddColumn("Address");
			query.AddColumn("Zip");
			var ownerColumn = query.AddColumn("Owner");
			var countryColumn = query.AddColumn("Country");
			var regionColumn = query.AddColumn("Region");
			var cityColumn = query.AddColumn("City");
			
			query.Filters.Add(filter);
			EntityCollection collection = query.GetEntityCollection(userConnection);
			var result = new List<IReadOnlyDictionary<string, object>>();
			foreach (var entity in collection)
			{
				var lookupColumnOwner = entity.Schema.Columns.GetByName(ownerColumn.Name);
				var lookupColumnCountry = entity.Schema.Columns.GetByName(countryColumn.Name);
				var lookupColumnRegion = entity.Schema.Columns.GetByName(regionColumn.Name);
				var lookupColumnCity = entity.Schema.Columns.GetByName(cityColumn.Name);
				result.Add(new Dictionary<string, object>
				{
					["Name"] = entity.GetColumnValue("Name").ToString(),
					["Phone"] = entity.GetColumnValue("Phone").ToString(),
					["HomePhone"] = entity.GetColumnValue("HomePhone").ToString(),
					["MobilePhone"] = entity.GetColumnValue("MobilePhone").ToString(),
					["Id"] = entity.GetTypedColumnValue<Guid>(idColumn.Name),
					["Email"] = entity.GetTypedColumnValue<string>("Email"),
					["Address"] = entity.GetTypedColumnValue<string>("Address"),
					["Zip"] = entity.GetTypedColumnValue<string>("Zip"),
					["OwnerName"] = entity.GetTypedColumnValue<string>(lookupColumnOwner.DisplayColumnValueName),
					["City"] = entity.GetTypedColumnValue<string>(lookupColumnCity.DisplayColumnValueName),
					["Region"] = entity.GetTypedColumnValue<string>(lookupColumnRegion.DisplayColumnValueName),
					["Country"] = entity.GetTypedColumnValue<string>(lookupColumnCountry.DisplayColumnValueName),
				});
			}
			return result;
		}
		private IEnumerable<IReadOnlyDictionary<string, object>> GetActivityData(UserConnection userConnection,
			Guid entitySchemaUId, IEntitySchemaQueryFilterItem filter)
		{
			var entitySchema = userConnection.EntitySchemaManager.GetInstanceByName("Activity");
			EntitySchemaQuery query = new EntitySchemaQuery(entitySchema);
			var idColumn = query.AddColumn("Id");
			query.AddColumn("Title");
			query.AddColumn("StartDate");
			query.AddColumn("DueDate");
			var ownerId = query.AddColumn("Owner.Id");

			var subQueryExpression = query.CreateSubEntitySchemaExpression("Owner.Id");
			subQueryExpression.SubQuery.Filters.Add(filter);
			var subFilter = new EntitySchemaQueryFilter(FilterComparisonType.Exists);
			subFilter.RightExpressions.Add(subQueryExpression);
			query.Filters.Add(subFilter);

			EntityCollection collection = query.GetEntityCollection(userConnection);
			var result = new List<IReadOnlyDictionary<string, object>>();
			foreach (var entity in collection)
			{
				result.Add(new Dictionary<string, object>
				{
					["Owner"] = entity.GetTypedColumnValue<Guid>(ownerId.Name),
					["Id"] = entity.GetTypedColumnValue<Guid>(idColumn.Name),
					["Title"] = entity.GetTypedColumnValue<string>("Title"),
					["StartDate"] = entity.GetTypedColumnValue<DateTime>("StartDate"),
					["DueDate"] = entity.GetTypedColumnValue<DateTime>("DueDate"),
				});
			}
			//DataValueTypes provided in (ENUM)Terrasoft.Nui.ServiceModel.DataContract.DataValueType
			return result;
		}

		private IEntitySchemaQueryFilterItem ExtractFilterFromParameters(UserConnection userConnection,
			Guid entitySchemaUId, IReadOnlyDictionary<string, object> parameters)
		{
			var managerItem = userConnection.EntitySchemaManager.GetItemByUId(entitySchemaUId);
			return parameters.ExtractEsqFilterFromReportParameters(userConnection, managerItem.Name) ??
				throw new Exception();
			;
		}
		#endregion

		#region Methods: Public

		public Task<ReportDataDictionary> GetData(UserConnection userConnection,
			IReadOnlyDictionary<string, object> parameters)
		{
			var mainFilter = ExtractFilterFromParameters(userConnection, _contactEntitySchemaUId, parameters);
			var result = new ReportDataDictionary
			{
				["Contact"] = GetContactData(userConnection, _contactEntitySchemaUId, mainFilter),
				["Activity"] = GetActivityData(userConnection, _contactEntitySchemaUId, mainFilter),
				["Environment"] = GetSettingsData(userConnection),
			};
			return Task.FromResult(result);
		}
		#endregion
	}

	#endregion

}

