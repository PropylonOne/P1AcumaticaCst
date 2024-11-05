using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN;
using System;
using static PX.Objects.EP.TimeCardMaint;
using Message = P1AcumaticaCst.Descriptor.Messages;

namespace PX.Objects.EP
{
    public class TimeCardMaint_Extension : PXGraphExtension<PX.Objects.EP.TimeCardMaint>
    {
        #region IsActive
        public static bool IsActive() { return PXAccess.FeatureInstalled<FeaturesSet.caseManagement>(); }
        #endregion

        #region Event Handlers
        [PXMergeAttributes]
        [PXUIField(DisplayName = "Case")]
        public virtual void _(Events.CacheAttached<EPTimeCardSummaryWithInfo.labourItemID> e) { }

        protected virtual void _(Events.RowPersisting<EPTimeCardSummaryWithInfo> e)
        {
                var timeCardSummary = e.Row as EPTimeCardSummaryWithInfo;
                if (timeCardSummary == null) return; 

                // Fetch the InventoryItem record based on LabourItemID
                InventoryItem inventoryItem = PXSelect<InventoryItem,
                Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>
                .Select(Base, timeCardSummary.LabourItemID);

                // Check if InventoryItem was found
                if (inventoryItem != null)
                {
                // Fetch the related CRCase record based on the InventoryCD
                 CRCase crCase = PXSelect<CRCase,
                 Where<CRCase.caseCD, Equal<Required<CRCase.caseCD>>>>
                 .Select(Base, inventoryItem.InventoryCD);
                if (crCase!= null ) return;
                // Ensure that Description is not null or empty
                if (timeCardSummary.Description==null)
                    {
                // Set the error on the Description field
                    PXUIFieldAttribute.SetError<EPTimeCardSummaryWithInfo.description>(e.Cache, timeCardSummary, Message.DescriptionRequired);
                    }
                }
                // Set an error if LabourItemID is empty
                if (timeCardSummary.LabourItemID == null)
                {
                PXUIFieldAttribute.SetError<EPTimeCardSummaryWithInfo.labourItemID>(e.Cache, timeCardSummary, Message.CaseIdRequired);
                }
        }
        #endregion
    }
}
