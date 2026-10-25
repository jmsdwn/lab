using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using TALXIS.DevKit.Build.Dataverse.Plugin;

namespace Plugins.Warehouse
{
    [PluginStep("Create", "lab_warehousetransaction", PluginStage.PreValidation)]
    public class ValidateWarehouseTransactionPlugin : PluginBase
    {
        public ValidateWarehouseTransactionPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(ValidateWarehouseTransactionPlugin))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;
            var serviceFactory = localPluginContext.OrgSvcFactory;
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            var tracingService = localPluginContext.TracingService;

            if (!(context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity target) ||
                target.LogicalName != "lab_warehousetransaction")
            {
                return;
            }

            if (!target.Contains("lab_quantity") || !target.Contains("lab_itemid") || !target.Contains("lab_transactiontype"))
            {
                return;
            }

            var transactionType = target.GetAttributeValue<OptionSetValue>("lab_transactiontype");
            if (transactionType == null || transactionType.Value != 100000001)
            {
                return;
            }

            try
            {
                var quantity = target.GetAttributeValue<int>("lab_quantity");
                var itemRef = target.GetAttributeValue<EntityReference>("lab_itemid");
                if (itemRef == null)
                {
                    return;
                }

                var item = service.Retrieve("lab_warehouseitem", itemRef.Id, new ColumnSet("lab_availablequantity"));
                var available = item != null && item.Contains("lab_availablequantity")
                    ? item.GetAttributeValue<int>("lab_availablequantity")
                    : 0;

                if (quantity > available)
                {
                    throw new InvalidPluginExecutionException(
                        $"Not enough product in stock. Available: {available}, requested: {quantity}.");
                }
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                tracingService.Trace("Plugin Exception: {0}", ex.ToString());
                throw;
            }
        }
    }
}
