/*
    Copyright 2016 Peoplutions
*/

namespace DrpServerQueueUser.Actions
{

    #region Using Statements

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Drp;
    using Drp.Types;

    #endregion

    /// <summary>
    /// Base User Action Class.
    /// </summary>
    public abstract class ActionBase
    {
        /// <summary> Default Unuqiue Id for this action instance /// </summary>
        protected string _actionId = Guid.NewGuid().ToStringNoFormatting().ToUpper();

        /// <summary>
        /// Constructor taking optional instance id for this action
        /// </summary>
        /// <param name="actionId">optional - id for this action instance. If not provided, GUID is used.</param>
        public ActionBase(string actionId = null)
        {
            if (false == string.IsNullOrWhiteSpace(actionId))
            {
                this._actionId = actionId;
            }
        }

        /// <summary>
        /// QueueItem.ItemType value - ServerQueue Filtering/Categorization
        /// </summary>
        public string ItemType { get; set; }

        /// <summary>
        /// Count of number of times executed
        /// </summary>
        public int ExecutionCounter { get; protected set; }

        public abstract bool Execute(IServerQueue serverQueue);

        protected virtual string GetTypeName()
        {
            return this.GetType().Name;
        }

        public string TypeName {  get { return this.GetTypeName(); } }

        public override string ToString()
        {
            return string.Format(
                "Action [{0}:{1}]: Count[{2}], ItemType: {3}",
                this._actionId, this.TypeName, this.ExecutionCounter, this.ItemType);
        }

        public virtual async Task<bool> ExecuteAsync(IServerQueue serverQueue)
        {
            return await Task<bool>.Run(() => this.Execute(serverQueue));
        }

    }
}
