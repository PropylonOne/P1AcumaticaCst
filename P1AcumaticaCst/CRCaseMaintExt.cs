/* ---------------------------------------------------------------------*
*                             Acumatica Inc.                            *
*              Copyright (c) 2005-2024 All rights reserved.             *
*                                                                       *
*                                                                       *
* This file and its contents are protected by United States and         *
* International copyright laws.  Unauthorized reproduction and/or       *
* distribution of all or any portion of the code contained herein       *
* is strictly prohibited and will result in severe civil and criminal   *
* penalties.  Any violations of this copyright will be prosecuted       *
* to the fullest extent possible under law.                             *
*                                                                       *
* UNDER NO CIRCUMSTANCES MAY THE SOURCE CODE BE USED IN WHOLE OR IN     *
* PART, AS THE BASIS FOR CREATING A PRODUCT THAT PROVIDES THE SAME, OR  *
* SUBSTANTIALLY THE SAME, FUNCTIONALITY AS ANY ACUMATICA PRODUCT.       *
*                                                                       *
* THIS COPYRIGHT NOTICE MAY NOT BE REMOVED FROM THIS FILE.              *
* --------------------------------------------------------------------- */

using PX.Data;
using PX.Data.BQL;
using PX.Objects.CR;
using PX.Objects.CR.Workflows;
using PX.Objects.CRC;
using PX.Objects.CS;
using PX.Objects.FA;
using System;
using static PX.CS.RMReport.FK;
using static PX.Data.BQL.BqlPlaceholder;
using static PX.Data.PXDateAndTimeAttribute;
using static PX.Objects.CRC.CRCaseExt;

namespace PX.Objects.CR
{
    /// <summary>
    /// A cache extension for <see cref="CRCase"/> to include time tracking fields.
    /// </summary>
    public class CRCaseMaintExt : PXGraphExtension<CRCaseMaint>
    {
        #region IsActive
        public static bool IsActive() { return PXAccess.FeatureInstalled<FeaturesSet.projectModule>(); }
        #endregion
        protected virtual void _(Events.RowSelected<CRCase> e)
        {
            CRCase row = e.Row;
            if (row == null) return;
            DateTime now = DateTime.Now;

            CRCaseExt rowExt = PXCache<CRCase>.GetExtension<CRCaseExt>(row);

            // Calculate UsrTimeSpentTotal
            rowExt.UsrTimeSpentTotal = (rowExt.UsrTimeSpent ?? 0) + (rowExt.UsrTimeSpentB ?? 0);

            // Only enable/disable the UsrTimeSpentEstimatedEffort field based on the current status
            bool enableEstimatedEffort = row.CaseClassID == "REQUEST" &&
                (row.Status == OpportunityStatus.New || row.Status == OpportunityStatus.Open);

            PXUIFieldAttribute.SetEnabled<CRCaseExt.usrTimeSpentEstimatedEffort>(e.Cache, row, enableEstimatedEffort);
            if (rowExt.UsrStartTime != null)
            {
                // Calculate UsrTotalTimeClock using the specified formula
                TimeSpan totalTime = now - rowExt.UsrStartTime.Value;
                int cumulativePausedTime = rowExt.UsrCumulativePausedTime ?? 0;
                TimeSpan pausedDuration = (rowExt.UsrPauseTime != null) ? now - rowExt.UsrPauseTime.Value : TimeSpan.Zero;

                int totalTimeMinutes = (int)totalTime.TotalMinutes - cumulativePausedTime - (int)pausedDuration.TotalMinutes;

                // Set UsrTotalTimeClock to the calculated value
                rowExt.UsrTotalTimeClock = totalTimeMinutes;
            }

            }

        protected virtual void _(Events.FieldUpdated<CRCase, CRCase.resolution> e)
        {
            CRCase row = e.Row;
            if (row == null) return;

            CRCaseExt rowExt = PXCache<CRCase>.GetExtension<CRCaseExt>(row);
            DateTime now = DateTime.Now;

            // Handle resolution transitions
            switch (row.Resolution)
            {
                case "IP":
                    HandleStartTimer(e.Cache, row, rowExt, now);
                    break;
                case "MI":
                case "CR":
                    HandlePauseTimer(e.Cache, row, rowExt, now);
                    break;
                case "AD":
                    HandleResumeTimer(e.Cache, row, rowExt, now);
                    break;
                case "RD":
                    HandleStopTimer(e.Cache, row, rowExt, now);
                    break;
            }

            // Capture time between AS (Unassigned) and AA (Assigned)
            if (row.Resolution == "AS")
            {
                // When resolution changes to "Unassigned", record the current time in UsrAssignedTimeB
                e.Cache.SetValueExt<CRCaseExt.usrAssignedTimeB>(row, now);
            }
            // Capture time between AS (Unassigned) and AA (Assigned)
            if (row.Resolution == "AA")
            {
                // When resolution changes to "Unassigned", record the current time in UsrAssignedTimeB
                e.Cache.SetValueExt<CRCaseExt.usrAssignedTime>(row, now);
            }

            if (row.Resolution == "AA" && rowExt.UsrAssignedTimeB != null)
            {
                // When resolution changes to "Assigned", calculate time difference from "Unassigned"
                TimeSpan timeBetweenASandAA = rowExt.UsrAssignedTime.Value - rowExt.UsrAssignedTimeB.Value;
                int timeInMinutes = (int)timeBetweenASandAA.TotalMinutes;

                // Set the calculated time difference in UsrTimeBetweenASAndAA
                e.Cache.SetValueExt<CRCaseExt.usrTimeBetweenASAndAA>(row, timeInMinutes);
            }
        }

