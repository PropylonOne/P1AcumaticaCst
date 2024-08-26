using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.EP;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Data.WorkflowAPI;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.GL;
using PX.Objects.GL.FinPeriods;
using PX.Objects.GL.FinPeriods.TableDefinition;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.SM;
using PX.TM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Branch = PX.Objects.GL.Branch;
using PX.Objects;
using PX.Objects.EP;
using PX.Objects.CA;
using Message = P1AcumaticaCst.Descriptor.Messages;
using static PX.Objects.EP.TimeCardMaint;

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
            // Make the Description field mandatory if LabourItemID is empty when saving the project
            if (e.Row.LabourItemID == null && string.IsNullOrEmpty(e.Row.Description))
            {
                PXUIFieldAttribute.SetError<EPTimeCardSummaryWithInfo.description>(e.Cache, e.Row,Message.DescriptionRequired );           
            }
        }

        #endregion
    }
}