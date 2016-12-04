using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Drp;
using Drp.Types;

namespace DrpServerQueueUser.Actions
{
    internal class ActionAcquireDequeue : ActionBase
    {

        public ActionAcquireDequeue(string actionId = null)
            : base(actionId)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool Execute(IServerQueue serverQueue)
        {
            IDrpQueueItem qiAcquire = null;
            IDrpQueueItem qiDequeue = null;
            bool ret = false;

            try
            {
                String acquirerId = "AcquireDequeue" + this._actionId;
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
                }

                ret = (null != qiDequeue);
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
