using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Drp;
using Drp.Types;

namespace DrpServerQueueUser.Actions
{
    internal class ActionAcquireDequeueEnqueueNew : ActionBase
    {

        public ActionAcquireDequeueEnqueueNew(string actionId = null)
            : base(actionId)
        { }

        public string NewEnqueueItemType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool Execute(IServerQueue serverQueue)
        {
            IDrpQueueItem qiAcquire = null;
            IDrpQueueItem qiDequeue = null;
            IDrpQueueItem qiEnqueue = null;

            bool ret = false;

            try
            {
                String acquirerId = "AcquireDequeueEnqueue" + this._actionId;
                if (string.IsNullOrWhiteSpace(this.ItemType))
                {
                    IDrpQueueItem qiPeak = serverQueue.Peak();
                    if (null != qiPeak)
                    {
                        qiAcquire = serverQueue.Acquire(acquirerId, qiPeak.Id);
                    }
                }
                else
                {
                    qiAcquire = serverQueue.Acquire(acquirerId, this.ItemType);
                }
                if (null != qiAcquire)
                {
                    qiDequeue = serverQueue.Dequeue(acquirerId, qiAcquire.Id);
                    if (null != qiDequeue)
                    {
                        string newItemType = (string.IsNullOrWhiteSpace(this.NewEnqueueItemType) ? qiAcquire.ItemType : this.NewEnqueueItemType);

                        qiEnqueue = serverQueue.Enqueue(
                            newItemType,
                            qiAcquire.ItemId,
                            string.Format(
                                "Original QueueItemId: {0}  [ItemId: {1} ItemData: {2}]",
                                qiAcquire.Id.ToStringDashes(), 
                                qiAcquire.ItemId, 
                                qiAcquire.ItemData),
                            string.Format(
                                "ItemType Original: {0}, New {1} ItemMetadata: {2}",
                                qiAcquire.ItemType,
                                newItemType,
                                qiAcquire.ItemMetadata));
                    }
                }
                ret = (null != qiEnqueue);
                this.ExecutionCounter++;
            }
            catch (System.Exception ex)
            {
                Drp.DrpExceptionHandler.HandleException("ActionAcquireRelease.ExecuteNextAction", ex);
                ret = false;
            }

            return ret;
        }
    }
}
