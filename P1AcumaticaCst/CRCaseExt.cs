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
using PX.Objects.CS;
using PX.Objects.CR.Workflow;
using System;
using PX.Objects.CR.Standalone;
using PX.TM;

namespace PX.Objects.CRC
{
    /// <summary>
    /// A cache extension for <see cref="CRCase"/> to include time tracking fields.
    /// </summary>
    public sealed class CRCaseExt : PXCacheExtension<CRCase>
    {
        #region IsActive
        public static bool IsActive() { return PXAccess.FeatureInstalled<FeaturesSet.projectModule>(); }
        #endregion

        #region UsrTimeSpentB

        [PXDBTimeSpanLong(Format = TimeSpanFormatType.LongHoursMinutes)]
        [PXDefault(0, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Time Spent-Billable", Enabled = true)]
        public int? UsrTimeSpentB
        {
            get; set;
        }

        public abstract class usrTimeSpentB : BqlInt.Field<usrTimeSpentB> { }
        #endregion

        #region UsrTimeSpent
        [PXDBTimeSpanLong(Format = TimeSpanFormatType.LongHoursMinutes)]
        [PXDefault(0, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Time Spent-Unbillable", Enabled = true)]
        public int? UsrTimeSpent
        {
            get; set;
        }
        public abstract class usrTimeSpent : BqlInt.Field<usrTimeSpent> { }
        #endregion

        #region UsrTimeSpentTotal

        [PXTimeSpanLong(Format = TimeSpanFormatType.LongHoursMinutes)]
        [PXDefault(0, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Total Time Spent", Enabled = false)]
        [PXFormula(typeof(Add<CRCaseExt.usrTimeSpentB, CRCaseExt.usrTimeSpent>))]
        [PXDependsOnFields(typeof(CRCaseExt.usrTimeSpentB), typeof(CRCaseExt.usrTimeSpent))]
        public int? UsrTimeSpentTotal { get; set; }
        public abstract class totalTimeSpent : PX.Data.BQL.BqlInt.Field<totalTimeSpent> { }
        #endregion

        #region UsrTimeSpentEstimatedEffort
        [PXDBTimeSpanLong(Format = TimeSpanFormatType.LongHoursMinutes)]
        [PXDefault(0, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Estimated Effort")]
        [PXUIEnabled(typeof(Where<CRCase.caseClassID, Equal<ReqClassId>,
             And<CRCase.status, Equal<OpportunityStatus.@new>, Or<CRCase.status, Equal<OpportunityStatus.open>>>>))]
        public int? UsrTimeSpentEstimatedEffort { get; set; }
        public abstract class usrTimeSpentEstimatedEffort : PX.Data.BQL.BqlInt.Field<usrTimeSpentEstimatedEffort> { }

        public class ReqClassId : PX.Data.BQL.BqlString.Constant<ReqClassId> { public ReqClassId() : base("REQUEST") { } }
        #endregion

        #region UsrStartTime
        [PXDBDateAndTime(DisplayMask = "t")]  // 't' is for short time pattern
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Start Time", Visible = false)]
        public DateTime? UsrStartTime { get; set; }
        public abstract class usrStartTime : PX.Data.BQL.BqlDateTime.Field<usrStartTime> { }
        #endregion

        #region 


        [PXDBDateAndTime(DisplayMask = "t")]  // 't' is for short time pattern
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Pause Time", Visible = false)]
        public DateTime? UsrPauseTime { get; set; }
        public abstract class usrPauseTime : PX.Data.BQL.BqlDateTime.Field<usrPauseTime> { }
        #endregion

        #region UsrStatusChangeTime
        [PXDBDateAndTime(DisplayMask = "t")]  // 't' is for short time pattern
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Status Change", Visible = false)]
        public DateTime? UsrStatusChangeTime { get; set; }
        public abstract class usrStatusChangeTime : PX.Data.BQL.BqlDateTime.Field<usrStatusChangeTime> { }
        #endregion

        #region UsrClosedTime
        [PXDBDateAndTime(DisplayMask = "t")]  // 't' is for short time pattern
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Closed Time", Visible = false)]
        public DateTime? UsrClosedTime { get; set; }
        public abstract class usrClosedTime : PX.Data.BQL.BqlDateTime.Field<usrClosedTime> { }
        #endregion

        #region UsrCumulativePausedTime
        [PXDBInt]  // This field stores the total paused time in minutes.
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Cumulative Paused Time", Visible = false)]  // Invisible in UI as it’s for internal tracking.
        public int? UsrCumulativePausedTime { get; set; }
        public abstract class usrCumulativePausedTime : PX.Data.BQL.BqlInt.Field<usrCumulativePausedTime> { }
        #endregion

        #region UsrTotalTimeClock
        [PXDBTimeSpanLong(Format = TimeSpanFormatType.LongHoursMinutes)] // This ensures hours and minutes are displayed.
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXFormula(typeof(Switch<
            Case<Where<CRCaseExt.usrClosedTime, IsNotNull>,
                DateDiff<CRCaseExt.usrStartTime, CRCaseExt.usrClosedTime, DateDiff.minute>>, int0>))]
        [PXUIField(DisplayName = "Resolution SLA")]
        [PXDependsOnFields(typeof(CRCaseExt.usrStartTime), typeof(CRCaseExt.usrClosedTime), typeof(CRCaseExt.usrCumulativePausedTime))] // Ensures recalculation when these fields change.
        public int? UsrTotalTimeClock { get; set; }
        public abstract class usrTotalTimeClock : PX.Data.BQL.BqlInt.Field<usrTotalTimeClock> { }
        #endregion

        #region UsrAssignedTime
        [PXDBDateAndTime(DisplayMask = "t")]// Stores time of assignment
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Assigned Time", Visible = false)]
        public DateTime? UsrAssignedTime { get; set; }
        public abstract class usrAssignedTime : PX.Data.BQL.BqlDateTime.Field<usrAssignedTime> { }
        #endregion

        #region UsrAssignedTimeB
        [PXDBDateAndTime(DisplayMask = "t")] // Stores time of assignment
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField (DisplayName = "Assigned Time B", Visible = false)]
        public DateTime? UsrAssignedTimeB { get; set; }
        public abstract class usrAssignedTimeB : PX.Data.BQL.BqlDateTime.Field<usrAssignedTimeB> { }
        #endregion

        #region UsrTimeBetweenASAndAA
        [PXTimeSpanLong(Format = TimeSpanFormatType.LongHoursMinutes)] // Display time in hours and minutes
        [PXDefault(0, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXFormula(typeof(Switch<
            Case<Where<CRCaseExt.usrAssignedTime, IsNotNull, And<CRCaseExt.usrAssignedTimeB, IsNotNull>>,
                DateDiff<CRCaseExt.usrAssignedTimeB, CRCaseExt.usrAssignedTime, DateDiff.minute>>,
            int0>))]
        [PXUIField(DisplayName = "1st Response SLA", Enabled = false)]
        [PXDependsOnFields(typeof(CRCaseExt.usrAssignedTime), typeof(CRCaseExt.usrAssignedTimeB))]
        public int? UsrTimeBetweenASAndAA { get; set; }
        public abstract class usrTimeBetweenASAndAA : PX.Data.BQL.BqlInt.Field<usrTimeBetweenASAndAA> { }
        #endregion

        #region UsrTestedBy

        public abstract class usrTestedBy : PX.Data.BQL.BqlInt.Field<usrTestedBy> { }
        [Owner(typeof(CRCase.workgroupID), Enabled = true, DisplayName = "Tested By")]
        public Int32? UsrTestedBy { get; set; }

        #endregion

        #region UsrTestResults
        [PXDBString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Test Results")]
        public string UsrTestResults { get; set; }
        public abstract class usrTestResults : PX.Data.BQL.BqlString.Field<usrTestResults> { }
        #endregion

        #region UsrTestedDate
        [PXDBDate]
        [PXUIField(DisplayName = "Tested Date")]
        public DateTime? UsrTestedDate { get; set; }
        public abstract class usrTestedDate : PX.Data.BQL.BqlDateTime.Field<usrTestedDate> { }
        #endregion
    }
}





