using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Drp;
using Drp.Types;

namespace DrpServerQueueUser.Actions
{
    internal class ActionAcquireRelease : ActionBase
    {

        public ActionAcquireRelease(string actionId = null)
            : base(actionId)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool Execute(IServerQueue serverQueue)
        {
            IDrpQueueItem qiAcquire = null;
            IDrpQueueItem qiReleased = null;
            bool ret = false;

            try
            {
                String acquirerId = "AcquireRelease" + this._actionId;
                if (string.IsNullOrWhiteSpace(this.ItemType))
                {
                    IDrpQueueItem qiPeak = serverQueue.Peak(this.ItemType);
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
                    qiReleased = serverQueue.Release(acquirerId, qiAcquire.Id);
                }
                ret = (null != qiReleased);
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