        protected virtual void _(Events.FieldUpdated<CRCase, CRCase.status> e)
        {
            CRCase row = e.Row;
            if (row == null) return;

            // Access the CRCaseExt extension
            CRCaseExt rowExt = PXCache<CRCase>.GetExtension<CRCaseExt>(row);

            // Record the current time when the status changes
            DateTime now = DateTime.Now;
            e.Cache.SetValueExt<CRCaseExt.usrStatusChangeTime>(row, now);
        }


        private void HandleStartTimer(PXCache cache, CRCase row, CRCaseExt rowExt, DateTime now)
        {
            // Start the timer if this is the first time "IP" is encountered
            if (rowExt.UsrStartTime == null)
            {
                cache.SetValueExt<CRCaseExt.usrStartTime>(row, now);
            }

            // If previously paused, resume the timer
            if (rowExt.UsrPauseTime != null)
            {
                HandleResumeTimer(cache, row, rowExt, now);
            }
        }

        private void HandlePauseTimer(PXCache cache, CRCase row, CRCaseExt rowExt, DateTime now)
        {
            // Record the pause time if not already paused
            if (rowExt.UsrPauseTime == null)
            {
                cache.SetValueExt<CRCaseExt.usrPauseTime>(row, now);
            }
        }

        private void HandleResumeTimer(PXCache cache, CRCase row, CRCaseExt rowExt, DateTime now)
        {
            // Resume the timer by calculating the paused duration and adding it to the cumulative paused time
            if (rowExt.UsrPauseTime != null)
            {
                TimeSpan pausedDuration = now - rowExt.UsrPauseTime.Value;
                int pausedMinutes = (int)pausedDuration.TotalMinutes;

                // Update cumulative paused time
                int cumulativePausedTime = rowExt.UsrCumulativePausedTime ?? 0;
                cache.SetValueExt<CRCaseExt.usrCumulativePausedTime>(row, cumulativePausedTime + pausedMinutes);

                // Clear the pause time
                cache.SetValueExt<CRCaseExt.usrPauseTime>(row, null);
            }
        }

        private void HandleStopTimer(PXCache cache, CRCase row, CRCaseExt rowExt, DateTime now)
        {
            // Stop the timer by recording the close time if not already set
            if (rowExt.UsrClosedTime == null)
            {
                cache.SetValueExt<CRCaseExt.usrClosedTime>(row, now);
            }

            // If paused, add the paused time to the cumulative paused time
            if (rowExt.UsrPauseTime != null)
            {
                HandleResumeTimer(cache, row, rowExt, now);
            }

            // Stop the timer if Status is "O" (Open) and Resolution is "CC" (Closed Complete)
            if (row.Status == "O" && row.Resolution == "CC")
            {
                // Set UsrClosedTime to the current time
                cache.SetValueExt<CRCaseExt.usrClosedTime>(row, now);


            }
        }


        protected void CRCase_Resolution_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            CRCase row = (CRCase)e.Row;
            if (row == null) return;

            CRCaseExt rowExt = PXCache<CRCase>.GetExtension<CRCaseExt>(row);
            DateTime now = DateTime.Now;

          /*  // Update the total time field based on logic you want to implement
            int totalTimeMinutes = CalculateTotalTime(rowExt, now);
            cache.SetValueExt<CRCaseExt.usrTotalTimeClock>(row, totalTimeMinutes);*/

            // Capture time between AS (Unassigned) and AA (Assigned)
            if (row.Resolution == "AS" && rowExt.UsrAssignedTimeB == null)
            {
                // Record the current time in UsrAssignedTimeB
                cache.SetValueExt<CRCaseExt.usrAssignedTimeB>(row, now);
            }

            if (row.Resolution == "AA" && rowExt.UsrAssignedTimeB != null)
            {
                // Calculate time difference from "Unassigned"
                TimeSpan timeBetweenASandAA = rowExt.UsrAssignedTime.Value - rowExt.UsrAssignedTimeB.Value;
                int timeInMinutes = (int)timeBetweenASandAA.TotalMinutes;

                // Set the calculated time difference in UsrTimeBetweenASAndAA
                cache.SetValueExt<CRCaseExt.usrTimeBetweenASAndAA>(row, timeInMinutes);
            }
        }

       

    }

}
