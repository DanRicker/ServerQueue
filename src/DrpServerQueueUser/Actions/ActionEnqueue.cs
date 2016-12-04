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

    internal class ActionEnqueue : ActionBase
    {

        public ActionEnqueue(string actionId = null)
            : base(actionId)
        { }

        public override bool Execute(IServerQueue serverQueue)
        {

            bool ret = false;

            string itemId = Guid.NewGuid().ToStringNoFormatting();
            string itemData = string.Format(
                "[Item Type: {0}, ItemId: {1}, Data: {2}]",
                this.ItemType,
                itemId,
                new string('d', 1000));

            string itemMetadata = string.Format(
                "[Item Type: {0}, ItemId: {1}, Metadata: {2}]",
                this.ItemType,
                itemId,
                new string('m', 100));

            try
            {
                IDrpQueueItem queueItem = serverQueue.Enqueue(
                    ItemType,
                    itemId,
                    itemData,
                    itemMetadata);
                ret = null != queueItem;
                this.ExecutionCounter++;
            }
            catch (System.Exception ex)
            {
                Drp.DrpExceptionHandler.HandleException("ActionEnqueue.ExecuteNextAction", ex);
                ret = false;
            }

            return ret;
        }

    }
}
